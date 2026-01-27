import { Link } from "react-router-dom";
import IApp from "./App";
import { useEffect, useMemo, useState } from "react";
import { useApiClient } from "@/hooks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PlusIcon } from "@heroicons/react/24/outline";

export default function List() {
  const { get } = useApiClient();
  const [searchTerm, setSearchTerm] = useState("");
  const [apps, setApps] = useState<IApp[]>([]);
  const filteredApps = useMemo(() => {
    if (!searchTerm) {
      return apps;
    }

    return apps.filter((app) => {
      return app.name.toLowerCase().includes(searchTerm.toLowerCase());
    });
  }, [apps, searchTerm]);

  useEffect(() => {
    document.title = "Apps";

    const getApps = async () => {
      const apps = await get<IApp[]>("/api/apps");
      setApps(apps);
    };

    getApps();
  }, []);

  return (
    <div className="mx-auto max-w-4xl p-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Apps</h1>
          <p className="text-sm text-muted-foreground">
            Manage your applications
          </p>
        </div>
        <Button asChild>
          <Link to={"new"}>
            <PlusIcon className="mr-1 h-4 w-4" />
            New App
          </Link>
        </Button>
      </div>

      <div className="mt-6">
        <Input
          type="text"
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Search apps..."
          className="max-w-sm"
        />
      </div>

      <div className="mt-6 grid gap-4">
        {filteredApps.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center">
            <p className="text-muted-foreground">No apps found</p>
          </div>
        ) : (
          filteredApps.map((app) => (
            <Link
              key={app.id}
              to={`${app.id}`}
              className="group block rounded-lg border bg-background p-4 transition-colors hover:border-primary hover:bg-accent/50"
            >
              <div className="flex items-start justify-between">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <h3 className="font-medium group-hover:text-primary">
                      {app.name}
                    </h3>
                    <span
                      className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                        app.status === "Active"
                          ? "bg-green-50 text-green-700 ring-1 ring-inset ring-green-600/20"
                          : "bg-red-50 text-red-700 ring-1 ring-inset ring-red-600/20"
                      }`}
                    >
                      {app.status}
                    </span>
                  </div>
                  {app.description && (
                    <p className="text-sm text-muted-foreground">
                      {app.description}
                    </p>
                  )}
                </div>
                <span className="text-xs text-muted-foreground">
                  {new Date(app.createdAt).toLocaleDateString()}
                </span>
              </div>
            </Link>
          ))
        )}
      </div>
    </div>
  );
}
