import { Link, Outlet, useLocation, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import { Breadcrumb } from "flowbite-react";
import { useApiClient } from "@/hooks";

export default function AppLayout() {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();
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
          <Breadcrumb.Item>
            <Link to="/">Home</Link>
          </Breadcrumb.Item>
          <Breadcrumb.Item>
            <Link to="/apps">Apps</Link>
          </Breadcrumb.Item>
          <Breadcrumb.Item>{app.name}</Breadcrumb.Item>
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
                  className={`text-xs ${
                    isMatchAuthenticationSchemes ? "text-primary" : ""
                  }`}
                >
                  Schemes
                </Link>
                <Link
                  to="authentications/settings"
                  className={`text-xs ${
                    isMatchAuthenticationSettings ? "text-primary" : ""
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
                  className={`text-xs ${
                    isMatchFileStorages ? "text-primary" : ""
                  }`}
                >
                  Files
                </Link>
                <Link
                  to="storages/textstorages"
                  className={`text-xs ${
                    isMatchTextStorages ? "text-primary" : ""
                  }`}
                >
                  Text Storages
                </Link>
                <Link
                  to="storages/variables"
                  className={`text-xs ${
                    isMatchVariables ? "text-primary" : ""
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
    </AppContext.Provider>
  );
}
