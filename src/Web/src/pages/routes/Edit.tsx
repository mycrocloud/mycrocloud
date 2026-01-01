import { useParams } from "react-router-dom";
import RouteCreateUpdate from "./CreateUpdateForm";
import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { toast } from "react-toastify";
import IRoute from "./Route";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import { useRoutesContext } from "./Context";
import { Spinner } from "flowbite-react";
import { useApiClient } from "@/hooks";

export default function RouteEdit() {
  const { get, put } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const {
    state: { routes },
    dispatch,
  } = useRoutesContext();

  const routeId = parseInt(useParams()["routeId"]!);
  const [route, setRoute] = useState<IRoute>();

  useEffect(() => {
    dispatch({
      type: "SET_ACTIVE_ROUTE",
      payload: routes.find((r) => r.id === routeId)!,
    });

    const getRoute = async () => {
      const route = await get<IRoute>(`/api/apps/${app.id}/routes/${routeId}`);
      setRoute(route);
    };
    getRoute();
  }, []);

  const onSubmit = async (data: RouteCreateUpdateInputs) => {
    await put(`/api/apps/${app.id}/routes/${routeId}`, data);

    toast("Route updated");

    if (!route) return;
    dispatch({ type: "UPDATE_ROUTE", payload: { ...route, name: data.name, method: data.method } });
  };

  if (!route) {
    return <Spinner />
  }

  return <RouteCreateUpdate route={route} onSubmit={onSubmit} />;
}
