import { useContext, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { toast } from "react-toastify";
import { useApiClient } from "@/hooks";
import { AppContext } from "../apps";
import { useRoutesContext } from "./Context";
import RouteCreateUpdate from "./CreateUpdateForm";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import IRoute from "./Route";

export default function RouteEdit() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const {
    state: { routes },
    dispatch,
  } = useRoutesContext();
  const { get, send } = useApiClient();
  const routeId = parseInt(useParams()["routeId"]!);
  const [route, setRoute] = useState<IRoute>();

  useEffect(() => {
    dispatch({
      type: "SET_ACTIVE_ROUTE",
      payload: routes.find((r) => r.id === routeId),
    });

    const loadRoute = async () => {
      try {
        const data = await get<IRoute>(`/api/apps/${app.id}/api/routes/${routeId}`);
        setRoute(data);
      } catch {
        toast.error("Failed to load route");
      }
    };
    loadRoute();
  }, [routeId, app.id, get, dispatch, routes]);

  const onSubmit = async (data: RouteCreateUpdateInputs) => {
    try {
      await send(`/api/apps/${app.id}/api/routes/${routeId}`, {
        method: "PUT",
        body: data,
      });
      toast.success("Route updated");
      const updatedRoute: IRoute = {
        ...route!,
        name: data.name,
        method: data.method,
        path: data.path,
        enabled: data.enabled,
      };
      dispatch({ type: "UPDATE_ROUTE", payload: updatedRoute });
    } catch {
      toast.error("Failed to update route");
    }
  };

  if (!route) {
    return null;
  }

  return <RouteCreateUpdate key={routeId} route={route} onSubmit={onSubmit} />;
}
