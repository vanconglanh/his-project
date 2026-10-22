import { create } from "zustand";
import { persist } from "zustand/middleware";
import { DEFAULT_GLUCOSE_UNIT, type GlucoseUnit } from "@/lib/utils/glucose";

// BM-69: lưu đơn vị đường huyết người dùng chọn ở 1 chỗ dùng chung -> mọi màn
// (form nhập sinh hiệu, card tóm tắt, nhật ký sinh hiệu) hiển thị NHẤT QUÁN cùng
// một đơn vị. Mặc định mmol/L (đơn vị chuẩn). Persist localStorage per-viewer.
interface GlucoseUnitState {
  unit: GlucoseUnit;
  setUnit: (unit: GlucoseUnit) => void;
  toggleUnit: () => void;
}

export const useGlucoseUnitStore = create<GlucoseUnitState>()(
  persist(
    (set) => ({
      unit: DEFAULT_GLUCOSE_UNIT,
      setUnit: (unit) => set({ unit }),
      toggleUnit: () =>
        set((s) => ({ unit: s.unit === "mmol/L" ? "mg/dL" : "mmol/L" })),
    }),
    { name: "glucose-unit-store" }
  )
);
