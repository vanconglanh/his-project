"use client";

import { useCallback } from "react";
import { useSearchParams, useRouter, usePathname } from "next/navigation";
import { Plus } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";

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

  return (
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
          onClick={() => router.push("/pharmacy/adjustments/new")}
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
        <AdjustmentTab />
      </TabsContent>
      <TabsContent value="alerts">
        <AlertsTab />
      </TabsContent>
    </Tabs>
  );
}
