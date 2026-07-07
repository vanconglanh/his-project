"use client";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { BarChart3, Stethoscope, Pill } from "lucide-react";
import { FinancialTab } from "./FinancialTab";
import { ClinicalTab } from "./ClinicalTab";
import { PharmacyTab } from "./PharmacyTab";

/** Tabs chart Tổng quan (Dashboard). PageHeader do màn hình cha (ReportEngineClient) quản lý. */
export function ReportsPageClient() {
  return (
    <div className="space-y-6">
      <Tabs defaultValue="financial">
        <TabsList className="mb-4">
          <TabsTrigger value="financial" className="gap-2">
            <BarChart3 className="h-4 w-4" />
            Tài chính
          </TabsTrigger>
          <TabsTrigger value="clinical" className="gap-2">
            <Stethoscope className="h-4 w-4" />
            Lâm sàng
          </TabsTrigger>
          <TabsTrigger value="pharmacy" className="gap-2">
            <Pill className="h-4 w-4" />
            Kho dược
          </TabsTrigger>
        </TabsList>

        <TabsContent value="financial">
          <FinancialTab />
        </TabsContent>
        <TabsContent value="clinical">
          <ClinicalTab />
        </TabsContent>
        <TabsContent value="pharmacy">
          <PharmacyTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
