namespace ProDiabHis.Application.Reports.Engine;

/// <summary>
/// Parse cong thuc cot tinh toan (calculated field) do nguoi dung nhap thanh SQL an toan.
/// TUYET DOI khong noi suy chuoi tu do vao SQL — chi chap nhan 3 loai token:
///   - Identifier khop dung 1 Measure field cua Dataset (whitelist) -> thay bang SUM(sqlExpr)
///   - So thap phan (literal)
///   - Toan tu + - * / va dau ngoac ( )
/// Bat ky ky tu/token nao khac (chuoi, ten ham, dau ';', v.v.) -> nem REPORT_DEFINITION_INVALID (400),
/// KHONG BAO GIO lot vao cau SQL sinh ra. Chia cho 0 -> boc mau so bang NULLIF(expr, 0).
/// </summary>
public static class CalcFormulaParser
{
    public const int MaxFormulaLength = 300;

    /// <summary>Sinh bieu thuc SQL (da wrap dau ngoac + NULLIF cho phep chia) tu 1 cong thuc calc field.</summary>
    public static string ToSql(Dataset dataset, string formula)
    {
        if (string.IsNullOrWhiteSpace(formula))
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Công thức cột tính toán không được để trống");

        if (formula.Length > MaxFormulaLength)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Công thức vượt quá độ dài cho phép ({MaxFormulaLength} ký tự)");

        var tokens = Tokenize(formula);
        var parser = new Parser(tokens, dataset);
        var node = parser.ParseExpression();
        parser.EnsureConsumed();

        return node.ToSql();
    }

    // ---------------- Tokenizer ---------------- //

    private enum TokenKind { Ident, Number, Plus, Minus, Star, Slash, LParen, RParen }

    private record Token(TokenKind Kind, string Text);

    private static List<Token> Tokenize(string formula)
    {
        var tokens = new List<Token>();
        var i = 0;
        while (i < formula.Length)
        {
            var c = formula[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsLetter(c) || c == '_')
            {
                var start = i;
                while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_')) i++;
                tokens.Add(new Token(TokenKind.Ident, formula[start..i]));
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                var start = i;
                var dotSeen = false;
                while (i < formula.Length && (char.IsDigit(formula[i]) || (formula[i] == '.' && !dotSeen)))
                {
                    if (formula[i] == '.') dotSeen = true;
                    i++;
                }
                var numText = formula[start..i];
                if (!decimal.TryParse(numText, System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out _))
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Số không hợp lệ trong công thức: '{numText}'");
                tokens.Add(new Token(TokenKind.Number, numText));
                continue;
            }

            var kind = c switch
            {
                '+' => TokenKind.Plus,
                '-' => TokenKind.Minus,
                '*' => TokenKind.Star,
                '/' => TokenKind.Slash,
                '(' => TokenKind.LParen,
                ')' => TokenKind.RParen,
                _ => (TokenKind?)null
            };

            if (kind is null)
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Ký tự không hợp lệ trong công thức: '{c}'");

            tokens.Add(new Token(kind.Value, c.ToString()));
            i++;
        }

        if (tokens.Count == 0)
            throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Công thức cột tính toán không được để trống");

        return tokens;
    }

    // ---------------- AST ---------------- //

    private abstract record Node
    {
        public abstract string ToSql();
    }

    private sealed record NumberNode(string Text) : Node
    {
        public override string ToSql() => Text;
    }

    private sealed record FieldNode(string SqlExpr) : Node
    {
        public override string ToSql() => $"SUM({SqlExpr})";
    }

    private sealed record UnaryMinusNode(Node Inner) : Node
    {
        public override string ToSql() => $"(-{Inner.ToSql()})";
    }

    private sealed record BinaryNode(char Op, Node Left, Node Right) : Node
    {
        public override string ToSql() => Op switch
        {
            '/' => $"({Left.ToSql()} / NULLIF({Right.ToSql()}, 0))",
            _ => $"({Left.ToSql()} {Op} {Right.ToSql()})"
        };
    }

    // ---------------- Recursive-descent parser: expr := term (('+'|'-') term)*; term := factor (('*'|'/') factor)*; factor := NUMBER | IDENT | '(' expr ')' | '-' factor ---------------- //

    private class Parser
    {
        private readonly List<Token> _tokens;
        private readonly Dataset _dataset;
        private int _pos;

        public Parser(List<Token> tokens, Dataset dataset)
        {
            _tokens = tokens;
            _dataset = dataset;
        }

        private Token? Current => _pos < _tokens.Count ? _tokens[_pos] : null;

        public void EnsureConsumed()
        {
            if (_pos != _tokens.Count)
                throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Công thức không hợp lệ — dư ký tự sau khi phân tích");
        }

        public Node ParseExpression()
        {
            var left = ParseTerm();
            while (Current is { Kind: TokenKind.Plus or TokenKind.Minus } tok)
            {
                _pos++;
                var right = ParseTerm();
                left = new BinaryNode(tok.Kind == TokenKind.Plus ? '+' : '-', left, right);
            }
            return left;
        }

        private Node ParseTerm()
        {
            var left = ParseFactor();
            while (Current is { Kind: TokenKind.Star or TokenKind.Slash } tok)
            {
                _pos++;
                var right = ParseFactor();
                left = new BinaryNode(tok.Kind == TokenKind.Star ? '*' : '/', left, right);
            }
            return left;
        }

        private Node ParseFactor()
        {
            var tok = Current ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Công thức thiếu toán hạng");

            if (tok.Kind == TokenKind.Minus)
            {
                _pos++;
                return new UnaryMinusNode(ParseFactor());
            }

            if (tok.Kind == TokenKind.Number)
            {
                _pos++;
                return new NumberNode(tok.Text);
            }

            if (tok.Kind == TokenKind.Ident)
            {
                _pos++;
                var field = _dataset.FindField(tok.Text)
                    ?? throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{tok.Text}' không thuộc dataset '{_dataset.Key}' hoặc không hợp lệ trong công thức");

                if (field.Role != DatasetFieldRole.Measure)
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Trường '{tok.Text}' là chiều (dimension) — công thức chỉ được tham chiếu số đo (measure)");

                return new FieldNode(field.SqlExpr);
            }

            if (tok.Kind == TokenKind.LParen)
            {
                _pos++;
                var inner = ParseExpression();
                if (Current is not { Kind: TokenKind.RParen })
                    throw new ReportValidationException("REPORT_DEFINITION_INVALID", "Công thức thiếu dấu đóng ngoặc ')'");
                _pos++;
                return inner;
            }

            throw new ReportValidationException("REPORT_DEFINITION_INVALID", $"Token không hợp lệ trong công thức: '{tok.Text}'");
        }
    }
}
