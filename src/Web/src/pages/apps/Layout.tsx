import { Link, Outlet, useLocation, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useApiClient } from "@/hooks";
import { cn } from "@/lib/utils";

interface NavItemProps {
  to: string;
  label: string;
  isActive: boolean;
}

function NavItem({ to, label, isActive }: NavItemProps) {
  return (
    <Link
      to={to}
      className={cn(
        "block rounded-lg px-3 py-2 text-sm transition-colors",
        isActive
          ? "bg-primary text-primary-foreground"
          : "text-muted-foreground hover:bg-muted hover:text-foreground"
      )}
    >
      {label}
    </Link>
  );
}

interface NavGroupProps {
  label: string;
  children: React.ReactNode;
}

function NavGroup({ label, children }: NavGroupProps) {
  return (
    <div className="space-y-1">
      <div className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
        {label}
      </div>
      <div className="ml-4 space-y-1 border-l pl-3">{children}</div>
    </div>
  );
}

export default function AppLayout() {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();
  const { pathname } = useLocation();
  const part3 = pathname.split("/")[3];

  const isMatch_Overview = part3 === undefined;
  const isMatch_Routes = part3 === "routes";
  const isMatchLogs = part3 === "logs";
  const isMatchBuilds = part3 === "builds";
  const isMatchSettings = part3 === "settings";

  useEffect(() => {
    const getApp = async () => {
      const app = await get<IApp>(`/api/apps/${appId}`);
      setApp(app);
    };
    getApp();
  }, []);

  if (!app) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    );
  }

  return (
    <AppContext.Provider value={{ app, setApp }}>
      <div>
        <Breadcrumb className="px-4 py-2">
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink asChild>
                <Link to="/">Home</Link>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbLink asChild>
                <Link to="/apps">Apps</Link>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>{app.name}</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>

        <div className="flex min-h-[calc(100vh-100px)]">
          {/* Sidebar */}
          <aside className="w-56 border-r bg-muted/30">
            <nav className="flex flex-col gap-4 p-4">
              {/* Overview */}
              <NavItem
                to=""
                label="Overview"
                isActive={isMatch_Overview}
              />

              {/* API Section */}
              <NavGroup label="API">
                <NavItem
                  to="routes"
                  label="Routes"
                  isActive={isMatch_Routes}
                />
                <NavItem
                  to="logs"
                  label="Logs"
                  isActive={isMatchLogs}
                />
              </NavGroup>

              {/* Pages Section */}
              <NavGroup label="Pages">
                <NavItem
                  to="builds"
                  label="Builds"
                  isActive={isMatchBuilds}
                />
              </NavGroup>

              {/* Settings */}
              <div className="mt-auto pt-4 border-t">
                <NavItem
                  to="settings"
                  label="Settings"
                  isActive={isMatchSettings}
                />
              </div>
            </nav>
          </aside>

          {/* Main Content */}
          <main className="flex-1 overflow-auto">
            <Outlet />
          </main>
        </div>
      </div>
    </AppContext.Provider>
  );
}
