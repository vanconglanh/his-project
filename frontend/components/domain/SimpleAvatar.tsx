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

const PALETTE = [
  { bg: "#22C55E", fg: "#FFFFFF" },
  { bg: "#0EA5E9", fg: "#FFFFFF" },
  { bg: "#EF4444", fg: "#FFFFFF" },
  { bg: "#8B5CF6", fg: "#FFFFFF" },
  { bg: "#F472B6", fg: "#FFFFFF" },
  { bg: "#F97316", fg: "#FFFFFF" },
  { bg: "#14B8A6", fg: "#FFFFFF" },
  { bg: "#EAB308", fg: "#1F2937" },
  { bg: "#A855F7", fg: "#FFFFFF" },
  { bg: "#3B82F6", fg: "#FFFFFF" },
];

function colorFor(name?: string | null) {
  if (!name) return PALETTE[PALETTE.length - 1];
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) >>> 0;
  return PALETTE[h % PALETTE.length];
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
