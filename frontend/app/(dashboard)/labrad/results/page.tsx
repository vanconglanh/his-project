import { LabResultsTab } from "../_components/LabResultsTab";

export default function LabResultsPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Kết quả cận lâm sàng</h1>
        <p className="text-sm text-muted-foreground">
          Danh sách kết quả xét nghiệm + chẩn đoán hình ảnh
        </p>
      </div>
      <LabResultsTab />
    </div>
  );
}
