"use client";

import { useRef } from "react";
import { Camera } from "lucide-react";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useUploadAvatar } from "@/lib/hooks/use-patients";
import { toast } from "sonner";

const MAX_SIZE = 2 * 1024 * 1024; // 2MB

interface PatientAvatarProps {
  patientId: string;
  avatarUrl?: string | null;
  fullName: string;
  size?: "sm" | "md" | "lg";
  editable?: boolean;
}

function getInitials(name?: string | null): string {
  if (!name) return "?";
  return (
    name
      .split(" ")
      .slice(-2)
      .map((w) => w[0] ?? "")
      .join("")
      .toUpperCase() || "?"
  );
}

const sizeClasses = {
  sm: "h-10 w-10",
  md: "h-16 w-16",
  lg: "h-24 w-24",
};

export function PatientAvatar({
  patientId,
  avatarUrl,
  fullName,
  size = "md",
  editable = false,
}: PatientAvatarProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const uploadMutation = useUploadAvatar(patientId);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!["image/png", "image/jpeg"].includes(file.type)) {
      toast.error("Chỉ chấp nhận ảnh PNG hoặc JPEG");
      return;
    }
    if (file.size > MAX_SIZE) {
      toast.error("Ảnh đại diện vượt quá 2MB");
      return;
    }

    uploadMutation.mutate(file);
    // Reset so same file can be re-selected
    e.target.value = "";
  };

  return (
    <div className="relative inline-block group">
      <Avatar className={sizeClasses[size]}>
        <AvatarImage src={avatarUrl ?? undefined} alt={fullName} />
        <AvatarFallback>{getInitials(fullName)}</AvatarFallback>
      </Avatar>

      {editable && (
        <>
          <button
            type="button"
            className="absolute inset-0 flex items-center justify-center rounded-full bg-black/40 opacity-0 group-hover:opacity-100 transition-opacity cursor-pointer"
            onClick={() => fileInputRef.current?.click()}
            aria-label="Thay đổi ảnh đại diện"
            disabled={uploadMutation.isPending}
          >
            <Camera className="h-5 w-5 text-white" />
          </button>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/png,image/jpeg"
            className="hidden"
            onChange={handleFileChange}
          />
        </>
      )}
    </div>
  );
}
