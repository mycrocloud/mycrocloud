import { Link, Outlet, useMatch, useParams } from "react-router-dom";
import { AppContext } from ".";
import { useEffect, useState } from "react";
import IApp from "./App";
import { Sidebar, SidebarItem, SidebarItemGroup, SidebarItems, Spinner, theme } from "flowbite-react";
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
        <aside className="shrink-0 overflow-y-auto">
          <div className="h-screen">
            <Menu />
          </div>
        </aside>

        <div className="flex min-h-0 flex-1 flex-col">
          <main className="min-h-0 flex-1 overflow-y-auto p-6">
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
