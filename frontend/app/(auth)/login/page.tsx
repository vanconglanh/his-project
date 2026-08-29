import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { LoginForm } from "@/components/forms/LoginForm";
import { TestLoginPanel } from "@/components/forms/TestLoginPanel";

// Chi bat panel dang nhap nhanh khi build voi NEXT_PUBLIC_TEST_LOGIN_PANEL=true
// (xem ops/docker-compose.local-app.yml). Mac dinh KHONG bat - production/staging
// build binh thuong se khong co bien nay nen panel khong bao gio xuat hien.
const SHOW_TEST_LOGIN_PANEL = process.env.NEXT_PUBLIC_TEST_LOGIN_PANEL === "true";

export async function generateMetadata(): Promise<Metadata> {
  const t = await getTranslations("Auth");
  return { title: t("loginTitle") };
}

export default async function LoginPage() {
  const t = await getTranslations("Auth");

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        <h2 className="text-xl font-semibold">{t("loginTitle")}</h2>
        <p className="text-sm text-muted-foreground">{t("loginSubtitle")}</p>
      </div>
      <LoginForm />
      {SHOW_TEST_LOGIN_PANEL && <TestLoginPanel />}
    </div>
  );
}
