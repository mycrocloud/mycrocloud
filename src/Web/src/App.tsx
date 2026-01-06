import { BrowserRouter, Route, Routes } from "react-router-dom";
import "./App.css";
import { Auth0Provider } from "@auth0/auth0-react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

import PublicLayout from "./components/PublicLayout";
import ProtectedLayout from "./components/ProtectedLayout";

import {
  AppList,
  AppOverview,
  AppCreate,
  AppLog,
  AppLayout,
} from "./pages/apps";

import { RouteIndex, RouteCreate, RouteEdit } from "./pages/routes";

import { NotFoundPage } from "./pages/NotFoundPage";

import { default as AppBuilds } from "./pages/builds";
import { default as IntegrationsGitHubCallback } from "./pages/settings/github_callback";
import { default as IntegrationsSlackCallback } from "./pages/settings/slack_callback";
import { default as IntegrationsSlackLink } from "./pages/settings/slack_link";

import Settings from "./pages/settings";
import { getConfig } from "./config";
import Home from "./pages/Home";
import AppSettings from "./pages/apps/settings";
import AppBuild from "./pages/builds/Build";

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
        <Routes>
          {/* Public Routes */}
          <Route element={<PublicLayout />}>
            <Route path="/" Component={Home} />
            <Route path="integrations">
              <Route path="callback/github" Component={IntegrationsGitHubCallback} />
              <Route path="slack/oauth/callback" Component={IntegrationsSlackCallback} />
              <Route path="slack/link" Component={IntegrationsSlackLink} />
            </Route>
          </Route>

          {/* Protected Routes */}
          <Route element={<ProtectedLayout />}>
            <Route path="settings" Component={Settings} />

            <Route path="apps">
              <Route index Component={AppList} />
              <Route path="new" Component={AppCreate} />
              <Route path=":appId" element={<AppLayout />}>
                <Route index Component={AppOverview} />
                <Route path="routes" Component={RouteIndex}>
                  <Route path="new/:folderId?" Component={RouteCreate} />
                  <Route path=":routeId" Component={RouteEdit} />
                </Route>
                <Route path="logs" Component={AppLog} />
                <Route path="deployments">
                  <Route index Component={AppBuilds} />
                  <Route path="builds/:buildId" Component={AppBuild} />
                </Route>
                <Route path="settings" Component={AppSettings} />
              </Route>
            </Route>
          </Route>

          {/* 404 - Can be accessed by anyone */}
          <Route path="*" Component={NotFoundPage} />
        </Routes>

        <ToastContainer />
      </BrowserRouter>
    </Auth0Provider>
  );
}
export default App;