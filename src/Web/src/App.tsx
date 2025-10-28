import { BrowserRouter, Route, Routes } from "react-router-dom";
import "./App.css";
import { Auth0Provider } from "@auth0/auth0-react";
import { ToastContainer } from "react-toastify";
import "react-toastify/dist/ReactToastify.css";

import Header from "./components/Header";
import Home from "./components/Home";
import ProtectedPage from "./components/ProtectedPage";

import {
  AppList,
  AppOverview,
  AppCreate,
  AppLog,
  AppLayout,
} from "./pages/apps";

import { RouteIndex, RouteLogs, RouteCreate, RouteEdit } from "./pages/routes";

import {
  AuthenticationSchemeCreateUpdate,
  AuthenticationSchemeList,
  AuthenticationSchemeSettings,
} from "./pages/authentications";

import {
  VariableList,
  VariableCreateUpdate,
} from "./pages/storages/VariablesAndSecrets";

import {
  CreateUpdateTextStorage,
  LogonTextStorage,
  TextStorageList,
} from "./pages/storages/TextStorages";

import { FileList } from "./pages/storages/files";

import { ObjectList } from "./pages/storages/Objects";

import About from "./pages/About";
import { NotFoundPage } from "./pages/NotFoundPage";
import { ApiKeyCreateUpdate, ApiKeyList } from "./pages/authentications/apikey";

import { default as Integrations } from "./pages/integrations";
import { default as IntegrationsGitHubCallback } from "./pages/settings/github_callback";
import Settings from "./pages/settings";
import { getConfig } from "./config";
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
        <Header />
        <div className="container mx-auto min-h-screen p-2">
          <Routes>
            <Route path="/" Component={Home} />
            <Route path="settings" Component={Settings} />
            <Route path="apps">
              <Route index element={<ProtectedPage children={<AppList />} />} />
              <Route
                path="new"
                element={<ProtectedPage children={<AppCreate />} />}
              />
              <Route
                path=":appId"
                element={<ProtectedPage children={<AppLayout />} />}
              >
                <Route index Component={AppOverview} />
                <Route path="routes" Component={RouteIndex}>
                  <Route path="new/:folderId?" Component={RouteCreate} />
                  <Route path=":routeId" Component={RouteEdit} />
                  <Route path=":routeId/logs" Component={RouteLogs} />
                </Route>
                <Route path="authentications">
                  <Route
                    index
                    path="schemes"
                    Component={AuthenticationSchemeList}
                  />
                  <Route
                    path="schemes/new"
                    Component={AuthenticationSchemeCreateUpdate}
                  />
                  <Route
                    path="schemes/:schemeId"
                    Component={AuthenticationSchemeCreateUpdate}
                  />
                  <Route
                    path="settings"
                    Component={AuthenticationSchemeSettings}
                  />
                  <Route path="apikeys">
                    <Route index Component={ApiKeyList} />
                    <Route path="new" Component={ApiKeyCreateUpdate} />
                    <Route path=":keyId/edit" Component={ApiKeyCreateUpdate} />
                  </Route>
                </Route>
                <Route path="logs" Component={AppLog} />
                <Route path="storages">
                  <Route path="files">
                    <Route index Component={FileList} />
                  </Route>
                  <Route path="textstorages">
                    <Route index Component={TextStorageList} />
                    <Route path="new" Component={CreateUpdateTextStorage} />
                    <Route
                      path=":storageId"
                      Component={CreateUpdateTextStorage}
                    />
                    <Route
                      path=":storageId/logon"
                      Component={LogonTextStorage}
                    />
                  </Route>
                  <Route path="objects">
                    <Route index Component={ObjectList} />
                  </Route>
                  <Route path="variables">
                    <Route index Component={VariableList} />
                    <Route path="new" Component={VariableCreateUpdate} />
                    <Route
                      path=":variableId/edit"
                      Component={VariableCreateUpdate}
                    />
                  </Route>
                </Route>
                <Route path="integrations" Component={Integrations}></Route>
              </Route>
            </Route>
            <Route path="integrations">
              <Route
                path="callback/github"
                Component={IntegrationsGitHubCallback}
              />
            </Route>
            <Route
              path="_about"
              element={<ProtectedPage children={<About />} />}
            />
            <Route path="*" Component={NotFoundPage} />
          </Routes>
        </div>
        <ToastContainer />
      </BrowserRouter>
    </Auth0Provider>
  );
}
export default App;
