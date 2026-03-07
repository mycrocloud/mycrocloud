import { Link, Outlet, useLocation, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import { useApiClient } from "@/hooks";
import { cn } from "@/lib/utils";
import { ChevronLeft, Loader2 } from "lucide-react";
import { getAppDomain } from "./service";

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
  const isMatch_Routes = part3 === "api" && pathname.split("/")[4] === "routes";
  const isMatchLogs = part3 === "logs";
  const isMatchSpaDeployments = part3 === "spa" && pathname.split("/")[4] === "deployments";
  const isMatchApiDeployments = part3 === "api" && pathname.split("/")[4] === "deployments";
  const isMatchSettings = part3 === "settings";

  useEffect(() => {
    const getApp = async () => {
      const app = await get<IApp>(`/api/apps/${appId}`);
      app.domain = getAppDomain(app.name);
      setApp(app);
    };
    getApp();
  }, []);

  if (!app) {
    return (
      <div className="flex min-h-[50vh] items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <AppContext.Provider value={{ app, setApp }}>
      <div className="flex min-h-[calc(100vh-64px)]">
        {/* Sidebar */}
        <aside className="w-56 border-r bg-muted/30">
          {/* App Header */}
          <div className="border-b p-4">
            <Link
              to="/apps"
              className="inline-flex items-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              <ChevronLeft className="h-3 w-3" />
              All Apps
            </Link>
            <h1 className="mt-1 font-semibold truncate" title={app.name}>
              {app.name}
            </h1>
          </div>

          {/* Navigation */}
          <nav className="flex flex-col gap-4 p-4">
            <NavItem
              to=""
              label="Overview"
              isActive={isMatch_Overview}
            />

            <NavGroup label="API">
              <NavItem
                to="api/routes"
                label="Routes"
                isActive={isMatch_Routes}
              />
              <NavItem
                to="api/deployments"
                label="Deployments"
                isActive={isMatchApiDeployments}
              />
            </NavGroup>

            <NavGroup label="Pages">
              <NavItem
                to="spa/deployments"
                label="Deployments"
                isActive={isMatchSpaDeployments}
              />
            </NavGroup>

            <NavItem
              to="logs"
              label="Logs"
              isActive={isMatchLogs}
            />

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
    </AppContext.Provider>
  );
}
