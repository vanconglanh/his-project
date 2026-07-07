"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Trash2, Edit2 } from "lucide-react";
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
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  useEmergencyContacts,
  useAddEmergencyContact,
  useUpdateEmergencyContact,
  useDeleteEmergencyContact,
} from "@/lib/hooks/use-patients";
import type { Relationship, EmergencyContactResponse } from "@/lib/api/types";

const PHONE_VN = /^(\+84|0)\d{9,10}$/;

const contactSchema = z.object({
  full_name: z.string().min(1, "Họ tên không được để trống"),
  relationship: z.enum(["FATHER", "MOTHER", "SPOUSE", "CHILD", "SIBLING", "OTHER"]),
  phone: z.string().regex(PHONE_VN, "Số điện thoại không hợp lệ"),
  address: z.string().optional(),
});

type ContactFormValues = z.infer<typeof contactSchema>;

const RELATIONSHIP_LABELS: Record<Relationship, string> = {
  FATHER: "Cha",
  MOTHER: "Mẹ",
  SPOUSE: "Vợ/Chồng",
  CHILD: "Con",
  SIBLING: "Anh/Chị/Em",
  OTHER: "Khác",
};

interface EmergencyContactListProps {
  patientId: string;
}

export function EmergencyContactList({ patientId }: EmergencyContactListProps) {
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<EmergencyContactResponse | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: contacts, isLoading } = useEmergencyContacts(patientId);
  const addMutation = useAddEmergencyContact(patientId);
  const updateMutation = useUpdateEmergencyContact(patientId);
  const deleteMutation = useDeleteEmergencyContact(patientId);

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } = useForm<ContactFormValues>({
    resolver: zodResolver(contactSchema),
    defaultValues: { relationship: "OTHER" },
  });

  const startEdit = (contact: EmergencyContactResponse) => {
    setEditItem(contact);
    setShowForm(true);
    setValue("full_name", contact.full_name);
    setValue("relationship", contact.relationship);
    setValue("phone", contact.phone);
    setValue("address", contact.address ?? "");
  };

  const onSubmit = async (values: ContactFormValues) => {
    if (editItem) {
      await updateMutation.mutateAsync({ contactId: editItem.id, body: values });
    } else {
      await addMutation.mutateAsync(values);
    }
    reset({ relationship: "OTHER" });
    setShowForm(false);
    setEditItem(null);
  };

  const closeForm = () => {
    reset({ relationship: "OTHER" });
    setShowForm(false);
    setEditItem(null);
  };

  if (isLoading) {
    return <div className="space-y-2">{[1, 2].map((i) => <Skeleton key={i} className="h-12 w-full" />)}</div>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Liên hệ khẩn cấp</h3>
        <Button size="sm" variant="outline" onClick={() => setShowForm(!showForm)} className="gap-1 h-8">
          <Plus className="h-3 w-3" />
          Thêm liên hệ
        </Button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="border rounded-lg p-4 space-y-3 bg-muted/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Họ và tên *</Label>
              <Input {...register("full_name")} placeholder="Nguyễn Thị B" />
              {errors.full_name && <p className="text-xs text-destructive">{errors.full_name.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Mối quan hệ *</Label>
              <Select
                items={RELATIONSHIP_LABELS}
                value={watch("relationship")}
                onValueChange={(v) => setValue("relationship", v as Relationship)}
              >
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {Object.entries(RELATIONSHIP_LABELS).map(([k, label]) => (
                    <SelectItem key={k} value={k}>{label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>Điện thoại *</Label>
              <Input {...register("phone")} placeholder="0987654321" />
              {errors.phone && <p className="text-xs text-destructive">{errors.phone.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Địa chỉ</Label>
              <Input {...register("address")} placeholder="12 Hàng Bài, HN" />
            </div>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={addMutation.isPending || updateMutation.isPending}>
              {addMutation.isPending || updateMutation.isPending ? "Đang lưu..." : editItem ? "Cập nhật" : "Lưu"}
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={closeForm}>Huỷ</Button>
          </div>
        </form>
      )}

      {!contacts || contacts.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">Chưa có liên hệ khẩn cấp</div>
      ) : (
        <div className="space-y-2">
          {contacts.map((c) => (
            <div key={c.id} className="flex items-center justify-between p-3 border rounded-lg">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium text-sm">{c.full_name}</span>
                  <span className="text-xs text-muted-foreground">({RELATIONSHIP_LABELS[c.relationship]})</span>
                </div>
                <p className="text-xs text-muted-foreground">{c.phone}{c.address ? ` • ${c.address}` : ""}</p>
              </div>
              <div className="flex gap-1">
                <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => startEdit(c)} aria-label="Sửa">
                  <Edit2 className="h-3.5 w-3.5" />
                </Button>
                <Button variant="ghost" size="icon" className="h-7 w-7 hover:text-destructive" onClick={() => setDeleteId(c.id)} aria-label="Xoá">
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null); }}
        title="Xoá liên hệ khẩn cấp"
        description="Bạn có chắc muốn xoá liên hệ này không?"
        onConfirm={() => { if (deleteId) deleteMutation.mutate(deleteId); setDeleteId(null); }}
        isLoading={deleteMutation.isPending}
        variant="destructive"
      />
    </div>
  );
}
