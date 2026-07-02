import { AppSidebar } from "@/components/layout/AppSidebar";
import { AppTopbar } from "@/components/layout/AppTopbar";
import { DashboardOverview } from "./(dashboard)/_components/DashboardOverview";

export default function HomePage() {
  return (
    <div className="flex h-screen overflow-hidden">
      <AppSidebar />
      <div className="flex flex-1 flex-col overflow-hidden">
        <AppTopbar />
        <main className="flex-1 overflow-y-auto p-6">
          <DashboardOverview />
        </main>
      </div>
    </div>
  );
}
