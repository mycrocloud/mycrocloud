import { Link, Outlet, useLocation, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useApiClient } from "@/hooks";

export default function AppLayout() {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();
  const { pathname } = useLocation();
  const part3 = pathname.split("/")[3];

  const isMatch_Overview = part3 === undefined;
  const isMatch_Routes = part3 === "routes";
  const isMatchLogs = part3 === "logs";
  const isMatchBuilds = part3 === "builds";
  const isMatchSettings = part3 === "settings";

  useEffect(() => {
    const getApp = async () => {
      const app = await get<IApp>(`/api/apps/${appId}`);
      setApp(app);
    };
    getApp();
  }, []);

  if (!app) {
    return <h1>Loading...</h1>;
  }
  return (
    <AppContext.Provider value={{ app, setApp }}>
      <div className="">
        <Breadcrumb className="p-1">
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbLink asChild>
                <Link to="/">Home</Link>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbLink asChild>
                <Link to="/apps">Apps</Link>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbSeparator />
            <BreadcrumbItem>
              <BreadcrumbPage>{app.name}</BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
        <div className="flex min-h-screen border">
          <div className="flex w-28 flex-col space-y-0.5 border-r p-1">
            <Link
              to=""
              className={`text-xs ${isMatch_Overview ? "text-primary" : ""}`}
            >
              Overview
            </Link>
            <div className="text-xs text-muted-foreground pt-2">API</div>
            <div className="flex flex-col space-y-0.5 pl-2">
              <Link
                to="routes"
                className={`text-xs ${isMatch_Routes ? "text-primary" : ""}`}
              >
                Routes
              </Link>
              <Link
                to="logs"
                className={`text-xs ${isMatchLogs ? "text-primary" : ""}`}
              >
                Logs
              </Link>
            </div>
            <div className="text-xs text-muted-foreground pt-2">Pages</div>
            <div className="flex flex-col space-y-0.5 pl-2">
              <Link
                to="builds"
                className={`text-xs ${isMatchBuilds ? "text-primary" : ""}`}
              >
                Builds
              </Link>
            </div>
            <Link
              to="settings"
              className={`text-xs pt-2 ${isMatchSettings ? "text-primary" : ""}`}
            >
              Settings
            </Link>
          </div>
          <div className="flex-1">
            <Outlet />
          </div>
        </div>
      </div>
    </AppContext.Provider>
  );
}
