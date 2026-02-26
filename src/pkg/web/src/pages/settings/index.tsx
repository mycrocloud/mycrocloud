import { Link, Outlet, useLocation, Navigate } from "react-router-dom";
import { Settings as SettingsIcon, Link2, Key } from "lucide-react";
import { cn } from "@/lib/utils";

const navItems = [
  { path: "connections", label: "Connections", icon: Link2 },
  { path: "tokens", label: "API Tokens", icon: Key },
];

export default function Settings() {
  const location = useLocation();

  // Redirect /settings to /settings/connections
  if (location.pathname === "/settings") {
    return <Navigate to="/settings/connections" replace />;
  }

  const isActive = (path: string) => location.pathname.startsWith(`/settings/${path}`);

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      {/* Page Header */}
      <div className="mb-6 flex items-center gap-2">
        <SettingsIcon className="h-5 w-5 text-muted-foreground" />
        <h1 className="text-lg font-semibold">Account Settings</h1>
      </div>

      {/* Sidebar Layout */}
      <div className="flex gap-6">
        {/* Sidebar Navigation */}
        <nav className="w-48 shrink-0">
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

        {/* Main Content */}
        <main className="flex-1 min-w-0">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
