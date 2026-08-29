"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import {
  Canvas as FabricCanvas,
  PencilBrush,
  FabricImage,
  Rect,
  Ellipse,
  IText,
  Line,
  Triangle,
  Group,
  type FabricObject,
  type TPointerEventInfo,
  type TPointerEvent,
} from "fabric";
import {
  Pencil,
  MoveUpRight,
  Square,
  Circle as CircleIcon,
  Type,
  Eraser,
  Loader2,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { toast } from "sonner";

type AnnotationTool = "pen" | "arrow" | "rect" | "circle" | "text";

const TOOLS: { id: AnnotationTool; label: string; icon: typeof Pencil }[] = [
  { id: "pen", label: "Bút", icon: Pencil },
  { id: "arrow", label: "Mũi tên", icon: MoveUpRight },
  { id: "rect", label: "Chữ nhật", icon: Square },
  { id: "circle", label: "Hình tròn", icon: CircleIcon },
  { id: "text", label: "Văn bản", icon: Type },
];

const COLORS = ["#ef4444", "#f59e0b", "#22c55e", "#3b82f6", "#a855f7", "#111827", "#ffffff"];

const CANVAS_MAX_WIDTH = 900;
const CANVAS_MAX_HEIGHT = 620;

export interface ImageAnnotationDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  imageUrl: string;
  fileName: string;
  isSaving?: boolean;
  onSave: (blob: Blob) => void | Promise<void>;
}

