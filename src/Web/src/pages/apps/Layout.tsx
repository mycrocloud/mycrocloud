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
      <div className="flex h-screen">
        <aside className="w-64 shrink-0 border-r border-slate-200">
          <Menu />
        </aside>

        <div className="flex flex-1 flex-col overflow-hidden">
          <header className="border-b border-slate-200 px-6 py-2">
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

          <main className="flex-1 overflow-y-auto p-6">
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
  const activeSettings = part3 == "settings";

  return <Sidebar>
    <SidebarItems>
      <SidebarItemGroup>
        <SidebarItem
          as={Link}
          to="."
          active={activeOverview}
        >
          Overview
        </SidebarItem>
        <SidebarItem
          as={Link}
          to="routes"
          active={activeRoutes}
        >
          Routes
        </SidebarItem>
        <SidebarCollapse label="Authentications">
          <SidebarItem
            as={Link}
            to="authentications/schemes"
            active={activeAuthenticationSchemes}
          >
            Schemes
          </SidebarItem>
          <SidebarItem
            as={Link}
            to="authentications/settings"
            active={activeAuthenticationSettings}
          >
            Settings
          </SidebarItem>
        </SidebarCollapse>
        <SidebarItem
          as={Link}
          to="integrations"
          active={activeIntegrations}
        >
          Integrations
        </SidebarItem>
        <SidebarItem
          as={Link}
          to="logs"
          active={activeLogs}
        >
          Logs
        </SidebarItem>
        <SidebarItem
          as={Link}
          to="settings"
          active={activeSettings}
        >
          Settings
        </SidebarItem>
      </SidebarItemGroup>
    </SidebarItems>
  </Sidebar>
}
