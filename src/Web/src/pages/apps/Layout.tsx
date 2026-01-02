import { Link, Outlet, useMatch, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import { Breadcrumb, BreadcrumbItem, Sidebar, SidebarCollapse, SidebarItem, SidebarItemGroup, SidebarItems, Spinner, theme } from "flowbite-react";
import { useApiClient } from "@/hooks";
import { twMerge } from "flowbite-react/helpers/tailwind-merge";
import { getAppDomain } from "./service";

export default function AppLayout() {
  const { get } = useApiClient();
  const appId = parseInt(useParams()["appId"]!.toString());
  const [app, setApp] = useState<IApp>();

  useEffect(() => {
    const getApp = async () => {
      const app = await get<IApp>(`/api/apps/${appId}`);
      app.domain = getAppDomain(app.id);
      setApp(app);
    };
    getApp();
  }, []);

  if (!app) {
    return <Spinner />
  }

  return (
    <AppContext.Provider value={{ app, setApp }}>
      <div className="flex h-screen overflow-hidden">
        <aside>
          <Menu />
        </aside>

        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          <header className="shrink-0 border-b border-slate-200 px-6 py-2">
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

          <main className="min-h-0 flex-1 overflow-hidden p-6">
            <Outlet />
          </main>
        </div>
      </div>
    </AppContext.Provider>
  );
}

const Menu = () => {
  const activeOverview = useMatch("/apps/:appId");
  const activeRoutes = useMatch("/apps/:appId/routes/*");
  const activeAuthenticationSchemes = useMatch("/apps/:appId/authentications/schemes");
  const activeAuthenticationSettings = useMatch("/apps/:appId/authentications/settings");
  const activeDeployments = useMatch("/apps/:appId/deployments");
  const activeLogs = useMatch("/apps/:appId/logs");
  const activeSettings = useMatch("/apps/:appId/settings");

  return <Sidebar
    theme={{
      root: {
        inner: twMerge(
          theme.sidebar.root.inner,
          "bg-white border-r border-slate-200"
        ),
      },
    }}
  >
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
          to="deployments"
          active={activeDeployments}
        >
          Deployments
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
