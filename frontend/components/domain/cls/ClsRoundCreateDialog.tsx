"use client";

import { useMemo, useState } from "react";
import { BookmarkPlus, Loader2, Plus, Search, Trash2 } from "lucide-react";
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
import { Checkbox } from "@/components/ui/checkbox";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useClsCatalog } from "@/lib/hooks/use-cls-orders";
import {
  useClsOrderTemplates,
  useCreateClsOrderTemplate,
  useDeleteClsOrderTemplate,
} from "@/lib/hooks/use-cls-order-templates";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { formatVnd } from "@/lib/utils/encounter-format";
import type { ClsCatalogItem } from "@/lib/api/types";
import type { CreateClsRoundRequest } from "@/lib/api/cls-rounds";

export interface ClsRoundCreateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  isPending?: boolean;
  onSubmit: (body: CreateClsRoundRequest) => void;
}

export function ClsRoundCreateDialog({
  open,
  onOpenChange,
  isPending,
  onSubmit,
}: ClsRoundCreateDialogProps) {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 300);
  const [cart, setCart] = useState<ClsCatalogItem[]>([]);
  // BM-16: dich vu nao duoc tick "Khong thu phi" ngay luc tao dot (theo ma dich vu).
  // Doc lap voi nut "Mien phi" ap dung ca dot o buoc Chot (giu nguyen, khong doi).
  const [freeCodes, setFreeCodes] = useState<Set<string>>(new Set());
  const [note, setNote] = useState("");
  // BM-12: bo chi dinh mau (rieng tung bac si) - chon 1 mau co san de nap nhanh vao gio,
  // hoac luu gio hien tai thanh mau moi de dung lai lan sau (khong lien quan freeCodes cua
  // "Mien phi ca dot" o Chot, van luu "Khong thu phi" tung dich vu neu co tick).
  const [templateId, setTemplateId] = useState("");
  const [saveTemplateOpen, setSaveTemplateOpen] = useState(false);
  const [templateName, setTemplateName] = useState("");

  const { data: catalog, isLoading } = useClsCatalog({ q: debouncedQuery, limit: 20 });
  const { data: templates, isLoading: isLoadingTemplates } = useClsOrderTemplates();
  const createTemplate = useCreateClsOrderTemplate();
  const deleteTemplate = useDeleteClsOrderTemplate();

  const total = useMemo(
    () =>
      cart.reduce(
        (sum, item) => sum + (freeCodes.has(item.code) ? 0 : item.default_price ?? 0),
        0
      ),
    [cart, freeCodes]
  );

  // BM-13: dich vu vua chon chen len DAU danh sach "Dich vu da chon" (bac si thay ngay
  // khong can cuon); bam lai dich vu da co trong gio thi bao "Chi dinh da duoc chon"
  // thay vi im lang khong lam gi (de bac si biet la he thong DA ghi nhan, khong phai loi).
  function addItem(item: ClsCatalogItem) {
    setCart((prev) => {
      if (prev.some((x) => x.code === item.code)) {
        toast.info(`"${item.name}" - Chỉ định đã được chọn`);
        return prev;
      }
      return [item, ...prev];
    });
  }

  function removeItem(code: string) {
    setCart((prev) => prev.filter((x) => x.code !== code));
    setFreeCodes((prev) => {
      if (!prev.has(code)) return prev;
      const next = new Set(prev);
      next.delete(code);
      return next;
    });
  }

  function toggleFree(code: string, checked: boolean) {
    setFreeCodes((prev) => {
      const next = new Set(prev);
      if (checked) next.add(code);
      else next.delete(code);
      return next;
    });
  }

  function reset() {
    setQuery("");
    setCart([]);
    setFreeCodes(new Set());
    setNote("");
    setTemplateId("");
  }

  // BM-12: nap mot bo mau da luu vao gio hien tai - CONG DON vao gio (khong thay the) de
  // bac si co the ghep nhieu mau/dich vu le trong 1 dot, tru trung ma da co san.
  function applyTemplate(id: string) {
    const tpl = templates?.find((t) => t.id === id);
    if (!tpl) return;
    setTemplateId(id);
    let addedCount = 0;
    setCart((prev) => {
      const existingCodes = new Set(prev.map((x) => x.code));
      const toAdd = tpl.items.filter((x) => !existingCodes.has(x.code));
      addedCount = toAdd.length;
      return [...toAdd, ...prev];
    });
    setFreeCodes((prev) => {
      const next = new Set(prev);
      tpl.items.forEach((x) => { if (x.is_free) next.add(x.code); });
      return next;
    });
    if (addedCount < tpl.items.length) {
      toast.info(`Đã bỏ qua ${tpl.items.length - addedCount} dịch vụ trùng đã có trong giỏ`);
    }
  }

  function handleSaveTemplate() {
    const name = templateName.trim();
    if (!name || cart.length === 0) return;
    createTemplate.mutate(
      {
        name,
        items: cart.map((x) => ({ ...x, is_free: freeCodes.has(x.code) })),
      },
      {
        onSuccess: () => {
          setSaveTemplateOpen(false);
          setTemplateName("");
        },
      }
    );
  }

  function handleSubmit() {
    const body: CreateClsRoundRequest = {
      note: note.trim() || undefined,
      lab_tests: cart
        .filter((x) => x.kind === "LAB")
        .map((x) => ({
          test_code: x.code,
          test_name: x.name,
          sample_type: x.sample_type ?? undefined,
          priority: "NORMAL",
          is_free: freeCodes.has(x.code),
        })),
      rad_orders: cart
        .filter((x) => x.kind === "RAD")
        .map((x) => ({
          modality: x.modality ?? "XRAY",
          contrast: false,
          procedure_code: x.code,
          procedure_name: x.name,
          priority: "NORMAL",
          is_free: freeCodes.has(x.code),
        })),
    };
    onSubmit(body);
    reset();
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(v) => {
        if (!v) reset();
        onOpenChange(v);
      }}
    >
      <DialogContent className="sm:max-w-4xl">
        <DialogHeader>
          {/* BM-11: bo "cận lâm sàng" thua - dot chi dinh gom ca XN lan CDHA, khong rieng CLS */}
          <DialogTitle>Tạo đợt chỉ định</DialogTitle>
          <DialogDescription>
            Chọn dịch vụ xét nghiệm / chẩn đoán hình ảnh cho đợt chỉ định này.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 md:grid-cols-[1.2fr_1fr]">
          {/* Trái — tìm dịch vụ */}
          <div className="space-y-2">
            <Label htmlFor="cls-search">Tìm dịch vụ</Label>
            <div className="relative">
              <Search
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <Input
                id="cls-search"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Nhập mã hoặc tên dịch vụ..."
                className="min-h-[44px] pl-9"
              />
            </div>
            <div className="max-h-96 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
              {isLoading ? (
                [1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)
              ) : !catalog || catalog.length === 0 ? (
                <p className="p-3 text-sm text-muted-foreground">
                  {debouncedQuery
                    ? "Không tìm thấy dịch vụ phù hợp."
                    : "Nhập từ khoá để tìm dịch vụ."}
                </p>
              ) : (
                catalog.map((item) => (
                  <button
                    key={`${item.kind}-${item.code}`}
                    type="button"
                    onClick={() => addItem(item)}
                    className="flex min-h-[44px] w-full items-center gap-2 rounded-md px-2 text-left hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]"
                  >
                    <Plus className="h-4 w-4 shrink-0" aria-hidden="true" />
                    <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                    <span className="flex-1 line-clamp-2 text-sm">{item.name}</span>
                    <span className="shrink-0 text-xs text-muted-foreground">
                      {item.kind === "LAB" ? "XN" : "CĐHA"}
                    </span>
                    <span className="font-mono text-xs tabular-nums">
                      {formatVnd(item.default_price)} ₫
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>

          {/* Phải — giỏ dịch vụ */}
          <div className="space-y-2">
            {/* BM-12: chon nhanh bo chi dinh mau da luu (rieng cua bac si dang dang nhap) */}
            {(isLoadingTemplates || (templates && templates.length > 0)) && (
              <div className="flex items-center gap-2">
                <Select
                  value={templateId}
                  onValueChange={(v) => { if (v) applyTemplate(v); }}
                  disabled={isLoadingTemplates}
                >
                  <SelectTrigger className="h-9 flex-1 text-xs">
                    <SelectValue placeholder="Nạp từ bộ chỉ định mẫu..." />
                  </SelectTrigger>
                  <SelectContent>
                    {templates?.map((tpl) => (
                      <SelectItem key={tpl.id} value={tpl.id}>
                        {tpl.name} ({tpl.items.length} dịch vụ)
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                {templateId && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-9 text-destructive"
                    disabled={deleteTemplate.isPending}
                    onClick={() => {
                      deleteTemplate.mutate(templateId);
                      setTemplateId("");
                    }}
                  >
                    <Trash2 className="h-3.5 w-3.5" aria-hidden="true" />
                  </Button>
                )}
              </div>
            )}
            <div className="flex items-center justify-between">
              <Label>Dịch vụ đã chọn ({cart.length})</Label>
              <Button
                variant="outline"
                size="sm"
                className="h-7 gap-1 text-xs"
                disabled={cart.length === 0}
                onClick={() => setSaveTemplateOpen(true)}
              >
                <BookmarkPlus className="h-3.5 w-3.5" aria-hidden="true" />
                Lưu bộ mẫu
              </Button>
            </div>
            <div className="max-h-56 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
              {cart.length === 0 ? (
                <p className="p-3 text-sm text-muted-foreground">Chưa chọn dịch vụ nào.</p>
              ) : (
                cart.map((item) => {
                  const isFree = freeCodes.has(item.code);
                  return (
                    <div key={item.code} className="flex items-center gap-2 rounded-md px-2 py-1">
                      <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                      <span className="flex-1 line-clamp-2 text-sm">{item.name}</span>
                      <span
                        className={
                          "font-mono text-xs tabular-nums" +
                          (isFree ? " text-muted-foreground line-through" : "")
                        }
                      >
                        {formatVnd(item.default_price)} ₫
                      </span>
                      <label className="flex shrink-0 items-center gap-1.5 text-xs text-muted-foreground">
                        <Checkbox
                          checked={isFree}
                          onCheckedChange={(v) => toggleFree(item.code, v === true)}
                          aria-label={`Không thu phí dịch vụ ${item.name}`}
                        />
                        Không thu phí
                      </label>
                      <Button
                        variant="ghost"
                        size="sm"
                        className="text-destructive"
                        onClick={() => removeItem(item.code)}
                        aria-label={`Bỏ dịch vụ ${item.name}`}
                      >
                        <Trash2 className="h-4 w-4" aria-hidden="true" />
                      </Button>
                    </div>
                  );
                })
              )}
            </div>
            <div className="flex items-center justify-between text-sm font-semibold">
              <span>Tổng tiền</span>
              <span className="font-mono tabular-nums">
                {formatVnd(total)} <span aria-label="đồng">₫</span>
              </span>
            </div>
            <div className="space-y-1">
              <Label htmlFor="cls-round-note">Ghi chú</Label>
              <Textarea
                id="cls-round-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder="Ghi chú cho đợt chỉ định..."
                rows={2}
              />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Huỷ
          </Button>
          <Button onClick={handleSubmit} disabled={cart.length === 0 || isPending} className="gap-2">
            {isPending && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
            {isPending ? "Đang lưu…" : "Lưu đợt chỉ định"}
          </Button>
        </DialogFooter>
      </DialogContent>

      {/* BM-12: luu gio hien tai thanh bo chi dinh mau (rieng cua bac si dang dang nhap) */}
      <ConfirmDialog
        open={saveTemplateOpen}
        onOpenChange={setSaveTemplateOpen}
        title="Lưu bộ chỉ định mẫu"
        description={
          <div className="space-y-2">
            <p>Đặt tên cho bộ mẫu gồm {cart.length} dịch vụ hiện tại:</p>
            <Input
              autoFocus
              placeholder="Ví dụ: Xét nghiệm tiền phẫu..."
              value={templateName}
              onChange={(e) => setTemplateName(e.target.value)}
            />
          </div>
        }
        confirmLabel="Lưu"
        isLoading={createTemplate.isPending}
        onConfirm={handleSaveTemplate}
      />
    </Dialog>
  );
}
