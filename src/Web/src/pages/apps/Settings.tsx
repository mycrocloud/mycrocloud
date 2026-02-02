import { useLocation, Link, Outlet, Navigate } from "react-router-dom";
import { Settings, Globe, FileCode } from "lucide-react";
import { cn } from "@/lib/utils";

export { GeneralTab, ApiTab, PagesTab } from "./settings/index";

const navItems = [
  { path: "general", label: "General", icon: Settings },
  { path: "api", label: "API", icon: Globe },
  { path: "pages", label: "Pages", icon: FileCode },
];

export default function AppSettings() {
  const location = useLocation();
  const settingsPath = location.pathname.split("/settings")[0] + "/settings";

  if (location.pathname === settingsPath || location.pathname === settingsPath + "/") {
    return <Navigate to={`${settingsPath}/general`} replace />;
  }

  const isActive = (path: string) => location.pathname === `${settingsPath}/${path}`;

  return (
    <div className="p-4">
      <div className="mb-6 flex items-center gap-2">
        <Settings className="h-5 w-5 text-muted-foreground" />
        <h2 className="text-lg font-semibold">Settings</h2>
      </div>

      <div className="flex gap-6">
        <nav className="w-56 shrink-0">
          <div className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active = isActive(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={cn(
                    "flex items-center gap-2 rounded-lg px-3 py-2 text-sm transition-colors hover:bg-muted",
                    active ? "bg-muted font-medium" : "text-muted-foreground"
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {item.label}
                </Link>
              );
            })}
          </div>
        </nav>

        <main className="flex-1 min-w-0 space-y-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
