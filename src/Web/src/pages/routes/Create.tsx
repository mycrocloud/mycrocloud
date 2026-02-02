import { useContext } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "react-toastify";
import { useApiClient } from "@/hooks";
import { AppContext } from "../apps";
import { useRoutesContext } from "./Context";
import RouteCreateUpdate from "./CreateUpdateForm";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import IRoute from "./Route";

export default function RouteCreate() {
  const navigate = useNavigate();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { dispatch } = useRoutesContext();
  const { post } = useApiClient();
  const folderId = useParams()["folderId"];

  const onSubmit = async (data: RouteCreateUpdateInputs) => {
    try {
      const newRoute = await post<IRoute>(`/api/apps/${app.id}/routes`, {
        ...data,
        folderId: folderId ? parseInt(folderId) : null,
      });
      dispatch({ type: "ADD_ROUTE", payload: newRoute });
      toast.success("Route created");
      navigate(`../${newRoute.id}`);
    } catch {
      toast.error("Failed to create route");
    }
  };

  return <RouteCreateUpdate onSubmit={onSubmit} />;
}
