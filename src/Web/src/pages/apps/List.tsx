import { Link } from "react-router-dom";
import IApp from "./App";
import { useEffect, useMemo, useState } from "react";
import { useApiClient } from "@/hooks";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

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
    <div className="mx-auto mt-4 max-w-4xl space-y-4 p-4">
      <div className="flex items-center">
        <h1 className="text-2xl font-semibold">Apps</h1>
        <Button asChild className="ms-auto">
          <Link to={"new"}>New</Link>
        </Button>
      </div>
      <div>
        <Input
          type="text"
          id="search-input"
          onChange={(e) => {
            setSearchTerm(e.target.value);
          }}
          placeholder="Search..."
        />
      </div>
      <ul className="divide-y">
        {filteredApps.map((app) => {
          return (
            <li key={app.id} className="py-3">
              <div className="flex items-start justify-between">
                <div>
                  <div className="flex items-center gap-2">
                    <Link
                      to={`${app.id}`}
                      className="font-medium hover:underline"
                    >
                      {app.name}
                    </Link>
                    <span
                      className={`rounded-full px-2 py-0.5 text-xs ${
                        app.status === "Active"
                          ? "bg-green-100 text-green-700"
                          : "bg-red-100 text-red-700"
                      }`}
                    >
                      {app.status}
                    </span>
                  </div>
                  <p className="mt-1 text-sm text-muted-foreground">{app.description}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Created: {new Date(app.createdAt).toDateString()}
                  </p>
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
