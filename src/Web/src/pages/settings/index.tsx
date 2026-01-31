import { useState } from "react";
import { Settings as SettingsIcon, Link2, Key } from "lucide-react";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import Connections from "./Connections";
import Tokens from "./Tokens";

type Section = "connections" | "tokens";

const navItems: { id: Section; label: string; icon: React.ElementType; description: string }[] = [
  {
    id: "connections",
    label: "Connections",
    icon: Link2,
    description: "Third-party integrations",
  },
  {
    id: "tokens",
    label: "API Tokens",
    icon: Key,
    description: "Manage access tokens",
  },
];

export default function Settings() {
  const [activeSection, setActiveSection] = useState<Section>("connections");

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
              const isActive = activeSection === item.id;
              return (
                <Button
                  key={item.id}
                  variant="ghost"
                  onClick={() => setActiveSection(item.id)}
                  className={cn(
                    "w-full justify-start gap-3 h-auto py-3 px-3",
                    isActive && "bg-muted"
                  )}
                >
                  <Icon className={cn("h-5 w-5", isActive ? "text-primary" : "text-muted-foreground")} />
                  <div className="text-left">
                    <div className={cn("font-medium", !isActive && "text-foreground")}>
                      {item.label}
                    </div>
                    <div className="text-xs text-muted-foreground font-normal">
                      {item.description}
                    </div>
                  </div>
                </Button>
              );
            })}
          </div>
        </nav>

        {/* Main Content */}
        <main className="flex-1 min-w-0">
          {activeSection === "connections" && <Connections />}
          {activeSection === "tokens" && <Tokens />}
        </main>
      </div>
    </div>
  );
}
