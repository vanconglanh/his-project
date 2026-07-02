import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { cn } from "@/lib/utils";

interface Props {
  name?: string | null;
  avatarUrl?: string | null;
  size?: "sm" | "md" | "lg";
  className?: string;
}

function getInitials(name?: string | null): string {
  if (!name) return "?";
  const clean = name.replace(/\(.*?\)/g, "").trim();
  const words = clean.split(/\s+/).filter(Boolean);
  if (words.length === 0) return "?";
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  const first = words[0][0] ?? "";
  const last = words[words.length - 1][0] ?? "";
  return (first + last).toUpperCase();
}

const PALETTE_SIZE = 10;

function colorFor(name?: string | null) {
  let index = PALETTE_SIZE - 1;
  if (name) {
    let h = 0;
    for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0;
    index = h % PALETTE_SIZE;
  }
  const n = index + 1;
  return { bg: `var(--avatar-${n})`, fg: `var(--avatar-${n}-foreground)` };
}

const sizeClasses = {
  sm: "h-10 w-10 text-sm",
  md: "h-16 w-16 text-lg",
  lg: "h-24 w-24 text-2xl",
};

export function SimpleAvatar({ name, avatarUrl, size = "md", className }: Props) {
  const color = colorFor(name);
  return (
    <Avatar className={cn(sizeClasses[size], className)}>
      <AvatarImage src={avatarUrl ?? undefined} alt={name ?? ""} />
      <AvatarFallback
        className="font-semibold"
        style={{ backgroundColor: color.bg, color: color.fg }}
      >
        {getInitials(name)}
      </AvatarFallback>
    </Avatar>
  );
}
