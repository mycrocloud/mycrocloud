import { BrowserRouter, Route, Routes } from "react-router-dom";
import "./App.css";
import { Auth0Provider } from "@auth0/auth0-react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

import Header from "./components/Header";
import ProtectedPage from "./components/ProtectedPage";
import { getConfig } from "./config";
import Home from "./pages/Home";
import { NotFoundPage } from "./pages/NotFoundPage";
import {
  AppList,
  AppOverview,
  AppCreate,
  AppLog,
  AppBuilds,
  AppBuildDetails,
  AppLayout,
} from "./pages/apps";
import AppSettings, { GeneralTab, ApiTab, PagesTab } from "./pages/apps/Settings";
import { RouteIndex, RouteCreate, RouteEdit } from "./pages/routes";
import Settings from "./pages/settings";
import Connections from "./pages/settings/Connections";
import Tokens from "./pages/settings/Tokens";
import TokenCreate from "./pages/settings/TokenCreate";
import TokenEdit from "./pages/settings/TokenEdit";
import IntegrationsGitHubCallback from "./pages/settings/github_callback";
import IntegrationsSlackCallback from "./pages/settings/slack_callback";
import IntegrationsSlackLink from "./pages/settings/slack_link";

const { AUTH0_DOMAIN, AUTH0_CLIENTID, AUTH0_AUDIENCE } = getConfig();

function App() {
  return (
    <Auth0Provider
      domain={AUTH0_DOMAIN}
      clientId={AUTH0_CLIENTID}
      authorizationParams={{
        redirect_uri: window.location.origin,
        audience: AUTH0_AUDIENCE,
      }}
    >
      <BrowserRouter>
        <div className="flex min-h-screen flex-col">
          <Header />
          <main className="flex-1">
            <Routes>
              <Route path="/" element={<Home />} />
              <Route
                path="settings"
                element={<ProtectedPage><Settings /></ProtectedPage>}
              >
                <Route path="connections" element={<Connections />} />
                <Route path="tokens" element={<Tokens />}>
                  <Route path="new" element={<TokenCreate />} />
                  <Route path=":id/edit" element={<TokenEdit />} />
                </Route>
              </Route>
              <Route path="apps">
                <Route
                  index
                  element={<ProtectedPage><AppList /></ProtectedPage>}
                />
                <Route
                  path="new"
                  element={<ProtectedPage><AppCreate /></ProtectedPage>}
                />
                <Route
                  path=":appId"
                  element={<ProtectedPage><AppLayout /></ProtectedPage>}
                >
                  <Route index element={<AppOverview />} />
                  <Route path="routes" element={<RouteIndex />}>
                    <Route path="new/:folderId?" element={<RouteCreate />} />
                    <Route path=":routeId" element={<RouteEdit />} />
                  </Route>
                  <Route path="logs" element={<AppLog />} />
                  <Route path="builds" element={<AppBuilds />} />
                  <Route path="builds/:buildId" element={<AppBuildDetails />} />
                  <Route path="settings" element={<AppSettings />}>
                    <Route path="general" element={<GeneralTab />} />
                    <Route path="api" element={<ApiTab />} />
                    <Route path="pages" element={<PagesTab />} />
                  </Route>
                </Route>
              </Route>
              <Route path="integrations">
                <Route
                  path="callback/github"
                  element={<IntegrationsGitHubCallback />}
                />
                <Route
                  path="slack/oauth/callback"
                  element={<IntegrationsSlackCallback />}
                />
                <Route path="slack/link" element={<IntegrationsSlackLink />} />
              </Route>
              <Route path="*" element={<NotFoundPage />} />
            </Routes>
          </main>
        </div>
        <ToastContainer />
      </BrowserRouter>
    </Auth0Provider>
  );
}
export default App;
