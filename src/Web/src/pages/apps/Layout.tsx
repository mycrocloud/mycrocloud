import { Link, Outlet, useLocation, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import { Breadcrumb, BreadcrumbItem, Sidebar, SidebarCollapse, SidebarItem, SidebarItemGroup, SidebarItems, Spinner } from "flowbite-react";
import { useApiClient } from "@/hooks";

export default function AppLayout() {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();

  useEffect(() => {
    const getApp = async () => {
      const app = await get<IApp>(`/api/apps/${appId}`);
      setApp(app);
    };
    getApp();
  }, []);

  if (!app) {
    return <Spinner />
  }

  return (
    <AppContext.Provider value={{ app, setApp }}>
      <div className="">
        <Breadcrumb className="p-1">
          <BreadcrumbItem>
            <Link to="/">Home</Link>
          </BreadcrumbItem>
          <BreadcrumbItem>
            <Link to="/apps">Apps</Link>
          </BreadcrumbItem>
          <BreadcrumbItem>{app.name}</BreadcrumbItem>
        </Breadcrumb>
        <div className="flex min-h-screen">
          <aside className="w-64 border-r border-slate-200 bg-white">
            <Menu />
          </aside>
          <main className="flex-1 p-6 bg-slate-50">
            <Outlet />
          </main>
        </div>
      </div>
    </AppContext.Provider>
  );
}

function Menu() {
  const { pathname } = useLocation();
  const parts = pathname.split("/")
  const part3 = parts[3];
  const part4 = parts[4];

  const isMatch_Overview = part3 === undefined;
  const isMatch_Routes = part3 === "routes";
  const isMatchAuthenticationSchemes = part3 == "authentications" && part4 == "schemes";
  const isMatchAuthenticationSettings = part3 == "authentications" && part4 == "settings";
  const isMatchIntegrations = part3 == "integrations";
  const isMatchLogs = part3 == "logs";

  return <Sidebar>
    <SidebarItems>
      <SidebarItemGroup>
        <SidebarItem active={isMatch_Overview}>
          <Link to="." >Overview</Link>
        </SidebarItem>
        <SidebarItem active={isMatch_Routes}>
          <Link to="routes">Routes</Link>
        </SidebarItem>
        <SidebarCollapse label="Authentications">
          <SidebarItem active={isMatchAuthenticationSchemes}>
            <Link to="authentications/schemes">Schemes</Link>
          </SidebarItem>
          <SidebarItem active={isMatchAuthenticationSettings}>
            <Link to="authentications/settings">Settings</Link>
          </SidebarItem>
        </SidebarCollapse>
        <SidebarItem active={isMatchIntegrations}>
          <Link to="integrations">Integrations</Link>
        </SidebarItem>
        <SidebarItem active={isMatchLogs}>
          <Link to="logs">Logs</Link>
        </SidebarItem>
      </SidebarItemGroup>
    </SidebarItems>
  </Sidebar>
}
