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
      <div className="flex min-h-screen bg-slate-50">
        {/* Sidebar */}
        <aside className="w-64 border-r border-slate-200 bg-white">
          <Menu />
        </aside>

        {/* Content area */}
        <div className="flex flex-1 flex-col">
          {/* Breadcrumb */}
          <header className="border-b border-slate-200 bg-white px-6 py-2">
            <Breadcrumb>
              <BreadcrumbItem>
                <Link to="/">Home</Link>
              </BreadcrumbItem>
              <BreadcrumbItem>
                <Link to="/apps">Apps</Link>
              </BreadcrumbItem>
              <BreadcrumbItem>{app.name}</BreadcrumbItem>
            </Breadcrumb>
          </header>

          {/* Main content */}
          <main className="flex-1 p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </AppContext.Provider>
  );
}

const Menu = () => {
  const { pathname } = useLocation();
  const parts = pathname.split("/")
  const part3 = parts[3];
  const part4 = parts[4];

  const activeOverview = part3 === undefined;
  const activeRoutes = part3 === "routes";
  const activeAuthenticationSchemes = part3 == "authentications" && part4 == "schemes";
  const activeAuthenticationSettings = part3 == "authentications" && part4 == "settings";
  const activeIntegrations = part3 == "integrations";
  const activeLogs = part3 == "logs";

  return <Sidebar>
    <SidebarItems>
      <SidebarItemGroup>
        <SidebarItem active={activeOverview}>
          <Link to="." >Overview</Link>
        </SidebarItem>
        <SidebarItem active={activeRoutes}>
          <Link to="routes">Routes</Link>
        </SidebarItem>
        <SidebarCollapse label="Authentications">
          <SidebarItem active={activeAuthenticationSchemes}>
            <Link to="authentications/schemes">Schemes</Link>
          </SidebarItem>
          <SidebarItem active={activeAuthenticationSettings}>
            <Link to="authentications/settings">Settings</Link>
          </SidebarItem>
        </SidebarCollapse>
        <SidebarItem active={activeIntegrations}>
          <Link to="integrations">Integrations</Link>
        </SidebarItem>
        <SidebarItem active={activeLogs}>
          <Link to="logs">Logs</Link>
        </SidebarItem>
      </SidebarItemGroup>
    </SidebarItems>
  </Sidebar>
}
