import { Outlet, useParams, useMatch } from "react-router-dom";
import { useReducer } from "react";
import { RoutesContext, routesReducer } from "./Context";
import RouteExplorer from "./RouteExplorer";
import { File } from "lucide-react";

export default function RouteIndex() {
  const [state, dispatch] = useReducer(routesReducer, {
    routes: [],
    activeRoute: undefined,
  });
  const params = useParams();
  const routeId = params["routeId"] ? parseInt(params["routeId"]) : undefined;

  const newRouteActive = useMatch("/apps/:appId/api/routes/new");
  const editRouteActive = useMatch("/apps/:appId/api/routes/:routeId");
  const logPageActive = useMatch("/apps/:appId/api/routes/:routeId/logs");

  return (
    <RoutesContext.Provider value={{ state, dispatch }}>
      <div className="flex h-full">
        <div className="w-72 border-r bg-muted/20">
          <RouteExplorer />
        </div>
        <div className="flex-1 overflow-auto">
          {newRouteActive || editRouteActive || logPageActive ? (
            <Outlet key={routeId} />
          ) : (
            <div className="flex h-full items-center justify-center text-muted-foreground">
              <div className="text-center">
                <File className="mx-auto h-12 w-12 text-muted-foreground/50" />
                <p className="mt-2">Select a route to edit or create a new one</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </RoutesContext.Provider>
  );
}
