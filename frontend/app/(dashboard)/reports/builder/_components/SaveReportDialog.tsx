"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Save, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import * as rolesApi from "@/lib/api/roles";
import type { ReportRoleCode, ReportVisibility } from "@/lib/api/reports";

interface SaveReportDialogProps {
  isEditing: boolean;
  defaultTitle: string;
  defaultVisibility: ReportVisibility;
  defaultSharedRoles: ReportRoleCode[];
  disabled?: boolean;
  isSaving: boolean;
  onSave: (title: string, visibility: ReportVisibility, sharedRoles: ReportRoleCode[]) => void;
}

/** Nút "Lưu báo cáo" + Dialog nhập Tên/Phạm vi — Dialog dùng pattern controlled open/onOpenChange chuẩn của dự án. */
export function SaveReportDialog({
  isEditing,
  defaultTitle,
  defaultVisibility,
  defaultSharedRoles,
  disabled,
  isSaving,
  onSave,
}: SaveReportDialogProps) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState(defaultTitle);
  const [visibility, setVisibility] = useState<ReportVisibility>(defaultVisibility);
  const [sharedRoles, setSharedRoles] = useState<ReportRoleCode[]>(defaultSharedRoles);

  const { data: roles = [], isLoading: isLoadingRoles } = useQuery({
    queryKey: ["roles"],
    queryFn: rolesApi.listRoles,
    enabled: open,
  });

  useEffect(() => {
    if (open) {
      setTitle(defaultTitle);
      setVisibility(defaultVisibility);
      setSharedRoles(defaultSharedRoles);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, defaultTitle, defaultVisibility]);

  function toggleRole(code: ReportRoleCode, checked: boolean) {
    setSharedRoles((prev) => (checked ? [...prev, code] : prev.filter((r) => r !== code)));
  }

  function handleSubmit() {
    if (!title.trim()) return;
    if (visibility === "ROLE" && sharedRoles.length === 0) return;
    onSave(title.trim(), visibility, visibility === "ROLE" ? sharedRoles : []);
  }

  const canSubmit = !!title.trim() && !(visibility === "ROLE" && sharedRoles.length === 0);

  return (
    <>
      <Button type="button" disabled={disabled} onClick={() => setOpen(true)} className="gap-1.5">
        <Save className="h-4 w-4" />
        {isEditing ? "Lưu thay đổi" : "Lưu báo cáo"}
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{isEditing ? "Cập nhật báo cáo" : "Lưu báo cáo mới"}</DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="report-title">Tên báo cáo</Label>
              <Input
                id="report-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="VD: Doanh thu theo bác sĩ tháng này"
                autoFocus
              />
            </div>

            <div className="space-y-1.5">
              <Label>Phạm vi hiển thị</Label>
              <RadioGroup value={visibility} onValueChange={(v) => v && setVisibility(v as ReportVisibility)} className="space-y-2">
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="TENANT" />
                  Cả phòng khám (mọi người có quyền đều xem được)
                </label>
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="ROLE" />
                  Theo vai trò
                </label>
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="PRIVATE" />
                  Chỉ mình tôi
                </label>
              </RadioGroup>

              {visibility === "ROLE" && (
                <div className="ml-6 space-y-1.5 rounded-md border p-2.5">
                  {isLoadingRoles ? (
                    <div className="flex items-center gap-2 py-2 text-sm text-muted-foreground">
                      <Loader2 className="h-4 w-4 animate-spin" />
                      Đang tải danh sách vai trò...
                    </div>
                  ) : (
                    <div className="grid grid-cols-2 gap-1.5">
                      {roles.map((r) => (
                        <label key={r.code} className="flex min-h-9 items-center gap-2 text-sm">
                          <Checkbox
                            checked={sharedRoles.includes(r.code)}
                            onCheckedChange={(v) => toggleRole(r.code, v === true)}
                          />
                          {r.name}
                        </label>
                      ))}
                    </div>
                  )}
                  {!isLoadingRoles && sharedRoles.length === 0 && (
                    <p className="text-xs text-[color:var(--status-warning)]">
                      Chọn ít nhất 1 vai trò được chia sẻ.
                    </p>
                  )}
                </div>
              )}
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={isSaving}>
              Huỷ
            </Button>
            <Button type="button" onClick={handleSubmit} disabled={!canSubmit || isSaving}>
              {isSaving ? "Đang lưu..." : "Lưu"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
