# Phân công fix bug — 2026-07-21

**Leader:** Khoa (main)
**Nguồn:** `docs/qc/bug-report-20260721.md` (11 bug)
**Flow:** Dev → Tester → QC → DevOps

## Bảng phân công

| BUG ID | Severity | Role | File chính | Status |
|--------|----------|------|-----------|--------|
| BUG-001 | Blocker | frontend | portal-client/middleware.ts (mới) | Assigned |
| BUG-002 | Blocker | frontend + backend | frontend/middleware.ts (mới) + verify SSR data | Assigned |
| BUG-003 | Blocker | frontend | portal-client/components/Providers.tsx | Assigned |
| BUG-004 | High | frontend | portal-client/lib/auth.ts | Assigned |
| BUG-005 | High | frontend | portal-client/app/prescriptions/page.tsx | Assigned |
| BUG-006 | High | frontend | portal-client/lib/hooks.ts | Assigned |
| BUG-007 | High | frontend | portal-client/app/health/page.tsx | Assigned |
| BUG-008 | Med | frontend | portal-client/app/medications/page.tsx | Assigned |
| BUG-009 | Med | frontend | portal-client/app/encounters/page.tsx | Assigned |
| BUG-010 | Med | frontend | portal-client/app/login/page.tsx | Assigned |
| BUG-011 | Low | frontend | portal-client/app/page.tsx | Assigned |

## Phase log

- **Phase 1 (Dev) — DONE:** Nam fix 11 bug FE. Thảo verify BUG-002 → xác nhận backend đã `[Authorize]`, KHÔNG data leak (chỉ placeholder). Nam fix thêm BUG-002bis (set cookie `his-access-token` sau login `frontend/lib/hooks/use-auth.ts`).
- **Phase 2 (Tester) — PASS 11/11:** Phượng verify static review.
- **Phase 3 (QC) — APPROVE (staging):** Chi ghi 3 follow-up backlog: (1) chuyển portal token sang httpOnly cookie backend Set-Cookie, (2) guard pathname !== '/login' trong Providers, (3) smoke test frontend login cookie (đã fix ở BUG-002bis).
- **Phase 4 (DevOps) — READY:** Chương chuẩn bị branch `fix/portal-qc-batch-20260721`, lệnh deploy + rollback plan. Chờ user duyệt commit + deploy.
