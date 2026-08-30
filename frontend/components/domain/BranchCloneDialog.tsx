"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { useCloneBranch } from "@/lib/hooks/use-branches";
import type { BranchResponse } from "@/lib/api/branches";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  branches: BranchResponse[];
  /** Chi nhánh nguồn được chọn sẵn (vd bấm "Nhân bản" từ dòng cụ thể) */
  defaultSourceId?: number;
}

export function BranchCloneDialog({ open, onOpenChange, branches, defaultSourceId }: Props) {
  const cloneMutation = useCloneBranch();
  const [sourceId, setSourceId] = useState<number | "">(defaultSourceId ?? "");
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [address, setAddress] = useState("");
  const [phone, setPhone] = useState("");

  function reset() {
    setSourceId(defaultSourceId ?? "");
    setCode("");
    setName("");
    setAddress("");
    setPhone("");
  }

  function handleSubmit() {
    if (!sourceId || !code || !name) return;
    cloneMutation.mutate(
      {
        sourceBranchId: sourceId,
        body: {
          source_branch_id: Number(sourceId),
          code,
          name,
          address: address || undefined,
          phone: phone || undefined,
        },
      },
      {
        onSuccess: () => {
          reset();
          onOpenChange(false);
        },
      }
    );
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset();
        onOpenChange(o);
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Nhân bản chi nhánh</DialogTitle>
          <DialogDescription>
            Chỉ sao chép cấu hình phòng/kho/bộ đếm/giá — KHÔNG sao chép bệnh nhân, tồn kho, mã
            CSKCB, nhân sự. Chi nhánh mới sẽ ở trạng thái Nháp (DRAFT).
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div className="space-y-1">
            <Label htmlFor="clone-source">
              Chi nhánh nguồn <span className="text-destructive">*</span>
            </Label>
            <select
              id="clone-source"
              className="h-9 w-full rounded-md border bg-background px-3 text-sm"
              value={sourceId}
              onChange={(e) => setSourceId(e.target.value ? Number(e.target.value) : "")}
            >
              <option value="">-- Chọn chi nhánh nguồn --</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name} ({b.code})
                </option>
              ))}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="clone-code">
                Mã chi nhánh mới <span className="text-destructive">*</span>
              </Label>
              <Input id="clone-code" value={code} onChange={(e) => setCode(e.target.value)} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="clone-name">
                Tên chi nhánh mới <span className="text-destructive">*</span>
              </Label>
              <Input id="clone-name" value={name} onChange={(e) => setName(e.target.value)} />
            </div>
          </div>
          <div className="space-y-1">
            <Label htmlFor="clone-phone">Điện thoại</Label>
            <Input id="clone-phone" value={phone} onChange={(e) => setPhone(e.target.value)} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="clone-address">Địa chỉ</Label>
            <Input
              id="clone-address"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Huỷ
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!sourceId || !code || !name || cloneMutation.isPending}
          >
            {cloneMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Nhân bản
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
