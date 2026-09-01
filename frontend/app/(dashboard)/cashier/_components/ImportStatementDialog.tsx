"use client";

import { useState } from "react";
import { isAxiosError } from "axios";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { UploadCloud } from "lucide-react";
import { useImportBankStatement } from "@/lib/hooks/use-bank-reconciliation";

export interface ImportStatementDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ImportStatementDialog({ open, onOpenChange }: ImportStatementDialogProps) {
  const [file, setFile] = useState<File | null>(null);
  const [bankCode, setBankCode] = useState("");
  const [statementDate, setStatementDate] = useState("");
  const importMutation = useImportBankStatement();

  function resetAndClose() {
    setFile(null);
    setBankCode("");
    setStatementDate("");
    onOpenChange(false);
  }

  function handleSubmit() {
    if (!file) {
      toast.error("Vui lòng chọn file sao kê (.xlsx hoặc .csv)");
      return;
    }
    importMutation.mutate(
      {
        file,
        bank_code: bankCode || undefined,
        statement_date: statementDate || undefined,
      },
      {
        onSuccess: (result) => {
          toast.success(
            `Đã nhập sao kê "${result.file_name}": khớp ${result.matched_lines}/${result.total_lines} dòng`
          );
          resetAndClose();
        },
        onError: (error) => {
          const message =
            (isAxiosError(error) && error.response?.data?.error?.message) ||
            "Không thể nhập file sao kê. Vui lòng thử lại.";
          toast.error(message);
        },
      }
    );
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) resetAndClose();
        else onOpenChange(true);
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Tải lên sao kê ngân hàng</DialogTitle>
          <DialogDescription>
            Chọn file sao kê (.xlsx hoặc .csv) để hệ thống tự động đối chiếu với khoản thu.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div className="space-y-1.5">
            <Label htmlFor="statement-file">File sao kê</Label>
            <Input
              id="statement-file"
              type="file"
              accept=".xlsx,.xls,.csv"
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="bank-code">Mã ngân hàng (tuỳ chọn)</Label>
            <Input
              id="bank-code"
              placeholder="VD: VCB, TCB, BIDV..."
              value={bankCode}
              onChange={(e) => setBankCode(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="statement-date">Kỳ sao kê (tuỳ chọn)</Label>
            <Input
              id="statement-date"
              type="date"
              value={statementDate}
              onChange={(e) => setStatementDate(e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={resetAndClose} disabled={importMutation.isPending}>
            Huỷ
          </Button>
          <Button onClick={handleSubmit} disabled={importMutation.isPending}>
            <UploadCloud className="h-4 w-4 mr-1" />
            {importMutation.isPending ? "Đang tải lên..." : "Tải lên"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
