import { Link, Outlet, useLocation } from "react-router-dom";
import { Breadcrumb, BreadcrumbItem } from "flowbite-react";
import { AppProvider, useApp } from "./context";

export default function AppLayout() {
  return (
    <AppProvider>
      <AppLayoutInner />
    </AppProvider>
  );
}

function AppLayoutInner() {
  const { app, loading } = useApp();

  if (loading) return <h1>Loading...</h1>;
  if (!app) return <h1>App not found</h1>;

  const { pathname } = useLocation();
  const part3 = pathname.split("/")[3];
  const part4 = pathname.split("/")[4];

  const isMatch_Overview = part3 === undefined;
  const isMatch_Routes = part3 === "routes";
  const isMatchAuthenticationSchemes =
    part3 == "authentications" && part4 === "schemes";
  const isMatchAuthenticationSettings =
    part3 == "authentications" && part4 === "settings";

  const isMatchFileStorages = part3 == "storages" && part4 === "files";
  const isMatchTextStorages = part3 == "storages" && part4 === "textstorages";
  const isMatchVariables = part3 == "storages" && part4 === "variables";

  const isMatchLogs = part3 == "logs";
  const isMatchIntegrations = part3 == "integrations";
  
  return <div className="">
    <Breadcrumb className="bg-gray-50 px-5 py-3 dark:bg-gray-800">
      <BreadcrumbItem>
        <Link to="/apps">Apps</Link>
      </BreadcrumbItem>
      <BreadcrumbItem>{app.name}</BreadcrumbItem>
    </Breadcrumb>
    <div className="flex min-h-screen border">
      <div className="flex w-28 flex-col space-y-0.5 border-r p-1">
        <Link
          to=""
          className={`text-xs ${isMatch_Overview ? "text-primary" : ""}`}
        >
          Overview
        </Link>
        <Link
          to="routes"
          className={`text-xs ${isMatch_Routes ? "text-primary" : ""}`}
        >
          Routes
        </Link>
        <div className="text-xs">
          Authentications
          <div className="flex flex-col pl-1">
            <Link
              to="authentications/schemes"
              className={`text-xs ${isMatchAuthenticationSchemes ? "text-primary" : ""
                }`}
            >
              Schemes
            </Link>
            <Link
              to="authentications/settings"
              className={`text-xs ${isMatchAuthenticationSettings ? "text-primary" : ""
                }`}
            >
              Settings
            </Link>
          </div>
        </div>
        <div className="text-xs">
          Storages
          <div className="flex flex-col px-1">
            <Link
              to="storages/files"
              className={`text-xs ${isMatchFileStorages ? "text-primary" : ""
                }`}
            >
              Files
            </Link>
            <Link
              to="storages/textstorages"
              className={`text-xs ${isMatchTextStorages ? "text-primary" : ""
                }`}
            >
              Text Storages
            </Link>
            <Link
              to="storages/variables"
              className={`text-xs ${isMatchVariables ? "text-primary" : ""
                }`}
            >
              Variables
            </Link>
          </div>
        </div>
        <Link
          to="integrations"
          className={`text-xs ${isMatchIntegrations ? "text-primary" : ""}`}
        >
          Integrations
        </Link>
        <Link
          to="logs"
          className={`text-xs ${isMatchLogs ? "text-primary" : ""}`}
        >
          Logs
        </Link>
      </div>
      <div className="flex-1">
        <Outlet />
      </div>
    </div>

  </div>
}
