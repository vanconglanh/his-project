"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Loader2, Paperclip, Search, X } from "lucide-react";
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
import { Textarea } from "@/components/ui/textarea";
import { usePatientSearch } from "@/lib/hooks/use-patients";
import { useServices, useServicePackages } from "@/lib/hooks/use-services";
import { createServiceOnly } from "@/lib/api/reception";
import { uploadFile } from "@/lib/api/files";
import { formatCurrency } from "@/lib/utils/format";
import type { PatientResponse } from "@/lib/api/types";

const CATEGORY_LABELS: Record<string, string> = {
  EXAM: "Khám",
  CONSULTATION: "Khám / tư vấn",
  LAB: "Xét nghiệm",
  RAD: "Chẩn đoán hình ảnh",
  "RAD-IMAGING": "Chẩn đoán hình ảnh",
  ECG: "Điện tim",
  XRAY: "X-quang",
  PROCEDURE: "Thủ thuật",
  OTHER: "Dịch vụ khác",
};
const catLabel = (c: string) => CATEGORY_LABELS[c] ?? "Dịch vụ khác";

const ERROR_MESSAGES: Record<string, string> = {
  PATIENT_NOT_FOUND: "Không tìm thấy bệnh nhân",
  SERVICE_NOT_FOUND: "Có dịch vụ không tồn tại hoặc đã ngừng sử dụng",
  SERVICE_PACKAGE_NOT_FOUND: "Không tìm thấy gói dịch vụ",
  SERVICE_ONLY_EMPTY: "Vui lòng chọn ít nhất một dịch vụ hoặc gói dịch vụ",
  SERVICE_PACKAGE_EXPIRED: "Gói dịch vụ đã hết hạn",
};

function errorMessage(err: unknown): string {
  const e = (err as { response?: { data?: { error?: { code?: string; message?: string } } } })
    ?.response?.data?.error;
  if (e?.code && ERROR_MESSAGES[e.code]) return ERROR_MESSAGES[e.code];
  return e?.message ?? "Không thể tạo chỉ định dịch vụ";
}

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Bệnh nhân chọn sẵn (nếu có). */
  initialPatient?: PatientResponse | null;
}

