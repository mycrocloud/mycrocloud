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
          onChange={(e) => { setSearchTerm(e.target.value);}}
          placeholder="Search..."
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
