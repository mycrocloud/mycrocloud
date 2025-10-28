import { Link, useParams } from "react-router-dom";
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
      payload: routes.find((r) => r.id === routeId)!,
    });
    const getRoute = async () => {
      const accessToken = await getAccessTokenSilently();
      const route = (await (
        await fetch(`/api/apps/${app.id}/routes/${routeId}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        })
      ).json()) as IRoute;
      setRoute(route);
    };
    getRoute();
  }, []);
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
      toast("Route updated");
      route!.name = data.name;
      route!.method = data.method;
      dispatch({ type: "UPDATE_ROUTE", payload: route! });
    }
  };

  if (!route) {
    return null;
  }
  return (
    <>
      <div className="mb-1 border-b border-gray-200">
        <div className="flex items-end justify-end space-x-1 px-2">
          <Link to={"logs"}>Logs</Link>
          {/* <button className="border px-3 py-0.5">Test</button> */}
          {/* <button className="border px-3 py-0.5">Share</button>
                    <button className="border px-3 py-0.5">History</button>
                    <button className="border px-3 py-0.5">Settings</button> */}
        </div>
      </div>
      <RouteCreateUpdate key={routeId} route={route} onSubmit={onSubmit} />
    </>
  );
}
