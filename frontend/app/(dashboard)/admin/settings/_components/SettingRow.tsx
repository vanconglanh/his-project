"use client";

import { useEffect, useState } from "react";
import { Loader2, Save, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import type { AdminSettingItem } from "@/lib/api/settings";

interface SettingRowProps {
  item: AdminSettingItem;
  isSaving?: boolean;
  onSave: (key: string, value: string) => void;
}

export function SettingRow({ item, isSaving, onSave }: SettingRowProps) {
  const [value, setValue] = useState(item.value);

  useEffect(() => {
    setValue(item.value);
  }, [item.value]);

  const dirty = value !== item.value;

  function handleSave() {
    onSave(item.key, value);
  }

  function handleReset() {
    setValue(item.default_value);
  }

  return (
    <div className="flex flex-col gap-3 border-b py-3 last:border-b-0 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="font-medium">{item.label_vi}</p>
          {item.is_overridden && (
            <Badge variant="secondary" className="shrink-0">
              Đã tuỳ chỉnh
            </Badge>
          )}
        </div>
        {item.description_vi && (
          <p className="text-sm text-muted-foreground">{item.description_vi}</p>
        )}
        <p className="mt-0.5 text-xs text-muted-foreground/70">{item.key}</p>
      </div>

      <div className="flex shrink-0 items-center gap-2">
        {item.data_type === "bool" ? (
          <Switch
            checked={value === "true" || value === "1"}
            onCheckedChange={(checked) => setValue(checked ? "true" : "false")}
            aria-label={item.label_vi}
          />
        ) : (
          <Input
            type={item.data_type === "int" || item.data_type === "decimal" ? "number" : "text"}
            step={item.data_type === "decimal" ? "0.01" : undefined}
            value={value}
            onChange={(e) => setValue(e.target.value)}
            className="w-40"
          />
        )}

        {item.is_overridden && (
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            onClick={handleReset}
            aria-label="Khôi phục mặc định"
          >
            <RotateCcw className="h-3.5 w-3.5" />
          </Button>
        )}

        <Button type="button" size="sm" onClick={handleSave} disabled={!dirty || isSaving}>
          {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          Lưu
        </Button>
      </div>
    </div>
  );
}
