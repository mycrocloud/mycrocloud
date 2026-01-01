import RouteCreateUpdate from "./CreateUpdateForm";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import { useContext } from "react";
import { AppContext } from "../apps";
import { toast } from "react-toastify";
import IRoute from "./Route";
import { useRoutesContext } from "./Context";
import { useNavigate, useParams } from "react-router-dom";
import { useApiClient } from "@/hooks";

export default function RouteCreate() {
  const navigate = useNavigate();
  const { post } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { dispatch } = useRoutesContext();

  const folderId = useParams()["folderId"];
  const onSubmit = async (data: RouteCreateUpdateInputs) => {
    const newRoute = await post<IRoute>(`/api/apps/${app.id}/routes`,
      {
        ...data,
        folderId: folderId ? parseInt(folderId) : null,
      }
    );

    dispatch({ type: "ADD_ROUTE", payload: newRoute });

    toast("Route created");

    navigate(`../${newRoute.id}`);
  };
  
  return <RouteCreateUpdate onSubmit={onSubmit} />;
}
