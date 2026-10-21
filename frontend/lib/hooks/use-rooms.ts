import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listRoomsAdmin,
  createRoomAdmin,
  updateRoomAdmin,
  deleteRoomAdmin,
  type CreateRoomRequest,
  type UpdateRoomRequest,
} from "../api/rooms";

export const roomAdminKeys = {
  all: ["rooms-admin"] as const,
  list: (params?: object) => [...roomAdminKeys.all, "list", params] as const,
};

function errMsg(e: unknown, fallback: string): string {
  const m = (e as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message;
  return m || fallback;
}

/** Sau khi đổi danh mục phòng, form Tiếp đón (reception/rooms) phải tải lại. */
function invalidateAll(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: roomAdminKeys.all });
  qc.invalidateQueries({ queryKey: ["rooms"] });
  qc.invalidateQueries({ queryKey: ["reception"] });
}

export function useRoomsAdmin(params?: { is_active?: boolean; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: roomAdminKeys.list(params),
    queryFn: () => listRoomsAdmin(params),
  });
}

export function useCreateRoom() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateRoomRequest) => createRoomAdmin(body),
    onSuccess: () => {
      invalidateAll(qc);
      toast.success("Đã thêm phòng khám");
    },
    onError: (e) => toast.error(errMsg(e, "Thêm phòng khám thất bại")),
  });
}

export function useUpdateRoom() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateRoomRequest }) => updateRoomAdmin(id, body),
    onSuccess: () => {
      invalidateAll(qc);
      toast.success("Đã cập nhật phòng khám");
    },
    onError: (e) => toast.error(errMsg(e, "Cập nhật phòng khám thất bại")),
  });
}

export function useDeleteRoom() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteRoomAdmin(id),
    onSuccess: () => {
      invalidateAll(qc);
      toast.success("Đã xóa phòng khám");
    },
    onError: (e) => toast.error(errMsg(e, "Xóa phòng khám thất bại")),
  });
}
