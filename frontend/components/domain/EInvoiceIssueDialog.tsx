"use client";

import { useForm } from "react-hook-form";
import { toast } from "sonner";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useIssueEInvoice } from "@/lib/hooks/use-einvoice";
import type { EInvoiceProvider } from "@/lib/api/einvoice";

interface FormData {
  provider: EInvoiceProvider;
  buyer_name: string;
  buyer_tax_code: string;
  buyer_email: string;
  send_email: boolean;
}

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  billingId: string;
  onSuccess?: () => void;
}

export function EInvoiceIssueDialog({ open, onOpenChange, billingId, onSuccess }: Props) {
  const { register, handleSubmit, setValue, watch, reset } = useForm<FormData>({
    defaultValues: {
      provider: "MISA",
      buyer_name: "",
      buyer_tax_code: "",
      buyer_email: "",
      send_email: true,
    },
  });
  const issueEInvoice = useIssueEInvoice();
  const provider = watch("provider");

  async function onSubmit(values: FormData) {
    try {
      await issueEInvoice.mutateAsync({
        billing_id: billingId,
        provider: values.provider,
        buyer: {
          name: values.buyer_name || undefined,
          tax_code: values.buyer_tax_code || null,
          email: values.buyer_email || null,
        },
        send_email: values.send_email,
      });
      toast.success("Đã phát hành HĐĐT");
      reset();
      onOpenChange(false);
      onSuccess?.();
    } catch {
      toast.error("Phát hành HĐĐT thất bại");
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Phát hành hoá đơn điện tử</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <Label>Nhà cung cấp HĐĐT</Label>
            <Select
              items={{ MISA: "MISA", VNPT: "VNPT", EFY: "EFY" }}
              value={provider}
              onValueChange={(v) => setValue("provider", v as EInvoiceProvider)}
            >
              <SelectTrigger className="mt-1">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="MISA">MISA</SelectItem>
                <SelectItem value="VNPT">VNPT</SelectItem>
                <SelectItem value="EFY">EFY</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div>
            <Label htmlFor="buyer_name">Tên người mua</Label>
            <Input id="buyer_name" {...register("buyer_name")} className="mt-1" />
          </div>
          <div>
            <Label htmlFor="buyer_tax_code">Mã số thuế</Label>
            <Input id="buyer_tax_code" {...register("buyer_tax_code")} className="mt-1" />
          </div>
          <div>
            <Label htmlFor="buyer_email">Email gửi hoá đơn</Label>
            <Input id="buyer_email" type="email" {...register("buyer_email")} className="mt-1" />
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>Huỷ</Button>
            <Button type="submit" disabled={issueEInvoice.isPending}>
              {issueEInvoice.isPending ? "Đang phát hành..." : "Phát hành HĐĐT"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
