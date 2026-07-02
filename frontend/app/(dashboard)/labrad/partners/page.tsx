import { LabPartnersTab } from "../_components/LabPartnersTab";

export default function LabPartnersPage() {
  return (
    <div className="space-y-4">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Đối tác xét nghiệm</h1>
        <p className="text-sm text-muted-foreground">
          Quản lý đơn vị xét nghiệm bên ngoài (Medlatec, Diag, BV 108...)
        </p>
      </div>
      <LabPartnersTab />
    </div>
  );
}
