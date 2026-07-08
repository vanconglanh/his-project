using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProDiabHis.ClinicGen;

/// <summary>
/// Generator: doc answers.json -> sinh bo deploy cho 1 phong kham (1 stack + 1 DB rieng):
///   deployments/&lt;code&gt;/{.env, docker-compose.yml, nginx.conf, migrate.sh, seed.sql}
/// + cap nhat deployments/registry.json. Secret tu sinh (giu on dinh khi re-gen).
///
/// Dung:  prodiab-clinic-gen --answers deploy/answers.example.json
///        [--out deploy/deployments] [--repo-root &lt;path&gt;]
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var opts = ParseArgs(args);
            var repoRoot = LocateRepoRoot(opts.GetValueOrDefault("repo-root"));
            var answersPath = opts.GetValueOrDefault("answers") ?? Path.Combine(repoRoot, "deploy", "answers.example.json");
            var outDir = opts.GetValueOrDefault("out") ?? Path.Combine(repoRoot, "deploy", "deployments");
            var templateDir = Path.Combine(repoRoot, "deploy", "template");

            var answers = LoadAnswers(answersPath);
            var code = answers.Clinic.Code;
            if (string.IsNullOrWhiteSpace(code)) throw new Exception("Thieu clinic.code");

            var clinicOut = Path.Combine(outDir, code);
            Directory.CreateDirectory(clinicOut);

            // Secrets: reuse tu .env cu neu co (khong xoay secret khi re-gen), else sinh moi
            var secrets = LoadOrCreateSecrets(Path.Combine(clinicOut, ".env"), code);

            // Registry + cap phat nginx_port
            var registryPath = Path.Combine(outDir, "registry.json");
            var registry = LoadRegistry(registryPath);
            var nginxPort = ResolveNginxPort(answers, code, registry);

            var domain = answers.Deployment.Domain;
            var baseDomain = DeriveBaseDomain(domain);

            // Token thay the cho template
            var tokens = new Dictionary<string, string>
            {
                ["CLINIC_CODE"] = code,
                ["CLINIC_NAME"] = answers.Clinic.Name,
                ["DOMAIN"] = domain,
                ["BASE_DOMAIN"] = baseDomain,
                ["NGINX_PORT"] = nginxPort.ToString(),
                ["REPO_REL"] = "../../..", // deploy/deployments/<code> -> repo root
                ["DB_NAME"] = secrets["DB_NAME"],
                ["DB_USER"] = secrets["DB_USER"],
                ["DB_PASSWORD"] = secrets["DB_PASSWORD"],
                ["DB_ROOT_PASSWORD"] = secrets["DB_ROOT_PASSWORD"],
                ["REDIS_PASSWORD"] = secrets["REDIS_PASSWORD"],
                ["MINIO_ROOT_USER"] = secrets["MINIO_ROOT_USER"],
                ["MINIO_ROOT_PASSWORD"] = secrets["MINIO_ROOT_PASSWORD"],
                ["JWT_SECRET"] = secrets["JWT_SECRET"],
                ["ENCRYPTION_MASTER_KEY"] = secrets["ENCRYPTION_MASTER_KEY"],
                ["SMTP_HOST"] = answers.Smtp?.Host ?? "",
                ["SMTP_PORT"] = (answers.Smtp?.Port ?? 587).ToString(),
                ["SMTP_USER"] = answers.Smtp?.User ?? "",
                ["SMTP_PASS"] = answers.Smtp?.Pass ?? "",
                ["SMTP_FROM"] = string.IsNullOrWhiteSpace(answers.Smtp?.From) ? $"no-reply@{domain}" : answers.Smtp!.From!,
                ["SMTP_SSL"] = (answers.Smtp?.Ssl ?? true) ? "true" : "false",
                ["SENTRY_DSN"] = answers.Deployment.SentryDsn ?? "",
            };

            // Render templates
            Render(Path.Combine(templateDir, "env.tmpl"), Path.Combine(clinicOut, ".env"), tokens);
            Render(Path.Combine(templateDir, "docker-compose.yml.tmpl"), Path.Combine(clinicOut, "docker-compose.yml"), tokens);
            Render(Path.Combine(templateDir, "nginx.conf.tmpl"), Path.Combine(clinicOut, "nginx.conf"), tokens);
            File.Copy(Path.Combine(templateDir, "migrate.sh"), Path.Combine(clinicOut, "migrate.sh"), overwrite: true);

            // Seed.sql tu answers
            var seed = SeedBuilder.Build(answers);
            File.WriteAllText(Path.Combine(clinicOut, "seed.sql"), seed, new UTF8Encoding(false));

            // Cap nhat registry
            UpsertRegistry(registry, new RegistryEntry(code, answers.Clinic.Name, domain, nginxPort, "generated"));
            SaveRegistry(registryPath, registry);

            Console.WriteLine($"[OK] Da sinh bo deploy cho '{code}' tai {clinicOut}");
            Console.WriteLine($"     domain={domain}  nginx_port={nginxPort}  db={secrets["DB_NAME"]}");
            Console.WriteLine($"     Deploy: cd {clinicOut} && docker compose up -d --build");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[LOI] {ex.Message}");
            return 1;
        }
    }

    // ── Args & repo ─────────────────────────────────────────
    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < args.Length; i++)
            if (args[i].StartsWith("--"))
                d[args[i][2..]] = (i + 1 < args.Length && !args[i + 1].StartsWith("--")) ? args[++i] : null;
        return d;
    }

    private static string LocateRepoRoot(string? overrideRoot)
    {
        if (!string.IsNullOrEmpty(overrideRoot)) return Path.GetFullPath(overrideRoot);
        var dir = Directory.GetCurrentDirectory();
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir, "deploy", "template"))) return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new Exception("Khong tim thay repo root (deploy/template). Dung --repo-root.");
    }

    // ── Answers ─────────────────────────────────────────────
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private static Answers LoadAnswers(string path)
    {
        if (!File.Exists(path)) throw new Exception($"Khong tim thay answers: {path}");
        var a = JsonSerializer.Deserialize<Answers>(File.ReadAllText(path), JsonOpts)
            ?? throw new Exception("answers.json rong/khong hop le");
        if (a.Clinic is null || a.Deployment is null || a.Admin is null)
            throw new Exception("answers thieu section bat buoc: clinic / deployment / admin");
        return a;
    }

    // ── Secrets ─────────────────────────────────────────────
    private static Dictionary<string, string> LoadOrCreateSecrets(string envPath, string code)
    {
        var s = new Dictionary<string, string>();
        var wanted = new[] { "DB_ROOT_PASSWORD", "DB_NAME", "DB_USER", "DB_PASSWORD",
            "REDIS_PASSWORD", "MINIO_ROOT_USER", "MINIO_ROOT_PASSWORD", "JWT_SECRET", "ENCRYPTION_MASTER_KEY" };

        if (File.Exists(envPath))
            foreach (var line in File.ReadAllLines(envPath))
            {
                var t = line.Trim();
                if (t.StartsWith("#") || !t.Contains('=')) continue;
                var idx = t.IndexOf('=');
                var k = t[..idx].Trim();
                if (Array.IndexOf(wanted, k) >= 0) s[k] = t[(idx + 1)..].Trim();
            }

        s.TryAdd("DB_NAME", $"prodiab_{code}");
        s.TryAdd("DB_USER", $"prodiab_{Trunc(code, 16)}");
        s.TryAdd("DB_ROOT_PASSWORD", RandPassword(28));
        s.TryAdd("DB_PASSWORD", RandPassword(28));
        s.TryAdd("REDIS_PASSWORD", RandPassword(28));
        s.TryAdd("MINIO_ROOT_USER", $"minio_{Trunc(code, 12)}");
        s.TryAdd("MINIO_ROOT_PASSWORD", RandPassword(28));
        s.TryAdd("JWT_SECRET", RandPassword(48));
        s.TryAdd("ENCRYPTION_MASTER_KEY", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        return s;
    }

    private static string Trunc(string s, int n) => s.Length <= n ? s : s[..n];

    private static string RandPassword(int len)
    {
        const string a = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var b = RandomNumberGenerator.GetBytes(len);
        return string.Concat(b.Select(x => a[x % a.Length]));
    }

    // ── Template render ─────────────────────────────────────
    private static void Render(string tmplPath, string outPath, Dictionary<string, string> tokens)
    {
        var text = File.ReadAllText(tmplPath);
        foreach (var (k, v) in tokens) text = text.Replace("{{" + k + "}}", v);
        File.WriteAllText(outPath, text, new UTF8Encoding(false));
    }

    private static string DeriveBaseDomain(string domain)
    {
        var parts = domain.Split('.');
        return parts.Length >= 3 ? string.Join('.', parts.Skip(1)) : domain;
    }

    // ── Registry + nginx port ───────────────────────────────
    private static int ResolveNginxPort(Answers a, string code, List<RegistryEntry> reg)
    {
        if (a.Deployment.NginxPort is int p and > 0) return p;
        var existing = reg.FirstOrDefault(r => r.Code == code);
        if (existing is not null) return existing.NginxPort;
        var used = reg.Select(r => r.NginxPort).ToHashSet();
        var port = 8090;
        while (used.Contains(port)) port++;
        return port;
    }

    private static List<RegistryEntry> LoadRegistry(string path)
        => File.Exists(path)
            ? JsonSerializer.Deserialize<List<RegistryEntry>>(File.ReadAllText(path), JsonOpts) ?? new()
            : new();

    private static void UpsertRegistry(List<RegistryEntry> reg, RegistryEntry e)
    {
        reg.RemoveAll(r => r.Code == e.Code);
        reg.Add(e);
    }

    private static void SaveRegistry(string path, List<RegistryEntry> reg)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var opts = new JsonSerializerOptions(JsonOpts) { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(reg.OrderBy(r => r.Code), opts), new UTF8Encoding(false));
    }
}

// ── Registry model ──────────────────────────────────────────
public record RegistryEntry(string Code, string Name, string Domain,
    [property: JsonPropertyName("nginx_port")] int NginxPort, string Status);
