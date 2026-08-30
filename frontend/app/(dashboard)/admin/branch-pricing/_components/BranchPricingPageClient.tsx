"use client";

import { useCallback, useMemo, useState } from "react";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Button } from "@/components/ui/button";
import { Plus } from "lucide-react";
import { useBranches } from "@/lib/hooks/use-branches";
import { searchDrugs } from "@/lib/api/drugs";
import { searchServices } from "@/lib/api/services";
import type { PickerOption } from "./ItemPicker";
import { PriceOverrideTable, type PriceOverrideRow } from "./PriceOverrideTable";
import { PriceOverrideFormDialog, type PriceOverrideEditTarget } from "./PriceOverrideFormDialog";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  useServicePriceOverrides,
  useCreateServicePriceOverride,
  useUpdateServicePriceOverride,
  useDeleteServicePriceOverride,
  useDrugPriceOverrides,
  useCreateDrugPriceOverride,
  useUpdateDrugPriceOverride,
  useDeleteDrugPriceOverride,
} from "@/lib/hooks/use-branch-pricing";
import type { ServicePriceOverrideResponse, DrugPriceOverrideResponse } from "@/lib/api/branch-pricing";

async function fetchServiceOptions(q: string): Promise<PickerOption[]> {
  const res = await searchServices(q);
  return res.map((s) => ({ id: s.id, name: s.name, subtitle: s.code }));
}

async function fetchDrugOptions(q: string): Promise<PickerOption[]> {
  if (!q) return [];
  const res = await searchDrugs(q);
  return res.map((d) => ({ id: d.id, name: d.name_vi, subtitle: d.code }));
}

