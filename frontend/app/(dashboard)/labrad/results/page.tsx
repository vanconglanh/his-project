import { LabResultsTab } from "../_components/LabResultsTab";

export default function LabResultsPage() {
  return (
    <div className="flex h-full min-h-0 flex-col gap-3">
      <div className="shrink-0">
        <h1 className="text-xl font-bold tracking-tight">Kết quả cận lâm sàng</h1>
        <p className="text-sm text-muted-foreground">
          Danh sách kết quả xét nghiệm + chẩn đoán hình ảnh
        </p>
      </div>
      <LabResultsTab />
    </div>
  );
}
