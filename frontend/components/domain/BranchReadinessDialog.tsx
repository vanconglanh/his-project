"use client";

import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { CheckCircle2, XCircle, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { useBranchReadiness, useActivateBranch } from "@/lib/hooks/use-branches";
import type { BranchResponse } from "@/lib/api/branches";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  branch: BranchResponse | null;
}

export function BranchReadinessDialog({ open, onOpenChange, branch }: Props) {
  const { data, isLoading } = useBranchReadiness(open ? branch?.id : undefined);
  const activateMutation = useActivateBranch();

  const canActivate = branch && branch.status !== "ACTIVE";

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Checklist go-live — {branch?.name}</DialogTitle>
          <DialogDescription>
            Chi nhánh chỉ có thể kích hoạt (chuyển sang Hoạt động) khi đạt tất cả các mục dưới đây.
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-10 w-full" />
            ))}
          </div>
        ) : (
          <div className="space-y-2 max-h-96 overflow-y-auto">
            {data?.items.map((item) => (
              <div
                key={item.key}
                className={cn(
                  "flex items-start gap-2 rounded-md border p-2.5",
                  item.passed ? "bg-green-50 border-green-200" : "bg-red-50 border-red-200"
                )}
              >
                {item.passed ? (
                  <CheckCircle2 className="h-4 w-4 mt-0.5 shrink-0 text-green-600" />
                ) : (
                  <XCircle className="h-4 w-4 mt-0.5 shrink-0 text-red-600" />
                )}
                <div className="min-w-0">
                  <p className="text-sm font-medium">{item.label}</p>
                  {item.detail && (
                    <p className="text-xs text-muted-foreground">{item.detail}</p>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
          {canActivate && (
            <Button
              disabled={!data?.all_passed || activateMutation.isPending}
              onClick={() => {
                if (!branch) return;
                activateMutation.mutate(branch.id, {
                  onSuccess: () => onOpenChange(false),
                });
              }}
            >
              {activateMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Kích hoạt chi nhánh
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
