import { Link } from "react-router-dom";
import IApp from "./App";
import { useEffect, useMemo, useState } from "react";
import { useApiClient } from "@/hooks";

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
      <div className="flex items-center">
        <h1 className="font-semibold">Apps</h1>
        <Link to={"new"} className="ms-auto bg-primary px-2 py-1 text-white">
          New
        </Link>
      </div>
      <form className="mt-2">
        <label
          htmlFor="search-input"
          className="sr-only mb-2 text-sm font-medium text-gray-900"
        >
          Search
        </label>
        <input
          type="text"
          id="search-input"
          onChange={(e) => {
            setSearchTerm(e.target.value);
          }}
          placeholder="Search..."
          className="block w-full border border-gray-300 bg-gray-50 p-1.5 text-sm text-gray-900 focus:border-blue-500 focus:ring-blue-500"
        />
      </form>
      <ul className="mt-3 divide-y">
        {filteredApps.map((app) => {
          return (
            <li key={app.id}>
              <div className="mb-2">
                <h4>
                  <Link
                    to={`${app.id}`}
                    className="font-semibold text-slate-900"
                  >
                    {app.name}
                  </Link>
                  <small
                    className={`ms-1 ${
                      app.status === "Active"
                        ? "text-green-500"
                        : "text-red-500"
                    }`}
                  >
                    {app.status}
                  </small>
                </h4>
                <p className="text-sm text-slate-600">{app.description}</p>
                <small className="text-sm text-slate-600">
                  Created: {new Date(app.createdAt).toDateString()}
                </small>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
