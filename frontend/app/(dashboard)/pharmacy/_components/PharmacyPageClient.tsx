"use client";

import { useState, useCallback } from "react";
import { useSearchParams, useRouter, usePathname } from "next/navigation";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { AdjustmentForm } from "@/components/domain/AdjustmentForm";

// Tab components (lazy-loaded inline)
import { StockTab } from "./StockTab";
import { WarehouseTab } from "./WarehouseTab";
import { DispenseTab } from "./DispenseTab";
import { AdjustmentTab } from "./AdjustmentTab";
import { AlertsTab } from "./AlertsTab";

const VALID_TABS = ["stock", "warehouse", "dispense", "adjustment", "alerts"] as const;
type TabValue = (typeof VALID_TABS)[number];

export function PharmacyPageClient() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  const rawTab = searchParams.get("tab") ?? "stock";
  const activeTab: TabValue = (VALID_TABS as readonly string[]).includes(rawTab)
    ? (rawTab as TabValue)
    : "stock";

  const handleTabChange = useCallback(
    (value: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("tab", value);
      router.replace(`${pathname}?${params.toString()}`, { scroll: false });
    },
    [router, pathname, searchParams]
  );

  // Adjustment dialog state lifted to page level so button is always reachable
  const [adjustOpen, setAdjustOpen] = useState(false);

  return (
    <>
      <Tabs value={activeTab} onValueChange={handleTabChange} className="space-y-4">
        <div className="flex items-center justify-between gap-2 flex-wrap">
          <TabsList className="flex-wrap h-auto">
            <TabsTrigger value="stock">Tồn kho</TabsTrigger>
            <TabsTrigger value="warehouse">Nhập kho</TabsTrigger>
            <TabsTrigger value="dispense">Phát thuốc</TabsTrigger>
            <TabsTrigger value="adjustment">Điều chỉnh</TabsTrigger>
            <TabsTrigger value="alerts">Cảnh báo</TabsTrigger>
          </TabsList>

          {/* Button always visible so spec can find it regardless of active tab */}
          <Button
            onClick={() => setAdjustOpen(true)}
            className="min-h-[44px]"
            aria-label="Tạo điều chỉnh tồn kho"
          >
            <Plus className="h-4 w-4 mr-2" />
            Tạo điều chỉnh
          </Button>
        </div>

        <TabsContent value="stock">
          <StockTab />
        </TabsContent>
        <TabsContent value="warehouse">
          <WarehouseTab />
        </TabsContent>
        <TabsContent value="dispense">
          <DispenseTab />
        </TabsContent>
        <TabsContent value="adjustment">
          <AdjustmentTab externalOpen={adjustOpen} onExternalOpenChange={setAdjustOpen} />
        </TabsContent>
        <TabsContent value="alerts">
          <AlertsTab />
        </TabsContent>
      </Tabs>

      {/* Render dialog outside tabs so it works even when adjustment tab is not active */}
      {activeTab !== "adjustment" && (
        <Dialog open={adjustOpen} onOpenChange={setAdjustOpen}>
          <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Tạo điều chỉnh tồn kho</DialogTitle>
            </DialogHeader>
            <AdjustmentForm onSuccess={() => setAdjustOpen(false)} />
          </DialogContent>
        </Dialog>
      )}
    </>
  );
}
