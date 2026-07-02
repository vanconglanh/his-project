import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listServices,
  getService,
  createService,
  updateService,
  deleteService,
  searchServices,
  importServices,
  listServicePackages,
  createServicePackage,
  updateServicePackage,
  deleteServicePackage,
  type ServiceListParams,
  type ServiceUpsertRequest,
  type ServicePackageUpsertRequest,
} from "@/lib/api/services";

export const SERVICE_KEYS = {
  all: ["services"] as const,
  list: (params?: ServiceListParams) => ["services", "list", params] as const,
  detail: (id: string) => ["services", id] as const,
  packages: (params?: object) => ["service-packages", params] as const,
};

export function useServices(params?: ServiceListParams) {
  return useQuery({
    queryKey: SERVICE_KEYS.list(params),
    queryFn: () => listServices(params),
  });
}

export function useService(id: string) {
  return useQuery({
    queryKey: SERVICE_KEYS.detail(id),
    queryFn: () => getService(id),
    enabled: Boolean(id),
  });
}

export function useSearchServices(q: string) {
  return useQuery({
    queryKey: ["services", "search", q],
    queryFn: () => searchServices(q),
    enabled: q.length >= 1,
    staleTime: 10_000,
  });
}

export function useCreateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ServiceUpsertRequest) => createService(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: SERVICE_KEYS.all }),
  });
}

export function useUpdateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: ServiceUpsertRequest }) =>
      updateService(id, body),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: SERVICE_KEYS.all });
      qc.invalidateQueries({ queryKey: SERVICE_KEYS.detail(id) });
    },
  });
}

export function useDeleteService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteService(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: SERVICE_KEYS.all }),
  });
}

export function useImportServices() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => importServices(file),
    onSuccess: () => qc.invalidateQueries({ queryKey: SERVICE_KEYS.all }),
  });
}

export function useServicePackages(params?: { q?: string; is_active?: boolean }) {
  return useQuery({
    queryKey: SERVICE_KEYS.packages(params),
    queryFn: () => listServicePackages(params),
  });
}

export function useCreateServicePackage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ServicePackageUpsertRequest) => createServicePackage(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["service-packages"] }),
  });
}

export function useUpdateServicePackage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: ServicePackageUpsertRequest }) =>
      updateServicePackage(id, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["service-packages"] }),
  });
}

export function useDeleteServicePackage() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteServicePackage(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["service-packages"] }),
  });
}
