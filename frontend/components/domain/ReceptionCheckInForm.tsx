"use client";

import { useState, useEffect, useCallback } from "react";
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
import { usePatientSearch } from "@/lib/hooks/use-patients";
import { PatientForm } from "@/components/domain/PatientForm";
import { useCreatePatient } from "@/lib/hooks/use-patients";
import type { PatientResponse, TicketPriority, CreatePatientRequest } from "@/lib/api/types";
import { cn } from "@/lib/utils";

const checkInSchema = z.object({
  patient_id: z.string().min(1, "Chọn bệnh nhân"),
  room_id: z.string().min(1, "Chọn phòng khám"),
  reason_for_visit: z.string().optional(),
  note: z.string().optional(),
  priority: z.enum(["NORMAL", "PRIORITY", "EMERGENCY"]),
});

type CheckInFormValues = z.infer<typeof checkInSchema>;

export function ReceptionCheckInForm() {
  const [searchQ, setSearchQ] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [selectedPatient, setSelectedPatient] = useState<PatientResponse | null>(null);
  const [showPatientCreate, setShowPatientCreate] = useState(false);
  const [showSearchList, setShowSearchList] = useState(false);

  const { data: rooms } = useRooms();
  const checkInMutation = useCheckIn();
  const createPatientMutation = useCreatePatient();

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
    reset({ priority: "NORMAL" });
    setSelectedPatient(null);
    setSearchQ("");
  };

  const selectPatient = (patient: PatientResponse) => {
    setSelectedPatient(patient);
    setValue("patient_id", patient.id);
    setSearchQ(patient.full_name);
    setShowSearchList(false);
  };

  const handleCreatePatient = async (data: CreatePatientRequest) => {
    const newPatient = await createPatientMutation.mutateAsync(data);
    selectPatient(newPatient);
    setShowPatientCreate(false);
  };

  return (
    <>
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <h3 className="font-semibold mb-3">Tiếp đón bệnh nhân</h3>
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
            <div className="border rounded-md shadow-lg bg-popover z-10 absolute w-full max-w-sm">
              {searchResults?.data && searchResults.data.length > 0 ? (
                <ul className="max-h-52 overflow-y-auto divide-y">
                  {searchResults.data.map((p) => (
                    <li key={p.id}>
                      <button
                        type="button"
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
                  onClick={() => {
                    setShowSearchList(false);
                    setShowPatientCreate(true);
                  }}
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

      <PatientForm
        open={showPatientCreate}
        onClose={() => setShowPatientCreate(false)}
        onSubmit={handleCreatePatient}
        isLoading={createPatientMutation.isPending}
      />
    </>
  );
}
