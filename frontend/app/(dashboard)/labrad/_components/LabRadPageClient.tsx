"use client";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { LabResultsTab } from "./LabResultsTab";
import { RadResultsTab } from "./RadResultsTab";
import { LabPartnersTab } from "./LabPartnersTab";
import { LabIntegrationTab } from "./LabIntegrationTab";

export function LabRadPageClient() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Cận lâm sàng (CLS)</h2>
        <p className="text-sm text-muted-foreground">
          Kết quả xét nghiệm, chẩn đoán hình ảnh, đối tác lab và tích hợp
        </p>
      </div>

      <Tabs defaultValue="lab-results">
        <TabsList className="flex-wrap h-auto gap-1">
          <TabsTrigger value="lab-results">Kết quả xét nghiệm</TabsTrigger>
          <TabsTrigger value="rad-results">Kết quả CĐHA</TabsTrigger>
          <TabsTrigger value="partners">Đối tác lab</TabsTrigger>
          <TabsTrigger value="integration">Tích hợp lab</TabsTrigger>
        </TabsList>

        <TabsContent value="lab-results" className="pt-4">
          <LabResultsTab />
        </TabsContent>

        <TabsContent value="rad-results" className="pt-4">
          <RadResultsTab />
        </TabsContent>

        <TabsContent value="partners" className="pt-4">
          <LabPartnersTab />
        </TabsContent>

        <TabsContent value="integration" className="pt-4">
          <LabIntegrationTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}
