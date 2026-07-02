import { StatusBadge } from "@/components/ui/entity-status-badge";
import type { TenantResponse } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

interface TenantDetailProps {
  tenant: TenantResponse;
}

function DetailRow({ label, value }: { label: string; value?: React.ReactNode }) {
  return (
    <div className="grid grid-cols-[140px_1fr] gap-2 py-2 border-b last:border-0">
      <span className="text-sm text-muted-foreground">{label}</span>
      <span className="text-sm font-medium break-all">{value ?? "—"}</span>
    </div>
  );
}

export function TenantDetail({ tenant }: TenantDetailProps) {
  return (
    <div className="mt-4 space-y-0">
      <DetailRow label="Mã phòng khám" value={<span className="font-mono">{tenant.code}</span>} />
      <DetailRow label="Tên" value={tenant.name} />
      <DetailRow label="Subdomain" value={`${tenant.subdomain}.prodiab.vn`} />
      <DetailRow label="Trạng thái" value={<StatusBadge status={tenant.status} />} />
      <DetailRow label="Mã CSKCB" value={tenant.cskcb_code} />
      <DetailRow label="Mã số thuế" value={tenant.tax_code} />
      <DetailRow label="Email" value={tenant.email} />
      <DetailRow label="Điện thoại" value={tenant.phone} />
      <DetailRow label="Địa chỉ" value={tenant.address} />
      <DetailRow
        label="Dung lượng"
        value={tenant.storage_quota_gb ? `${tenant.storage_quota_gb} GB` : undefined}
      />
      <DetailRow
        label="Hết hạn"
        value={tenant.expires_at ? formatDateTime(tenant.expires_at) : "Không giới hạn"}
      />
      <DetailRow label="Ngày tạo" value={formatDateTime(tenant.created_at)} />
      <DetailRow label="Cập nhật" value={tenant.updated_at ? formatDateTime(tenant.updated_at) : undefined} />
    </div>
  );
}