export function BranchPricingPageClient() {
  const [tab, setTab] = useState<"service" | "drug">("service");
  const [page, setPage] = useState(1);
  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<PriceOverrideEditTarget | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<PriceOverrideRow | null>(null);

  const { data: branchesData } = useBranches();
  const branchMap = useMemo(() => {
    const m = new Map<number, string>();
    (branchesData?.data ?? []).forEach((b) => m.set(b.id, b.name));
    return m;
  }, [branchesData]);

  const branchLabel = useCallback(
    (scope: string, branchId: number | null, groupId: number | null) => {
      if (scope === "BRANCH") return branchId != null ? branchMap.get(branchId) ?? `#${branchId}` : "-";
      return groupId != null ? `Nhóm #${groupId}` : "-";
    },
    [branchMap]
  );

  // ─── Dịch vụ ────────────────────────────────────────────────────────────
  const serviceQuery = useServicePriceOverrides({ page, page_size: 20 });
  const createService = useCreateServicePriceOverride();
  const deleteService = useDeleteServicePriceOverride();

  const serviceRows: PriceOverrideRow[] = useMemo(
    () =>
      (serviceQuery.data?.data ?? []).map((o: ServicePriceOverrideResponse) => ({
        id: o.id,
        itemLabel: o.service_name,
        scope: o.scope,
        branch_id: o.branch_id,
        group_id: o.group_id,
        branchLabel: branchLabel(o.scope, o.branch_id, o.group_id),
        price: o.price,
        is_active: o.is_active,
        effective_from: o.effective_from,
        effective_to: o.effective_to,
        note: o.note,
      })),
    [serviceQuery.data, branchLabel]
  );

  // ─── Thuốc ──────────────────────────────────────────────────────────────
  const drugQuery = useDrugPriceOverrides({ page, page_size: 20 });
  const createDrug = useCreateDrugPriceOverride();
  const deleteDrug = useDeleteDrugPriceOverride();

  const drugRows: PriceOverrideRow[] = useMemo(
    () =>
      (drugQuery.data?.data ?? []).map((o: DrugPriceOverrideResponse) => ({
        id: o.id,
        itemLabel: o.drug_name,
        scope: o.scope,
        branch_id: o.branch_id,
        group_id: o.group_id,
        branchLabel: branchLabel(o.scope, o.branch_id, o.group_id),
        price: o.price,
        is_active: o.is_active,
        effective_from: o.effective_from,
        effective_to: o.effective_to,
        note: o.note,
      })),
    [drugQuery.data, branchLabel]
  );

  // Hook update luôn cần id — chỉ tạo khi có editTarget đang mở form sửa.
  const updateService = useUpdateServicePriceOverride(editTarget?.id ?? "");
  const updateDrug = useUpdateDrugPriceOverride(editTarget?.id ?? "");

  function openCreate() {
    setEditTarget(null);
    setFormOpen(true);
  }

  function openEdit(row: PriceOverrideRow) {
    setEditTarget({
      id: row.id,
      itemLabel: row.itemLabel,
      scope: row.scope,
      branch_id: row.branch_id,
      group_id: row.group_id,
      price: row.price,
      is_active: row.is_active,
      effective_from: row.effective_from,
      effective_to: row.effective_to,
      note: row.note,
    });
    setFormOpen(true);
  }

  function handleCreate(values: {
    item_id: string;
    scope: "BRANCH" | "GROUP";
    branch_id?: number;
    group_id?: number;
    price: number;
    is_active: boolean;
    effective_from: string;
    effective_to?: string | null;
    note?: string;
  }) {
    const mutate = tab === "service" ? createService.mutate : createDrug.mutate;
    const body =
      tab === "service"
        ? {
            service_id: values.item_id,
            scope: values.scope,
            branch_id: values.branch_id,
            group_id: values.group_id,
            price: values.price,
            is_active: values.is_active,
            effective_from: values.effective_from,
            effective_to: values.effective_to,
            note: values.note,
          }
        : {
            drug_id: values.item_id,
            scope: values.scope,
            branch_id: values.branch_id,
            group_id: values.group_id,
            price: values.price,
            is_active: values.is_active,
            effective_from: values.effective_from,
            effective_to: values.effective_to,
            note: values.note,
          };
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    mutate(body as any, { onSuccess: () => setFormOpen(false) });
  }

  function handleUpdate(values: {
    price: number;
    is_active: boolean;
    effective_from: string;
    effective_to?: string | null;
    note?: string;
  }) {
    const mutate = tab === "service" ? updateService.mutate : updateDrug.mutate;
    mutate(values, { onSuccess: () => setFormOpen(false) });
  }

  function handleDeleteConfirm() {
    if (!deleteTarget) return;
    const mutate = tab === "service" ? deleteService.mutate : deleteDrug.mutate;
    mutate(deleteTarget.id, { onSuccess: () => setDeleteTarget(null) });
  }

  return (
    <div className="space-y-4">
      <Tabs
        value={tab}
        onValueChange={(v) => {
          setTab(v as "service" | "drug");
          setPage(1);
        }}
      >
        <div className="flex items-center justify-between">
          <TabsList>
            <TabsTrigger value="service">Dịch vụ</TabsTrigger>
            <TabsTrigger value="drug">Thuốc</TabsTrigger>
          </TabsList>
          <Button onClick={openCreate}>
            <Plus className="h-4 w-4" />
            Thêm override
          </Button>
        </div>

        <TabsContent value="service" className="mt-4">
          <PriceOverrideTable
            rows={serviceRows}
            isLoading={serviceQuery.isLoading}
            meta={serviceQuery.data?.meta}
            onPageChange={setPage}
            onEdit={openEdit}
            onDelete={setDeleteTarget}
            emptyLabel="Chưa có override giá dịch vụ nào theo chi nhánh/nhóm chi nhánh."
          />
        </TabsContent>

        <TabsContent value="drug" className="mt-4">
          <PriceOverrideTable
            rows={drugRows}
            isLoading={drugQuery.isLoading}
            meta={drugQuery.data?.meta}
            onPageChange={setPage}
            onEdit={openEdit}
            onDelete={setDeleteTarget}
            emptyLabel="Chưa có override giá thuốc nào theo chi nhánh/nhóm chi nhánh."
          />
        </TabsContent>
      </Tabs>

      <PriceOverrideFormDialog
        open={formOpen}
        onOpenChange={setFormOpen}
        kind={tab === "service" ? "SERVICE" : "DRUG"}
        editTarget={editTarget}
        fetchItemOptions={tab === "service" ? fetchServiceOptions : fetchDrugOptions}
        isSaving={
          createService.isPending || createDrug.isPending || updateService.isPending || updateDrug.isPending
        }
        onCreate={handleCreate}
        onUpdate={handleUpdate}
      />

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(o) => !o && setDeleteTarget(null)}
        title="Xoá override giá"
        description={
          deleteTarget ? (
            <>
              Bạn chắc chắn muốn xoá override giá cho <b>{deleteTarget.itemLabel}</b> (
              {deleteTarget.branchLabel})? Hành động này không thể hoàn tác.
            </>
          ) : (
            ""
          )
        }
        variant="destructive"
        isLoading={deleteService.isPending || deleteDrug.isPending}
        onConfirm={handleDeleteConfirm}
      />
    </div>
  );
}