export function ServiceOnlyDialog({ open, onOpenChange, initialPatient }: Props) {
  const qc = useQueryClient();
  const [patient, setPatient] = useState<PatientResponse | null>(null);
  const [searchQ, setSearchQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [serviceIds, setServiceIds] = useState<string[]>([]);
  const [packageIds, setPackageIds] = useState<string[]>([]);
  const [note, setNote] = useState("");
  const [files, setFiles] = useState<File[]>([]);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(searchQ), 300);
    return () => clearTimeout(t);
  }, [searchQ]);

  useEffect(() => {
    if (open) {
      setPatient(initialPatient ?? null);
    } else {
      setSearchQ("");
      setServiceIds([]);
      setPackageIds([]);
      setNote("");
      setFiles([]);
    }
  }, [open, initialPatient]);

  const { data: searchResults, isFetching: isSearching } = usePatientSearch(
    { q: debouncedQ, page_size: 6 },
    open && !patient && debouncedQ.length >= 2
  );
  const { data: servicesRes, isLoading: loadingServices } = useServices({
    is_active: true,
    page_size: 500,
  });
  const { data: packagesRes } = useServicePackages({ is_active: true });

  const grouped = useMemo(() => {
    const map = new Map<string, NonNullable<typeof servicesRes>["data"]>();
    for (const s of servicesRes?.data ?? []) {
      if (!s.is_active) continue;
      const list = map.get(s.category) ?? [];
      list.push(s);
      map.set(s.category, list);
    }
    return Array.from(map.entries());
  }, [servicesRes]);

  const toggle = (list: string[], set: (v: string[]) => void, id: string) =>
    set(list.includes(id) ? list.filter((x) => x !== id) : [...list, id]);

  const submit = useMutation({
    mutationFn: async () => {
      const externalFileIds: string[] = [];
      for (const f of files) {
        const up = await uploadFile(f, "EXTERNAL_CLS");
        externalFileIds.push(up.id);
      }
      return createServiceOnly({
        patientId: patient!.id,
        serviceIds,
        servicePackageIds: packageIds,
        note: note.trim() || undefined,
        externalFileIds: externalFileIds.length ? externalFileIds : undefined,
      });
    },
    onSuccess: (res) => {
      qc.invalidateQueries({ queryKey: ["billings"] });
      toast.success(
        <span>
          Đã tạo chỉ định dịch vụ, mã hoá đơn {res.billNo}.{" "}
          <Link href={`/billings/${res.billingId}`} className="underline font-medium">
            Xem hoá đơn
          </Link>
        </span>
      );
      onOpenChange(false);
    },
    onError: (err) => toast.error(errorMessage(err)),
  });

  const canSubmit =
    !!patient && (serviceIds.length > 0 || packageIds.length > 0) && !submit.isPending;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Chỉ định dịch vụ (không khám)</DialogTitle>
          <DialogDescription>
            Tạo hoá đơn nháp cho dịch vụ không cần lượt khám của bác sĩ.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-2">
            <Label>Bệnh nhân</Label>
            {patient ? (
              <div className="flex items-center justify-between rounded-md border px-3 py-2 min-h-11">
                <span className="text-sm">
                  <span className="font-medium">{patient.full_name}</span>{" "}
                  <span className="text-muted-foreground">({patient.code})</span>
                </span>
                <Button type="button" variant="ghost" size="sm" onClick={() => setPatient(null)}>
                  Đổi
                </Button>
              </div>
            ) : (
              <>
                <div className="relative">
                  <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                  <Input
                    className="pl-9 h-11"
                    placeholder="Tìm theo tên, SĐT, mã bệnh nhân..."
                    value={searchQ}
                    onChange={(e) => setSearchQ(e.target.value)}
                  />
                </div>
                {isSearching && <p className="text-xs text-muted-foreground">Đang tìm...</p>}
                {searchResults?.data && searchResults.data.length > 0 && (
                  <ul className="rounded-md border divide-y">
                    {searchResults.data.map((p) => (
                      <li key={p.id}>
                        <button
                          type="button"
                          className="w-full text-left px-3 py-2 min-h-11 text-sm hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring"
                          onClick={() => setPatient(p)}
                        >
                          <span className="font-medium">{p.full_name}</span>{" "}
                          <span className="text-muted-foreground">
                            {p.code}
                            {p.phone ? ` · ${p.phone}` : ""}
                          </span>
                        </button>
                      </li>
                    ))}
                  </ul>
                )}
              </>
            )}
          </div>

          <div className="space-y-2">
            <Label>Dịch vụ</Label>
            {loadingServices ? (
              <p className="text-sm text-muted-foreground">Đang tải danh mục dịch vụ...</p>
            ) : grouped.length === 0 ? (
              <p className="text-sm text-muted-foreground">Chưa có dịch vụ nào đang hoạt động.</p>
            ) : (
              <div className="rounded-md border max-h-64 overflow-y-auto divide-y">
                {grouped.map(([cat, items]) => (
                  <div key={cat} className="p-2">
                    <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
                      {catLabel(cat)}
                    </p>
                    {items.map((s) => (
                      <label
                        key={s.id}
                        className="flex items-center gap-3 px-1 min-h-11 cursor-pointer text-sm"
                      >
                        <input
                          type="checkbox"
                          className="h-4 w-4"
                          checked={serviceIds.includes(s.id)}
                          onChange={() => toggle(serviceIds, setServiceIds, s.id)}
                        />
                        <span className="flex-1">
                          {s.name} — {formatCurrency(s.price)}
                        </span>
                      </label>
                    ))}
                  </div>
                ))}
              </div>
            )}
          </div>

          {(packagesRes?.data?.length ?? 0) > 0 && (
            <div className="space-y-2">
              <Label>Gói dịch vụ</Label>
              <div className="rounded-md border max-h-40 overflow-y-auto p-2">
                {packagesRes!.data.map((p) => (
                  <label
                    key={p.id}
                    className="flex items-center gap-3 px-1 min-h-11 cursor-pointer text-sm"
                  >
                    <input
                      type="checkbox"
                      className="h-4 w-4"
                      checked={packageIds.includes(p.id)}
                      onChange={() => toggle(packageIds, setPackageIds, p.id)}
                    />
                    <span className="flex-1">
                      {p.name} — {formatCurrency(p.final_price)}
                    </span>
                  </label>
                ))}
              </div>
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="so-note">Ghi chú</Label>
            <Textarea
              id="so-note"
              rows={2}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Ghi chú cho chỉ định (không bắt buộc)"
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor="so-files">Ảnh/scan chỉ định CLS từ nơi khác</Label>
            <Input
              id="so-files"
              type="file"
              multiple
              accept="image/*,application/pdf"
              onChange={(e) => {
                const picked = Array.from(e.target.files ?? []);
                setFiles((prev) => [...prev, ...picked]);
                e.target.value = "";
              }}
            />
            {files.length > 0 && (
              <ul className="space-y-1">
                {files.map((f, i) => (
                  <li key={`${f.name}-${i}`} className="flex items-center gap-2 text-sm">
                    <Paperclip className="h-3.5 w-3.5 text-muted-foreground" />
                    <span className="flex-1 truncate">{f.name}</span>
                    <button
                      type="button"
                      aria-label={`Bỏ tệp ${f.name}`}
                      className="p-1 rounded hover:bg-muted"
                      onClick={() => setFiles(files.filter((_, j) => j !== i))}
                    >
                      <X className="h-4 w-4" />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            Huỷ
          </Button>
          <Button type="button" disabled={!canSubmit} onClick={() => submit.mutate()}>
            {submit.isPending && <Loader2 className="h-4 w-4 mr-2 animate-spin" />}
            Tạo chỉ định
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
