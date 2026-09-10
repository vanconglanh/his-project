// BM-05 (Lark Bug-Feedback): helper dung chung de nhan dien role "nghiep vu dieu duong"
// (Dieu duong hoac Ky thuat vien) - ca 2 dung chung 1 so man hinh/luong (Tiep don chi xem
// hang doi, Kham benh chi thay tab CLS). Dat rieng file de tai su dung giua nhieu component
// (reception/page.tsx, ReceptionQueueBoard.tsx, EncounterTabs.tsx) thay vi copy logic.
export function isNursingRole(roles: string[]): boolean {
  return roles.some((r) => {
    const v = r.toLowerCase();
    return (
      v === "dieu_duong" ||
      v === "điều dưỡng" ||
      v === "ky_thuat_vien" ||
      v === "kỹ thuật viên"
    );
  });
}
