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

// BM-08 (Lark Bug-Feedback): Le tan KHONG duoc chuyen sang man "Kham benh" duoi bat ky
// hinh thuc nao (bam "Dua vao kham" van tao duoc encounter vi le_tan co quyen
// encounter.create/reception.queue.manage de quan ly hang doi, nhung khong co quyen ghi
// lam sang nao khac -> moi thao tac deu bao "That bai"). An han nut nay o FE cho role Le tan.
export function isReceptionistRole(roles: string[]): boolean {
  return roles.some((r) => {
    const v = r.toLowerCase();
    return v === "le_tan" || v === "lễ tân";
  });
}
