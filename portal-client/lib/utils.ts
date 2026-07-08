/** Ghép class name, bỏ qua giá trị falsy — không phụ thuộc thư viện ngoài */
export function cn(...values: Array<string | false | null | undefined>): string {
  return values.filter(Boolean).join(" ");
}
