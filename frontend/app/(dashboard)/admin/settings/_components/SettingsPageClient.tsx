"use client";

import { useMemo } from "react";
import { Card, CardHeader, CardTitle, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { useAdminSettings, useUpdateAdminSetting } from "@/lib/hooks/use-settings";
import { SettingRow } from "./SettingRow";

export function SettingsPageClient() {
  const { data: settings = [], isLoading } = useAdminSettings();
  const updateMutation = useUpdateAdminSetting();

  const groups = useMemo(() => {
    const map = new Map<string, typeof settings>();
    for (const item of settings) {
      const key = item.value_group || "Khác";
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(item);
    }
    return Array.from(map.entries());
  }, [settings]);

  if (isLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-40 w-full" />
        ))}
      </div>
    );
  }

  if (settings.length === 0) {
    return (
      <div className="flex h-48 flex-col items-center justify-center gap-2 rounded-md border text-muted-foreground">
        <p className="text-sm">Chưa có cấu hình nào được khai báo.</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {groups.map(([groupName, items]) => (
        <Card key={groupName}>
          <CardHeader>
            <CardTitle>{groupName}</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="divide-y">
              {items.map((item) => (
                <SettingRow
                  key={item.key}
                  item={item}
                  isSaving={
                    updateMutation.isPending && updateMutation.variables?.key === item.key
                  }
                  onSave={(key, value) => updateMutation.mutate({ key, value })}
                />
              ))}
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
