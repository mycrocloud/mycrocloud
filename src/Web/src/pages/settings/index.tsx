import { Link, Outlet, useLocation, Navigate } from "react-router-dom";
import { Settings as SettingsIcon, Link2, Key } from "lucide-react";
import { cn } from "@/lib/utils";

const navItems = [
  {
    path: "connections",
    label: "Connections",
    icon: Link2,
    description: "Third-party integrations",
  },
  {
    path: "tokens",
    label: "API Tokens",
    icon: Key,
    description: "Manage access tokens",
  },
];

export default function Settings() {
  const location = useLocation();

  // Redirect /settings to /settings/connections
  if (location.pathname === "/settings") {
    return <Navigate to="/settings/connections" replace />;
  }

  const isActive = (path: string) => location.pathname === `/settings/${path}`;

  return (
    <div className="container max-w-6xl py-8">
      {/* Page Header */}
      <div className="mb-8">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
            <SettingsIcon className="h-5 w-5 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold tracking-tight">Account Settings</h1>
            <p className="text-sm text-muted-foreground">
              Manage your account preferences and integrations
            </p>
          </div>
        </div>
      </div>

      {/* Sidebar Layout */}
      <div className="flex gap-8">
        {/* Sidebar Navigation */}
        <nav className="w-64 shrink-0">
          <div className="space-y-1">
            {navItems.map((item) => {
              const Icon = item.icon;
              const active = isActive(item.path);
              return (
                <Link
                  key={item.path}
                  to={item.path}
                  className={cn(
                    "flex items-center gap-3 rounded-lg px-3 py-3 transition-colors hover:bg-muted",
                    active && "bg-muted"
                  )}
                >
                  <Icon
                    className={cn(
                      "h-5 w-5",
                      active ? "text-primary" : "text-muted-foreground"
                    )}
                  />
                  <div>
                    <div className={cn("text-sm font-medium", !active && "text-foreground")}>
                      {item.label}
                    </div>
                    <div className="text-xs text-muted-foreground">
                      {item.description}
                    </div>
                  </div>
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
