import { useParams } from "react-router-dom";
import RouteCreateUpdate from "./CreateUpdateForm";
import { useAuth0 } from "@auth0/auth0-react";
import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { toast } from "react-toastify";
import IRoute from "./Route";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import { useRoutesContext } from "./Context";

export default function RouteEdit() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const {
    state: { routes },
    dispatch,
  } = useRoutesContext();
  const { getAccessTokenSilently } = useAuth0();
  const routeId = parseInt(useParams()["routeId"]!);
  const [route, setRoute] = useState<IRoute>();

  useEffect(() => {
    dispatch({
      type: "SET_ACTIVE_ROUTE",
      payload: routes.find((r) => r.id === routeId),
    });
    const getRoute = async () => {
      try {
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/routes/${routeId}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        });
        if (!res.ok) {
          toast.error("Failed to load route");
          return;
        }
        const route = (await res.json()) as IRoute;
        setRoute(route);
      } catch {
        toast.error("Failed to load route");
      }
    };
    getRoute();
  }, [routeId]);

  const onSubmit = async (data: RouteCreateUpdateInputs) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/routes/${routeId}`, {
      method: "PUT",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(data),
    });
    if (res.ok) {
      toast.success("Route updated");
      const updatedRoute: IRoute = {
        ...route!,
        name: data.name,
        method: data.method,
        path: data.path,
        enabled: data.enabled,
      };
      dispatch({ type: "UPDATE_ROUTE", payload: updatedRoute });
    } else {
      toast.error("Failed to update route");
    }
  };

  if (!route) {
    return null;
  }
  return (
    <>
      <RouteCreateUpdate key={routeId} route={route} onSubmit={onSubmit} />
    </>
  );
}
