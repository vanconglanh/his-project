"use client";

import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { LabResultsTab } from "./LabResultsTab";
import { RadResultsTab } from "./RadResultsTab";
import { LabPartnersTab } from "./LabPartnersTab";
import { LabIntegrationTab } from "./LabIntegrationTab";
import { ClsOrderQueueTab } from "./ClsOrderQueueTab";

export function LabRadPageClient() {
  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Cận lâm sàng (CLS)</h2>
        <p className="text-sm text-muted-foreground">
          Kết quả xét nghiệm, chẩn đoán hình ảnh, đối tác lab và tích hợp
        </p>
      </div>

      <Tabs defaultValue="lab-results">
        <TabsList className="flex-wrap h-auto gap-1" data-tour="labrad-tabs">
          {/* BM-40 (Lark Bug-Feedback): tab moi cho KTV cap nhat trang thai chi dinh CLS
              (truoc day chua co man hinh nao lam viec nay). */}
          <TabsTrigger value="queue">Hàng chờ chỉ định</TabsTrigger>
          <TabsTrigger value="lab-results">Kết quả xét nghiệm</TabsTrigger>
          <TabsTrigger value="rad-results">Kết quả CĐHA</TabsTrigger>
          <TabsTrigger value="partners">Đối tác lab</TabsTrigger>
          <TabsTrigger value="integration">Tích hợp lab</TabsTrigger>
        </TabsList>

        <TabsContent value="queue" className="pt-4">
          <ClsOrderQueueTab />
        </TabsContent>

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
