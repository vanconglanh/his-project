"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { useAssignRoles } from "@/lib/hooks/use-users";
import type { UserResponse } from "@/lib/api/types";

const ROLES = [
  { code: "ADMIN", label: "Quản trị viên" },
  { code: "BACSI", label: "Bác sĩ" },
  { code: "DIEUDUONG", label: "Điều dưỡng" },
  { code: "LETAN", label: "Lễ tân" },
  { code: "DUOCSI", label: "Dược sĩ" },
  { code: "KETOAN", label: "Kế toán" },
  { code: "KYTHUATVIEN", label: "Kỹ thuật viên" },
];

interface AssignRolesFormProps {
  user: UserResponse;
  onClose: () => void;
}

export function AssignRolesForm({ user, onClose }: AssignRolesFormProps) {
  const [selected, setSelected] = useState<string[]>(user.roles.map((r) => r.code));
  const assignMutation = useAssignRoles();

  function toggleRole(code: string) {
    setSelected((prev) =>
      prev.includes(code) ? prev.filter((r) => r !== code) : [...prev, code]
    );
  }

  async function handleSubmit() {
    await assignMutation.mutateAsync({ id: user.id, role_codes: selected });
    onClose();
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Chọn vai trò cho <span className="font-medium text-foreground">{user.full_name}</span>
      </p>

      <div className="grid grid-cols-2 gap-3">
        {ROLES.map((role) => (
          <div key={role.code} className="flex items-center gap-2">
            <Checkbox
              id={`assign-${role.code}`}
              checked={selected.includes(role.code)}
              onCheckedChange={() => toggleRole(role.code)}
            />
            <Label htmlFor={`assign-${role.code}`} className="font-normal cursor-pointer">
              {role.label}
            </Label>
          </div>
        ))}
      </div>

      <div className="flex justify-end gap-3 pt-2">
        <Button variant="outline" onClick={onClose} disabled={assignMutation.isPending}>
          Huỷ
        </Button>
        <Button onClick={handleSubmit} disabled={assignMutation.isPending}>
          {assignMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Lưu vai trò
        </Button>
      </div>
    </div>
  );
}
