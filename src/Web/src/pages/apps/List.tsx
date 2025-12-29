import { Link } from "react-router-dom";
import IApp from "./App";
import { useEffect, useMemo, useState } from "react";
import { useApiClient } from "@/hooks";
import { Button, TextInput } from "flowbite-react";

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
    const getApps = async () => {
      const apps = await get<IApp[]>("/api/apps");
      setApps(apps);
    };

    getApps();
  }, []);

  return (
    <div className="mx-auto max-w-4xl space-y-4 p-4">
      {/* Header */}
      <header className="flex items-center justify-between">
        <h1 className="text-lg font-semibold">Apps</h1>
        <Button as={Link} to="new">
          New
        </Button>
      </header>

      {/* Search */}
      <form role="search">
        <TextInput
          id="search-input"
          placeholder="Search..."
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </form>

      {/* List */}
      <ul className="divide-y rounded-xl border border-slate-200">
        {filteredApps.map(app => (
          <li key={app.id} className="px-6 py-5">
            <div className="space-y-1">
              <div className="flex items-center gap-2">
                <Link
                  to={`${app.id}`}
                  className="text-lg font-semibold text-slate-900 hover:underline"
                >
                  {app.name}
                </Link>

                <span className="rounded-full bg-green-50 px-2 py-0.5 text-xs font-medium text-green-700">
                  {app.status}
                </span>
              </div>

              <time className="block text-xs text-slate-500">
                Created: {new Date(app.createdAt).toDateString()}
              </time>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
