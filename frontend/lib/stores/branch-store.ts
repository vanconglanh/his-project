import { create } from "zustand";
import { persist } from "zustand/middleware";

/**
 * Chi nhánh đang làm việc (active branch) của user hiện tại trên trình duyệt này.
 * - `activeBranchId === null` nghĩa là "Tất cả chi nhánh" (chỉ dùng được khi user có
 *   quyền xem toàn tenant — cross_view/super admin).
 * - Đổi chi nhánh KHÔNG cần đăng nhập lại: chỉ cần gửi header `X-Branch-Id` khác
 *   (theo quyết định Q10) — xem interceptor trong `lib/api/client.ts`.
 */
interface BranchState {
  activeBranchId: number | null;
  activeBranchName: string | null;
}

interface BranchActions {
  setActiveBranch: (id: number | null, name: string | null) => void;
  clearActiveBranch: () => void;
}

export const useBranchStore = create<BranchState & BranchActions>()(
  persist(
    (set) => ({
      activeBranchId: null,
      activeBranchName: null,

      setActiveBranch: (id, name) =>
        set({ activeBranchId: id, activeBranchName: name }),

      clearActiveBranch: () => set({ activeBranchId: null, activeBranchName: null }),
    }),
    {
      // Key localStorage — dùng trực tiếp bởi request interceptor của apiClient.
      name: "prodiab.activeBranchId",
    }
  )
);
