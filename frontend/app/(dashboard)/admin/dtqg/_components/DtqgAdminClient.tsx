"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DtqgCredentialsForm } from "@/components/domain/DtqgCredentialsForm";
import { DtqgSubmissionTable } from "@/components/domain/DtqgSubmissionTable";
import { useDtqgSubmissions } from "@/lib/hooks/use-dtqg";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";

export function DtqgAdminClient() {
  const [status, setStatus] = useState<"" | "PENDING" | "SUBMITTED" | "ACCEPTED" | "REJECTED">("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [page, setPage] = useState(1);

  const { data: submissionsData, isLoading } = useDtqgSubmissions({
    status: status || undefined,
    from_date: fromDate || undefined,
    to_date: toDate || undefined,
    page,
    page_size: 20,
  });

  return (
    <Tabs defaultValue="credentials" className="space-y-4">
      <TabsList>
        <TabsTrigger value="credentials">Thông tin kết nối</TabsTrigger>
        <TabsTrigger value="submissions">Lịch sử gửi ĐTQG</TabsTrigger>
      </TabsList>

      <TabsContent value="credentials">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Cấu hình kết nối donthuocquocgia.vn</CardTitle>
          </CardHeader>
          <CardContent>
            <DtqgCredentialsForm />
          </CardContent>
        </Card>
      </TabsContent>

      <TabsContent value="submissions">
        <div className="space-y-4">
          {/* Filters */}
          <div className="flex flex-wrap gap-3 items-end">
            <div className="space-y-1">
              <Label className="text-xs">Trạng thái</Label>
              <Select value={status} onValueChange={(v) => { setStatus(v as typeof status); setPage(1); }}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Tất cả" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="">Tất cả</SelectItem>
                  <SelectItem value="PENDING">Đang chờ</SelectItem>
                  <SelectItem value="SUBMITTED">Đã gửi</SelectItem>
                  <SelectItem value="ACCEPTED">Chấp nhận</SelectItem>
                  <SelectItem value="REJECTED">Từ chối</SelectItem>
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Từ ngày</Label>
              <Input type="date" value={fromDate} onChange={(e) => setFromDate(e.target.value)} className="w-36" />
            </div>
            <div className="space-y-1">
              <Label className="text-xs">Đến ngày</Label>
              <Input type="date" value={toDate} onChange={(e) => setToDate(e.target.value)} className="w-36" />
            </div>
          </div>

          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
            </div>
          ) : (
            <DtqgSubmissionTable submissions={submissionsData?.data ?? []} />
          )}

          {submissionsData?.meta && submissionsData.meta.total > 20 && (
            <div className="flex items-center justify-between text-sm text-muted-foreground">
              <span>Tổng: {submissionsData.meta.total}</span>
              <div className="flex gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Trước</Button>
                <span className="flex items-center px-2">{page} / {Math.ceil(submissionsData.meta.total / 20)}</span>
                <Button variant="outline" size="sm" disabled={page >= Math.ceil(submissionsData.meta.total / 20)} onClick={() => setPage((p) => p + 1)}>Tiếp</Button>
              </div>
            </div>
          )}
        </div>
      </TabsContent>
    </Tabs>
  );
}