export function ImageAnnotationDialog({
  open,
  onOpenChange,
  imageUrl,
  fileName,
  isSaving = false,
  onSave,
}: ImageAnnotationDialogProps) {
  const canvasElRef = useRef<HTMLCanvasElement>(null);
  const fabricRef = useRef<FabricCanvas | null>(null);
  const shapeRef = useRef<FabricObject | null>(null);
  const startPointRef = useRef<{ x: number; y: number } | null>(null);
  const toolRef = useRef<AnnotationTool>("pen");
  const colorRef = useRef<string>(COLORS[0]);

  const [tool, setTool] = useState<AnnotationTool>("pen");
  const [color, setColor] = useState<string>(COLORS[0]);
  const [isLoadingImage, setIsLoadingImage] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  useEffect(() => {
    toolRef.current = tool;
  }, [tool]);

  useEffect(() => {
    colorRef.current = color;
  }, [color]);

  // Khởi tạo canvas + tải ảnh khi mở dialog
  useEffect(() => {
    if (!open || !canvasElRef.current) return;
    setIsLoadingImage(true);
    setLoadError(null);

    const canvas = new FabricCanvas(canvasElRef.current, {
      selection: false,
      preserveObjectStacking: true,
    });
    fabricRef.current = canvas;

    let cancelled = false;

    FabricImage.fromURL(imageUrl, { crossOrigin: "anonymous" })
      .then((img) => {
        if (cancelled) return;
        const scale = Math.min(
          CANVAS_MAX_WIDTH / (img.width ?? CANVAS_MAX_WIDTH),
          CANVAS_MAX_HEIGHT / (img.height ?? CANVAS_MAX_HEIGHT),
          1
        );
        const width = Math.round((img.width ?? CANVAS_MAX_WIDTH) * scale);
        const height = Math.round((img.height ?? CANVAS_MAX_HEIGHT) * scale);
        canvas.setDimensions({ width, height });
        img.set({ left: 0, top: 0, scaleX: scale, scaleY: scale, selectable: false, evented: false });
        canvas.backgroundImage = img;
        canvas.requestRenderAll();
        setIsLoadingImage(false);
      })
      .catch(() => {
        if (cancelled) return;
        setLoadError("Không thể tải ảnh gốc để chú thích. Vui lòng thử lại.");
        setIsLoadingImage(false);
      });

    canvas.freeDrawingBrush = new PencilBrush(canvas);
    canvas.freeDrawingBrush.width = 3;
    canvas.freeDrawingBrush.color = colorRef.current;

    const onMouseDown = (e: TPointerEventInfo<TPointerEvent>) => {
      const currentTool = toolRef.current;
      if (currentTool === "pen") return;
      const pointer = canvas.getScenePoint(e.e);
      startPointRef.current = { x: pointer.x, y: pointer.y };

      if (currentTool === "text") {
        const text = new IText("Nhập chú thích", {
          left: pointer.x,
          top: pointer.y,
          fill: colorRef.current,
          fontSize: 20,
          fontFamily: "Arial",
        });
        canvas.add(text);
        canvas.setActiveObject(text);
        text.enterEditing();
        return;
      }

      let shape: FabricObject | null = null;
      if (currentTool === "rect") {
        shape = new Rect({
          left: pointer.x,
          top: pointer.y,
          width: 1,
          height: 1,
          fill: "transparent",
          stroke: colorRef.current,
          strokeWidth: 3,
          selectable: true,
        });
      } else if (currentTool === "circle") {
        shape = new Ellipse({
          left: pointer.x,
          top: pointer.y,
          rx: 1,
          ry: 1,
          fill: "transparent",
          stroke: colorRef.current,
          strokeWidth: 3,
          selectable: true,
        });
      } else if (currentTool === "arrow") {
        const line = new Line([pointer.x, pointer.y, pointer.x, pointer.y], {
          stroke: colorRef.current,
          strokeWidth: 3,
        });
        const tip = new Triangle({
          left: pointer.x,
          top: pointer.y,
          width: 12,
          height: 14,
          fill: colorRef.current,
          angle: 0,
        });
        shape = new Group([line, tip], { selectable: true });
      }

      if (shape) {
        shapeRef.current = shape;
        canvas.add(shape);
      }
    };

    const onMouseMove = (e: TPointerEventInfo<TPointerEvent>) => {
      const currentTool = toolRef.current;
      const start = startPointRef.current;
      const shape = shapeRef.current;
      if (!start || !shape || currentTool === "pen" || currentTool === "text") return;
      const pointer = canvas.getScenePoint(e.e);

      if (currentTool === "rect" && shape instanceof Rect) {
        shape.set({
          width: Math.abs(pointer.x - start.x),
          height: Math.abs(pointer.y - start.y),
          left: Math.min(pointer.x, start.x),
          top: Math.min(pointer.y, start.y),
        });
      } else if (currentTool === "circle" && shape instanceof Ellipse) {
        shape.set({
          rx: Math.abs(pointer.x - start.x) / 2,
          ry: Math.abs(pointer.y - start.y) / 2,
          left: Math.min(pointer.x, start.x),
          top: Math.min(pointer.y, start.y),
        });
      } else if (currentTool === "arrow" && shape instanceof Group) {
        const objs = shape.getObjects();
        const line = objs[0] as Line;
        const tip = objs[1] as Triangle;
        line.set({ x2: pointer.x - start.x, y2: pointer.y - start.y });
        const dx = pointer.x - start.x;
        const dy = pointer.y - start.y;
        const angle = (Math.atan2(dy, dx) * 180) / Math.PI + 90;
        tip.set({ left: pointer.x - start.x, top: pointer.y - start.y, angle });
        shape.set({ left: start.x, top: start.y });
      }
      canvas.requestRenderAll();
    };

    const onMouseUp = () => {
      startPointRef.current = null;
      shapeRef.current = null;
      canvas.requestRenderAll();
    };

    canvas.on("mouse:down", onMouseDown);
    canvas.on("mouse:move", onMouseMove);
    canvas.on("mouse:up", onMouseUp);

    return () => {
      cancelled = true;
      canvas.off("mouse:down", onMouseDown);
      canvas.off("mouse:move", onMouseMove);
      canvas.off("mouse:up", onMouseUp);
      canvas.dispose();
      fabricRef.current = null;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, imageUrl]);

  // Bật/tắt chế độ vẽ tự do khi đổi công cụ
  useEffect(() => {
    const canvas = fabricRef.current;
    if (!canvas) return;
    canvas.isDrawingMode = tool === "pen";
    canvas.selection = false;
    if (canvas.freeDrawingBrush) {
      canvas.freeDrawingBrush.color = color;
    }
  }, [tool, color]);

  const handleClear = useCallback(() => {
    const canvas = fabricRef.current;
    if (!canvas) return;
    canvas.getObjects().forEach((obj) => canvas.remove(obj));
    canvas.requestRenderAll();
  }, []);

  const handleSave = useCallback(async () => {
    const canvas = fabricRef.current;
    if (!canvas) return;
    try {
      const blob: Blob = await new Promise((resolve, reject) => {
        canvas.getElement().toBlob((b) => {
          if (b) resolve(b);
          else reject(new Error("Không xuất được ảnh"));
        }, "image/png");
      });
      await onSave(blob);
    } catch {
      toast.error("Không thể xuất ảnh chú thích (có thể do ảnh gốc chặn CORS). Vui lòng thử lại.");
    }
  }, [onSave]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-3xl max-h-[95vh] overflow-auto">
        <DialogHeader>
          <DialogTitle>Chú thích ảnh — {fileName}</DialogTitle>
        </DialogHeader>

        <div className="flex flex-wrap items-center gap-2 rounded-md border bg-muted/30 p-2">
          <div className="flex flex-wrap gap-1" role="group" aria-label="Công cụ vẽ">
            {TOOLS.map((t) => {
              const Icon = t.icon;
              return (
                <Button
                  key={t.id}
                  type="button"
                  size="sm"
                  variant={tool === t.id ? "default" : "outline"}
                  className="min-h-11 gap-1.5"
                  aria-pressed={tool === t.id}
                  onClick={() => setTool(t.id)}
                >
                  <Icon className="h-4 w-4" />
                  {t.label}
                </Button>
              );
            })}
          </div>

          <div className="mx-1 h-6 w-px bg-border" />

          <div className="flex items-center gap-1" role="group" aria-label="Chọn màu">
            {COLORS.map((c) => (
              <button
                key={c}
                type="button"
                aria-label={`Màu ${c}`}
                aria-pressed={color === c}
                className={cn(
                  "h-8 w-8 rounded-full border-2 transition-transform",
                  color === c ? "border-primary scale-110" : "border-muted-foreground/30"
                )}
                style={{ backgroundColor: c }}
                onClick={() => setColor(c)}
              />
            ))}
          </div>

          <div className="mx-1 h-6 w-px bg-border" />

          <Button
            type="button"
            size="sm"
            variant="outline"
            className="min-h-11 gap-1.5"
            onClick={handleClear}
          >
            <Eraser className="h-4 w-4" />
            Xoá
          </Button>
        </div>

        <div className="relative flex items-center justify-center rounded-md border bg-black/5 p-2 min-h-[200px]">
          {isLoadingImage && (
            <div className="absolute inset-0 flex items-center justify-center bg-background/60">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          )}
          {loadError ? (
            <p className="py-10 text-sm text-destructive">{loadError}</p>
          ) : (
            <canvas ref={canvasElRef} />
          )}
        </div>

        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            className="min-h-11"
            onClick={() => onOpenChange(false)}
            disabled={isSaving}
          >
            Huỷ
          </Button>
          <Button
            type="button"
            className="min-h-11"
            onClick={handleSave}
            disabled={isSaving || isLoadingImage || !!loadError}
          >
            {isSaving ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Đang lưu...
              </>
            ) : (
              "Lưu chú thích"
            )}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
