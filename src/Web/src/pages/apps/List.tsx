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
    document.title = "Apps";

    const getApps = async () => {
      const apps = await get<IApp[]>("/api/apps");
      setApps(apps);
    };

    getApps();
  }, []);

  return (
    <div className="mx-auto mt-2 max-w-4xl p-2">
      <div className="flex items-center justify-between">
        <h1 className="font-semibold">Apps</h1>
        <Button as={Link} to={"new"} size="sm">
          New
        </Button>
      </div>
      <form className="mt-2">
        <TextInput
          id="search-input"
          onChange={(e) => { setSearchTerm(e.target.value); }}
          placeholder="Search..."
        />
      </form>
      <ul className="mt-4 space-y-4">
        {filteredApps.map((app) => (
          <li
            key={app.id}
            className="p-4 border rounded-xl hover:shadow-sm transition bg-white dark:bg-slate-800"
          >
            <div className="flex items-start justify-between">
              <div>
                <Link
                  to={`${app.id}`}
                  className="font-semibold text-lg text-slate-900 dark:text-slate-100 hover:text-blue-600 dark:hover:text-blue-400"
                >
                  {app.name}
                </Link>

                <p className="text-sm text-slate-600 dark:text-slate-400 mt-1">
                  {app.description}
                </p>

                <div className="mt-2 text-xs text-slate-500 dark:text-slate-400">
                  Created: {new Date(app.createdAt).toDateString()}
                </div>
              </div>

              <span
                className={`text-sm font-medium px-2 py-1 rounded-full ${app.status === "Active"
                    ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300"
                    : "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-300"
                  }`}
              >
                {app.status}
              </span>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
