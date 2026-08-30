"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Search, UserPlus, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCheckIn, useRooms } from "@/lib/hooks/use-reception";
import {
  usePatient,
  usePatientSearch,
  useCheckCccdDuplicate,
  useApplyCccdFields,
} from "@/lib/hooks/use-patients";
import { getPatient } from "@/lib/api/patients";
import { usePatientPackageSummary } from "@/lib/hooks/use-packages";
import type { PatientResponse, TicketPriority, CccdDuplicateCheckResult } from "@/lib/api/types";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { formatDate } from "@/lib/utils/format";
import { PackageCheck } from "lucide-react";
import { CccdQrScanner } from "@/components/domain/CccdQrScanner";
import { CccdMismatchDialog, type CccdFieldUpdateSelection } from "@/components/domain/CccdMismatchDialog";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { AlertCircle } from "lucide-react";
import { toast } from "sonner";
import type { CccdQrData } from "@/lib/utils/cccd-qr";

/** sessionStorage key lưu dữ liệu đã quét CCCD để prefill /patients/new (dùng 1 lần). */
const CCCD_PREFILL_KEY = "reception-cccd-prefill";

const checkInSchema = z.object({
  patient_id: z.string().min(1, "Chọn bệnh nhân"),
  room_id: z.string().min(1, "Chọn phòng khám"),
  reason_for_visit: z.string().optional(),
  note: z.string().optional(),
  priority: z.enum(["NORMAL", "PRIORITY", "EMERGENCY"]),
});

type CheckInFormValues = z.infer<typeof checkInSchema>;

/** sessionStorage key lưu draft form check-in khi điều hướng sang tạo bệnh nhân mới (dùng 1 lần). */
const CHECKIN_DRAFT_KEY = "reception-checkin-draft";

type CheckInDraft = Pick<CheckInFormValues, "room_id" | "reason_for_visit" | "note" | "priority">;

interface ReceptionCheckInFormProps {
  /** id bệnh nhân vừa tạo ở `/patients/new`, cần tự chọn lại khi quay về tiếp đón. */
  preselectPatientId?: string;
}

