"use client";

import { useEffect } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { Building2, ChevronDown, Check } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useBranchStore } from "@/lib/stores/branch-store";
import { useBranches } from "@/lib/hooks/use-branches";
import { usePermissions } from "@/lib/hooks/use-permissions";

/**
 * Chọn chi nhánh đang làm việc — hiển thị trên topbar.
 *
 * GIẢ ĐỊNH (chưa xác nhận với BE): danh sách chi nhánh "user được phép" hiện
 * lấy từ GET /api/v1/branches?is_active=true (toàn bộ chi nhánh active của
 * tenant), vì `GET /api/v1/me` (users.ts:getMe) hiện KHÔNG trả branch_ids /
 * default_branch_id. Nếu sau này backend bổ sung field này vào UserResponse,
 * nên thay danh sách filter theo đó.
 */
export function BranchSwitcher() {
  const qc = useQueryClient();
  const { isSuperAdmin, has } = usePermissions();
  const canViewAllBranches = isSuperAdmin || has("branch.cross_view");

  const { activeBranchId, activeBranchName, setActiveBranch } = useBranchStore();
  const { data, isLoading } = useBranches({ is_active: true });

  const branches = data?.data ?? [];

  // Auto-chọn chi nhánh mặc định lần đầu (chưa có lựa chọn nào được lưu)
  useEffect(() => {
    if (isLoading) return;
    if (activeBranchId !== null) return;
    if (branches.length === 0) return;
    const defaultBranch = branches.find((b) => b.is_default) ?? branches[0];
    if (defaultBranch) {
      setActiveBranch(defaultBranch.id, defaultBranch.name);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoading, branches.length]);

  function handleSelect(id: number | null, name: string | null) {
    if (id === activeBranchId) return;
    setActiveBranch(id, name);
    // Đổi chi nhánh -> toàn bộ dữ liệu cache theo chi nhánh cũ không còn hợp lệ.
    qc.clear();
    toast.success(
      id === null
        ? "Đã chuyển sang xem Tất cả chi nhánh"
        : `Đã chuyển sang chi nhánh ${name ?? ""}`
    );
  }

  if (isLoading) return null;

  // Chỉ 1 chi nhánh và không có quyền xem toàn tenant -> hiển thị nhãn tĩnh
  if (branches.length <= 1 && !canViewAllBranches) {
    const label = branches[0]?.name ?? activeBranchName ?? "Chi nhánh";
    return (
      <div className="hidden md:flex items-center gap-1.5 px-2.5 py-1.5 text-sm text-muted-foreground rounded-md border border-transparent">
        <Building2 className="h-4 w-4" aria-hidden="true" />
        <span className="truncate max-w-40">{label}</span>
      </div>
    );
  }

  const currentLabel =
    activeBranchId === null ? "Tất cả chi nhánh" : activeBranchName ?? "Chọn chi nhánh";

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label="Chọn chi nhánh đang làm việc"
        data-tour="branch-switcher"
        className="flex items-center gap-1.5 px-2.5 min-h-[36px] text-sm font-medium rounded-md border border-border hover:bg-accent hover:text-accent-foreground transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
      >
        <Building2 className="h-4 w-4 shrink-0" aria-hidden="true" />
        <span className="truncate max-w-36">{currentLabel}</span>
        <ChevronDown className="h-3.5 w-3.5 shrink-0 opacity-60" aria-hidden="true" />
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-64">
        <DropdownMenuGroup>
          <DropdownMenuLabel>Chi nhánh đang làm việc</DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        {canViewAllBranches && (
          <>
            <DropdownMenuItem onClick={() => handleSelect(null, null)}>
              {activeBranchId === null && <Check className="h-4 w-4" />}
              <span className={activeBranchId === null ? "font-medium" : ""}>
                Tất cả chi nhánh
              </span>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
          </>
        )}
        {branches.length === 0 ? (
          <div className="px-2 py-2 text-xs text-muted-foreground">
            Chưa có chi nhánh nào được cấu hình.
          </div>
        ) : (
          branches.map((b) => (
            <DropdownMenuItem key={b.id} onClick={() => handleSelect(b.id, b.name)}>
              {activeBranchId === b.id && <Check className="h-4 w-4" />}
              <span className={activeBranchId === b.id ? "font-medium" : ""}>
                {b.name}
                {b.is_default && (
                  <span className="ml-1.5 text-xs text-muted-foreground">(mặc định)</span>
                )}
              </span>
            </DropdownMenuItem>
          ))
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
