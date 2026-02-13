import { Link } from "react-router-dom";
import IApp from "./App";
import { useEffect, useMemo, useState } from "react";
import { useApiClient } from "@/hooks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Plus,
  Search,
  Box,
  ArrowRight,
  Loader2,
  Calendar,
} from "lucide-react";

export default function List() {
  const { get } = useApiClient();
  const [searchTerm, setSearchTerm] = useState("");
  const [apps, setApps] = useState<IApp[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const filteredApps = useMemo(() => {
    if (!searchTerm) {
      return apps;
    }
    return apps.filter((app) =>
      app.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }, [apps, searchTerm]);

  useEffect(() => {
    document.title = "Apps";

    const getApps = async () => {
      try {
        const apps = await get<IApp[]>("/api/apps");
        setApps(apps);
      } finally {
        setIsLoading(false);
      }
    };

    getApps();
  }, []);

  return (
    <div className="mx-auto max-w-4xl px-4 py-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight">Your Apps</h1>
          <p className="text-muted-foreground">
            Create and manage your applications
          </p>
        </div>
        <Button asChild>
          <Link to="new">
            <Plus className="mr-2 h-4 w-4" />
            New App
          </Link>
        </Button>
      </div>

      {/* Search */}
      {apps.length > 0 && (
        <div className="relative mt-6">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="text"
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Search apps..."
            className="max-w-sm pl-9"
          />
        </div>
      )}

      {/* Content */}
      <div className="mt-6">
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : apps.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-16">
            <div className="flex h-14 w-14 items-center justify-center rounded-full bg-muted">
              <Box className="h-7 w-7 text-muted-foreground" />
            </div>
            <h3 className="mt-4 text-lg font-medium">No apps yet</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Get started by creating your first application
            </p>
            <Button asChild className="mt-6">
              <Link to="new">
                <Plus className="mr-2 h-4 w-4" />
                Create App
              </Link>
            </Button>
          </div>
        ) : filteredApps.length === 0 ? (
          <div className="flex flex-col items-center justify-center rounded-lg border border-dashed py-12">
            <Search className="h-8 w-8 text-muted-foreground" />
            <p className="mt-4 text-muted-foreground">
              No apps matching "{searchTerm}"
            </p>
          </div>
        ) : (
          <div className="grid gap-3">
            {filteredApps.map((app) => (
              <Link
                key={app.id}
                to={`${app.id}`}
                className="group flex items-center justify-between rounded-lg border bg-card p-4 transition-all hover:border-primary hover:shadow-sm"
              >
                <div className="flex items-center gap-4">
                  <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                    <Box className="h-5 w-5 text-primary" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-medium group-hover:text-primary">
                        {app.name}
                      </h3>
                      <Badge
                        variant={app.state === "Active" ? "default" : "destructive"}
                        className="text-xs"
                      >
                        {app.state}
                      </Badge>
                    </div>
                    {app.description ? (
                      <p className="mt-0.5 text-sm text-muted-foreground line-clamp-1">
                        {app.description}
                      </p>
                    ) : (
                      <p className="mt-0.5 flex items-center gap-1 text-xs text-muted-foreground">
                        <Calendar className="h-3 w-3" />
                        Created {new Date(app.createdAt).toLocaleDateString()}
                      </p>
                    )}
                  </div>
                </div>
                <ArrowRight className="h-5 w-5 text-muted-foreground opacity-0 transition-opacity group-hover:opacity-100" />
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
