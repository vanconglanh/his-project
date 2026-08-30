"use client";

/**
 * Engine chạy product tour dựa trên driver.js.
 * Chịu trách nhiệm: lọc bước theo quyền, dựng driver, chạy tour, và lưu trạng thái đã xem.
 */
import { driver, type DriveStep } from "driver.js";
import "driver.js/dist/driver.css";
import type { TourDefinition, TourStep } from "./types";

/** Sinh key localStorage nhớ trạng thái đã xem theo route + user. */
export function tourSeenKey(route: string, userId: number | string): string {
  return `tour-seen:${route}:${userId}`;
}

/** Kiểm tra tour đã được user xem chưa (an toàn với SSR / localStorage bị chặn). */
export function isTourSeen(route: string, userId: number | string): boolean {
  if (typeof window === "undefined") return false;
  try {
    return window.localStorage.getItem(tourSeenKey(route, userId)) === "1";
  } catch {
    return false;
  }
}

/** Đánh dấu tour đã xem. */
export function markTourSeen(route: string, userId: number | string): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(tourSeenKey(route, userId), "1");
  } catch {
    /* localStorage bị chặn -> bỏ qua, không chặn UX */
  }
}

/**
 * Key localStorage cho tour "Làm quen hệ thống" (onboarding) — chạy 1 lần duy nhất
 * theo user, KHÔNG gắn với route cụ thể. Tách riêng khỏi `tourSeenKey` (tour trang lẻ).
 */
export function onboardingSeenKey(userId: number | string): string {
  return `tour-onboarding-seen:${userId}`;
}

/** Kiểm tra user đã xem tour onboarding chưa. */
export function isOnboardingSeen(userId: number | string): boolean {
  if (typeof window === "undefined") return false;
  try {
    return window.localStorage.getItem(onboardingSeenKey(userId)) === "1";
  } catch {
    return false;
  }
}

/** Đánh dấu tour onboarding đã xem. */
export function markOnboardingSeen(userId: number | string): void {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(onboardingSeenKey(userId), "1");
  } catch {
    /* localStorage bị chặn -> bỏ qua, không chặn UX */
  }
}

/** Hàm kiểm tra quyền: trả về true nếu user có permission code truyền vào. */
export type PermissionChecker = (code: string) => boolean;

/**
 * Lọc bước theo quyền + giữ lại bước có element thực sự tồn tại trên DOM
 * (element intro/outro không có selector luôn được giữ).
 */
function resolveSteps(steps: TourStep[], can: PermissionChecker): TourStep[] {
  return steps.filter((s) => {
    if (s.permission && !can(s.permission)) return false;
    if (!s.selector) return true;
    if (typeof document === "undefined") return false;
    return document.querySelector(s.selector) != null;
  });
}

/** Chuyển TourStep -> DriveStep của driver.js. */
function toDriveStep(s: TourStep): DriveStep {
  const popover: DriveStep["popover"] = {
    title: s.title,
    description: s.description,
  };
  if (s.side) popover.side = s.side;
  if (s.align) popover.align = s.align;
  return s.selector ? { element: s.selector, popover } : { popover };
}

export interface RunTourOptions {
  tour: TourDefinition;
  can: PermissionChecker;
  /** Route hiện tại, dùng để lưu trạng thái đã xem. */
  route: string;
  /** userId để tách trạng thái theo từng user. */
  userId: number | string;
  /** Gọi khi tour kết thúc (hoàn tất hoặc đóng). */
  onDone?: () => void;
}

/**
 * Chạy 1 tour. Trả về true nếu có ít nhất 1 bước hiển thị được (đã chạy),
 * false nếu không có bước nào hợp lệ (vd toàn bộ element chưa render / không đủ quyền).
 */
export function runTour(opts: RunTourOptions): boolean {
  const { tour, can, route, userId, onDone } = opts;
  const steps = resolveSteps(tour.steps, can);
  if (steps.length === 0) {
    // Vẫn đánh dấu đã xem để không cố auto-chạy lặp lại vô ích.
    markTourSeen(route, userId);
    onDone?.();
    return false;
  }

  // GHI CHÚ: khi có >1 lần gọi driver.js `driver()` trong cùng 1 phiên trang
  // (vd tour trang lẻ chạy nối ngay sau tour onboarding), sự kiện đóng bằng nút
  // "X" của instance THỨ 2 đôi khi không kích hoạt `onDestroyed` một cách đáng
  // tin cậy (quan sát thực nghiệm với driver.js — không rõ nguyên nhân sâu bên
  // trong thư viện). Để tránh phụ thuộc hoàn toàn vào `onDestroyed`, gọi thẳng
  // `finish()` ở `onCloseClick` (chạy đồng bộ ngay khi bấm X) rồi mới destroy.
  let handled = false;
  function finish() {
    if (handled) return;
    handled = true;
    markTourSeen(route, userId);
    onDone?.();
  }

  const d = driver({
    showProgress: true,
    allowClose: true,
    overlayColor: "rgba(0,0,0,0.55)",
    nextBtnText: "Tiếp →",
    prevBtnText: "← Quay lại",
    doneBtnText: "Hoàn tất",
    progressText: "Bước {{current}}/{{total}}",
    steps: steps.map(toDriveStep),
    onCloseClick: () => {
      finish();
      d.destroy();
    },
    onDestroyed: () => {
      finish();
    },
  });

  d.drive();
  return true;
}

export interface RunOnboardingTourOptions {
  tour: TourDefinition;
  can: PermissionChecker;
  /** userId để tách trạng thái đã xem theo từng user. */
  userId: number | string;
  onDone?: () => void;
}

/**
 * Chạy tour onboarding (dùng chung mọi trang) — đánh dấu đã xem theo user,
 * KHÔNG theo route. Trả về true nếu có ít nhất 1 bước hiển thị được.
 */
export function runOnboardingTour(opts: RunOnboardingTourOptions): boolean {
  const { tour, can, userId, onDone } = opts;
  const steps = resolveSteps(tour.steps, can);
  if (steps.length === 0) {
    markOnboardingSeen(userId);
    onDone?.();
    return false;
  }

  // Xem ghi chú tương tự trong runTour() ở trên.
  let handled = false;
  function finish() {
    if (handled) return;
    handled = true;
    markOnboardingSeen(userId);
    onDone?.();
  }

  const d = driver({
    showProgress: true,
    allowClose: true,
    overlayColor: "rgba(0,0,0,0.55)",
    nextBtnText: "Tiếp →",
    prevBtnText: "← Quay lại",
    doneBtnText: "Hoàn tất",
    progressText: "Bước {{current}}/{{total}}",
    steps: steps.map(toDriveStep),
    onCloseClick: () => {
      finish();
      d.destroy();
    },
    onDestroyed: () => {
      finish();
    },
  });

  d.drive();
  return true;
}

/**
 * Đếm số bước khả dụng (theo quyền + có mặt trên DOM). Dùng để quyết định
 * ẩn/disable nút "Hướng dẫn".
 */
export function countAvailableSteps(
  tour: TourDefinition,
  can: PermissionChecker,
): number {
  return resolveSteps(tour.steps, can).length;
}
