import { redirect } from "next/navigation";

// /dashboard → / (alias cho user gõ tay URL quen tay)
export default function DashboardAlias() {
  redirect("/");
}
