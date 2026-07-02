import { DebtsTab } from "../_components/DebtsTab";

export default function DebtsPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-xl font-bold tracking-tight">Công nợ</h1>
        <p className="text-sm text-muted-foreground">
          Danh sách bệnh nhân còn nợ tiền khám/thuốc
        </p>
      </div>
      <DebtsTab />
    </div>
  );
}
