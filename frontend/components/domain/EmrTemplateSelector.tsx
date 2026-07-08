"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useEmrTemplates } from "@/lib/hooks/use-emr";
import type { EmrTemplateResponse } from "@/lib/api/types";
import { FileText, ChevronDown } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuLabel,
} from "@/components/ui/dropdown-menu";

interface Props {
  onSelect: (template: EmrTemplateResponse) => void;
}

export function EmrTemplateSelector({ onSelect }: Props) {
  const { data: templates, isLoading } = useEmrTemplates();

  const systemTemplates = templates?.filter((t) => t.is_system) ?? [];
  const customTemplates = templates?.filter((t) => !t.is_system) ?? [];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        className="inline-flex items-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
        disabled={isLoading}
      >
        <FileText className="h-4 w-4" />
        Mẫu bệnh án
        <ChevronDown className="h-3 w-3 opacity-60" />
      </DropdownMenuTrigger>
      <DropdownMenuContent className="w-64" align="start">
        {systemTemplates.length > 0 && (
          <DropdownMenuGroup>
            <DropdownMenuLabel>Mẫu hệ thống</DropdownMenuLabel>
            {systemTemplates.map((t) => (
              <DropdownMenuItem key={t.id} onClick={() => onSelect(t)} className="gap-2">
                <FileText className="h-4 w-4 text-muted-foreground" />
                <span className="flex-1">{t.name}</span>
                <Badge variant="outline" className="text-xs">{t.speciality}</Badge>
              </DropdownMenuItem>
            ))}
          </DropdownMenuGroup>
        )}
        {customTemplates.length > 0 && (
          <>
            {systemTemplates.length > 0 && <DropdownMenuSeparator />}
            <DropdownMenuGroup>
              <DropdownMenuLabel>Mẫu tùy chỉnh</DropdownMenuLabel>
              {customTemplates.map((t) => (
                <DropdownMenuItem key={t.id} onClick={() => onSelect(t)} className="gap-2">
                  <FileText className="h-4 w-4 text-muted-foreground" />
                  <span>{t.name}</span>
                </DropdownMenuItem>
              ))}
            </DropdownMenuGroup>
          </>
        )}
        {(!templates || templates.length === 0) && !isLoading && (
          <DropdownMenuItem disabled>Chưa có mẫu bệnh án</DropdownMenuItem>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