export function ReceptionCheckInForm({ preselectPatientId }: ReceptionCheckInFormProps) {
  const router = useRouter();
  const [searchQ, setSearchQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [showSearchList, setShowSearchList] = useState(false);

  const { data: rooms } = useRooms();
  const checkInMutation = useCheckIn();
  const { data: preselectedPatient } = usePatient(preselectPatientId ?? "");
  const { data: packageSummary, isLoading: isPackageLoading } = usePatientPackageSummary(
    selectedPatient?.id
  );

  // ── Quét QR CCCD tại quầy tiếp đón (US-QR-001..005) ──
  const [exactMatch, setExactMatch] = useState<CccdDuplicateCheckResult | null>(null);
  const [mismatch, setMismatch] = useState<CccdDuplicateCheckResult | null>(null);
  const [scannedIdNumber, setScannedIdNumber] = useState<string>("");
  const checkCccdDuplicate = useCheckCccdDuplicate();
  const applyCccdFields = useApplyCccdFields(mismatch?.patient_id ?? "");

  const { data: searchResults, isFetching: isSearching } = usePatientSearch(
    { q: debouncedQ, page_size: 8 },
    debouncedQ.length >= 2
  );

  // Debounce search
  useEffect(() => {
    const t = setTimeout(() => setDebouncedQ(searchQ), 300);
    return () => clearTimeout(t);
  }, [searchQ]);

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } =
    useForm<CheckInFormValues>({
      resolver: zodResolver(checkInSchema),
      defaultValues: { priority: "NORMAL" },
    });

  const onSubmit = async (values: CheckInFormValues) => {
    await checkInMutation.mutateAsync(values);
    sessionStorage.removeItem(CHECKIN_DRAFT_KEY);
    reset({ priority: "NORMAL" });
    setSelectedPatient(null);
    setSearchQ("");
  };

  const selectPatient = useCallback(
    (patient: PatientResponse) => {
      setSelectedPatient(patient);
      setValue("patient_id", patient.id);
      setSearchQ(patient.full_name);
      setShowSearchList(false);
    },
    [setValue]
  );

  // US-QR-001: quét CCCD tại quầy tiếp đón trước khi điều hướng sang /patients/new (GA-004:
  // form này chỉ có ô tìm bệnh nhân, không có field họ tên/ngày sinh riêng để tự điền trực tiếp).
  const handleCccdScanned = useCallback(
    async (data: CccdQrData) => {
      if (data.has_encoding_warning) {
        toast.warning("Có thể có lỗi encoding — vui lòng kiểm tra lại họ tên / địa chỉ");
      }
      if (!data.id_number) return;
      setScannedIdNumber(data.id_number);

      try {
        const result = await checkCccdDuplicate.mutateAsync({
          id_number: data.id_number,
          full_name: data.full_name ?? undefined,
          date_of_birth: data.date_of_birth ?? undefined,
          gender: data.gender ?? undefined,
          address: data.address ?? undefined,
        });

        if (result.case === "NONE") {
          // BR-DUP-002: chưa tồn tại -> lưu draft prefill rồi sang /patients/new tạo mới
          sessionStorage.setItem(CCCD_PREFILL_KEY, JSON.stringify(data));
          saveDraftAndCreatePatient();
        } else if (result.case === "EXACT_MATCH") {
          setExactMatch(result);
        } else if (result.case === "FIELD_MISMATCH") {
          setMismatch(result);
        }
      } catch {
        // Loi check trung khong nen chan luong — le tan van co the tao thu cong (da toast trong hook)
      }
    },
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [checkCccdDuplicate]
  );

  const openExactMatchPatient = async () => {
    if (!exactMatch?.patient_id) return;
    const patient = await getPatient(exactMatch.patient_id);
    selectPatient(patient);
    setExactMatch(null);
  };

  const handleSaveMismatch = (fields: CccdFieldUpdateSelection[]) => {
    if (!mismatch?.patient_id) return;
    if (fields.length === 0) {
      setMismatch(null);
      return;
    }
    applyCccdFields.mutate(fields, {
      onSuccess: async () => {
        const patient = await getPatient(mismatch.patient_id!);
        selectPatient(patient);
        setMismatch(null);
      },
    });
  };

  // Quay về từ /patients/new với bệnh nhân vừa tạo → tự chọn (parity với Dialog cũ)
  useEffect(() => {
    if (preselectedPatient) {
      selectPatient(preselectedPatient);
    }
  }, [preselectedPatient, selectPatient]);

  // Khôi phục draft check-in đã lưu trước khi điều hướng sang tạo bệnh nhân mới (dùng 1 lần)
  useEffect(() => {
    if (!preselectPatientId) return;
    const raw = sessionStorage.getItem(CHECKIN_DRAFT_KEY);
    if (!raw) return;
    sessionStorage.removeItem(CHECKIN_DRAFT_KEY);
    try {
      const draft = JSON.parse(raw) as Partial<CheckInDraft>;
      if (draft.room_id) setValue("room_id", draft.room_id);
      if (draft.reason_for_visit) setValue("reason_for_visit", draft.reason_for_visit);
      if (draft.note) setValue("note", draft.note);
      if (draft.priority) setValue("priority", draft.priority);
    } catch {
      // draft hỏng — bỏ qua, không chặn luồng tiếp đón
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [preselectPatientId]);

  const saveDraftAndCreatePatient = () => {
    setShowSearchList(false);
    const draft: CheckInDraft = {
      room_id: watch("room_id"),
      reason_for_visit: watch("reason_for_visit"),
      note: watch("note"),
      priority: watch("priority"),
    };
    sessionStorage.setItem(CHECKIN_DRAFT_KEY, JSON.stringify(draft));
    router.push("/patients/new?returnTo=/reception");
  };

  return (
    <>
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <h3 className="font-semibold mb-3">Tiếp đón bệnh nhân</h3>
      </div>

      {/* Quét CCCD để tự động tìm/tạo bệnh nhân (US-QR-001) */}
      <div data-tour="reception-qr-scan">
        <CccdQrScanner onScanned={handleCccdScanned} />
      </div>

      {/* Patient search */}
      <div className="space-y-1">
        <Label>Bệnh nhân <span className="text-destructive">*</span></Label>
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
          <Input
            value={searchQ}
            onChange={(e) => {
              setSearchQ(e.target.value);
              setShowSearchList(true);
              if (!e.target.value) {
                setSelectedPatient(null);
                setValue("patient_id", "");
              }
            }}
            onFocus={() => setShowSearchList(true)}
            className="pl-9"
            placeholder="Tìm tên, SĐT, CMND, BHYT..."
            role="combobox"
            aria-autocomplete="list"
            aria-expanded={showSearchList && debouncedQ.length >= 2}
            aria-controls="patient-search-listbox"
          />
          {isSearching && (
            <Loader2 className="absolute right-3 top-1/2 -translate-y-1/2 h-4 w-4 animate-spin text-muted-foreground" />
          )}
        </div>
        {errors.patient_id && (
          <p className="text-xs text-destructive">{errors.patient_id.message}</p>
        )}

        {/* Search dropdown */}
        {showSearchList && debouncedQ.length >= 2 && (
          <div
            id="patient-search-listbox"
            role="listbox"
            aria-label="Kết quả tìm bệnh nhân"
            className="border rounded-md shadow-lg bg-popover z-10 absolute w-full max-w-sm"
          >
            {searchResults?.data && searchResults.data.length > 0 ? (
              <ul className="max-h-52 overflow-y-auto divide-y">
                {searchResults.data.map((p) => (
                  <li key={p.id}>
                    <button
                      type="button"
                      role="option"
                      aria-selected={selectedPatient?.id === p.id}
                      className="w-full text-left px-3 py-2 hover:bg-muted text-sm"
                      onClick={() => selectPatient(p)}
                    >
                      <span className="font-medium">{p.full_name}</span>
                      <span className="text-xs text-muted-foreground ml-2">
                        {p.code} • {p.phone}
                      </span>
                    </button>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="p-3 text-sm text-muted-foreground">Không tìm thấy bệnh nhân</div>
            )}
            <div className="border-t p-2">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="w-full gap-1.5 text-xs"
                onClick={saveDraftAndCreatePatient}
              >
                <UserPlus className="h-3.5 w-3.5" />
                Tạo bệnh nhân mới
              </Button>
            </div>
          </div>
        )}

        {selectedPatient && (
          <div className="bg-muted/40 rounded-md px-3 py-2 text-sm">
            <span className="font-medium">{selectedPatient.full_name}</span>
            <span className="text-muted-foreground ml-2 text-xs">
              {selectedPatient.code} • {selectedPatient.phone}
            </span>
          </div>
        )}

        {/* [FR-1205] Tóm tắt gói dịch vụ bệnh nhân đang có, hiển thị real-time định mức còn lại */}
        {selectedPatient && !isPackageLoading && (
          <div className="space-y-1.5">
            {packageSummary && packageSummary.subscriptions.length > 0 ? (
              packageSummary.subscriptions.map((sub) => (
                <div
                  key={sub.id}
                  className="flex items-start gap-2 rounded-md border px-3 py-2 text-xs"
                >
                  <PackageCheck className="h-3.5 w-3.5 mt-0.5 shrink-0 text-primary" />
                  <div className="space-y-1">
                    <p className="font-medium">
                      Gói: {sub.package_name} — HSD {formatDate(sub.expiry_date)}
                    </p>
                    <div className="flex flex-wrap gap-1">
                      {sub.balances.map((b) => (
                        <Badge
                          key={b.item_code}
                          variant={b.is_low ? "destructive" : "secondary"}
                          className="font-normal"
                        >
                          {b.item_name}: {b.display}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>
              ))
            ) : (
              <p className="text-xs text-muted-foreground">Không có gói dịch vụ</p>
            )}
          </div>
        )}
      </div>

      {/* Room selection */}
      <div className="space-y-1">
        <Label>Phòng khám <span className="text-destructive">*</span></Label>
        <div className="grid grid-cols-1 gap-2">
          {(rooms ?? []).map((room) => {
            const isSelected = watch("room_id") === room.id;
            return (
              <label
                key={room.id}
                className={cn(
                  "flex items-center justify-between border rounded-lg p-3 cursor-pointer transition-colors",
                  isSelected ? "border-primary bg-primary/5" : "hover:bg-muted/50"
                )}
              >
                <div className="flex items-center gap-2">
                  <input
                    type="radio"
                    value={room.id}
                    checked={isSelected}
                    onChange={() => setValue("room_id", room.id)}
                    className="sr-only"
                  />
                  <span className="text-sm font-medium">{room.name}</span>
                  {room.on_duty_doctor && (
                    <span className="text-xs text-muted-foreground">
                      {room.on_duty_doctor.full_name}
                    </span>
                  )}
                </div>
                <span className="text-xs text-muted-foreground">
                  Chờ: {room.current_waiting ?? 0}
                </span>
              </label>
            );
          })}
          {(!rooms || rooms.length === 0) && (
            <p className="text-sm text-muted-foreground">Không có phòng khám</p>
          )}
        </div>
        {errors.room_id && (
          <p className="text-xs text-destructive">{errors.room_id.message}</p>
        )}
      </div>

      {/* Priority */}
      <div className="space-y-1">
        <Label>Ưu tiên</Label>
        <Select
          items={{ NORMAL: "Thông thường", PRIORITY: "Ưu tiên", EMERGENCY: "Khẩn cấp" }}
          value={watch("priority")}
          onValueChange={(v) => setValue("priority", v as TicketPriority)}
        >
          <SelectTrigger>
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="NORMAL">Thông thường</SelectItem>
            <SelectItem value="PRIORITY">Ưu tiên</SelectItem>
            <SelectItem value="EMERGENCY">Khẩn cấp</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Reason */}
      <div className="space-y-1">
        <Label>Lý do khám</Label>
        <Input
          {...register("reason_for_visit")}
          placeholder="Đau đầu, sốt 3 ngày..."
        />
      </div>

      {/* Note */}
      <div className="space-y-1">
        <Label>Ghi chú cho lễ tân</Label>
        <Input {...register("note")} placeholder="Bệnh nhân có hẹn..." />
      </div>

      <Button
        type="submit"
        className="w-full"
        disabled={checkInMutation.isPending}
        size="lg"
      >
        {checkInMutation.isPending ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            Đang tiếp đón...
          </>
        ) : (
          "Tiếp đón (F4)"
        )}
      </Button>
    </form>

    {/* US-QR-004 (Case 2 — BR-DUP-003): CCCD đã tồn tại, khớp hoàn toàn */}
    <Dialog open={!!exactMatch} onOpenChange={(o) => !o && setExactMatch(null)}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4 text-amber-500" />
            Bệnh nhân đã có hồ sơ
          </DialogTitle>
          <DialogDescription>
            Số CCCD này đã được đăng ký trong hệ thống. Vui lòng dùng hồ sơ cũ thay vì tạo mới.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-1 text-sm">
          <p>
            <span className="text-muted-foreground">Bệnh nhân: </span>
            <span className="font-medium">{exactMatch?.patient_full_name}</span>
          </p>
          <p>
            <span className="text-muted-foreground">Mã hồ sơ: </span>
            <span className="font-medium">{exactMatch?.patient_code}</span>
          </p>
        </div>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => setExactMatch(null)}>
            Thoát
          </Button>
          <Button type="button" onClick={openExactMatchPatient}>
            Dùng hồ sơ này
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>

    {/* US-QR-005 (Case 3 — BR-DUP-004/005): CCCD tồn tại, có trường lệch */}
    {mismatch && (
      <CccdMismatchDialog
        open={!!mismatch}
        onOpenChange={(o) => !o && setMismatch(null)}
        idNumber={scannedIdNumber}
        diffs={mismatch.field_diffs}
        isSaving={applyCccdFields.isPending}
        onSave={handleSaveMismatch}
      />
    )}
    </>
  );
}
