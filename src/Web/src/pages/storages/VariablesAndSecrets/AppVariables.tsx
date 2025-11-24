import { useEffect, useState } from "react";
import IVariable from "./Variable";
import { Link } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { useApp } from "../../apps";
import { toast } from "react-toastify";
import { Spinner } from "flowbite-react";

export default function AppVariables() {
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const [variables, setVariables] = useState<IVariable[]>([]);
  const { getAccessTokenSilently } = useAuth0();

  useEffect(() => {
    const getVariable = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/variables`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      const variables = (await res.json()) as IVariable[];
      setVariables(variables);
    };
    getVariable();
  }, []);

  const handleDeleteClick = async (variable: IVariable) => {
    if (
      !window.confirm(
        `Are you sure you want to delete the variable "${variable.name}"?`,
      )
    ) {
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const response = await fetch(
      `/api/apps/${app.id}/variables/${variable.id}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      },
    );

    if (response.ok) {
      toast.success(`Variable "${variable.name}" has been deleted.`);
      setVariables(variables.filter((v) => v.id !== variable.id));
    }
  };
  return (
    <div className="p-2">
      <h1 className="font-bold">Variables</h1>
      <div className="mt-2">
        <Link
          to={"new"}
          type="button"
          className="bg-primary px-3 py-1 text-white"
        >
          Add
        </Link>
        <table className="mt-2 w-full">
          <thead>
            <tr>
              <th className="text-start">Name</th>
              <th className="text-start">Value Type</th>
              <th className="text-start">Created at</th>
              <th className="text-start">Updated at</th>
              <th className="text-start">Actions</th>
            </tr>
          </thead>
          <tbody>
            {variables.map((v) => (
              <tr key={v.id}>
                <td>
                  <Link to={`${v.id}/edit`} className="text-blue-500">
                    {v.name}
                  </Link>
                </td>
                <td>{v.valueType}</td>
                <td>{new Date(v.createdAt).toLocaleString()}</td>
                <td>
                  {v.updatedAt ? new Date(v.updatedAt).toLocaleString() : "-"}
                </td>
                <td className="inline-flex">
                  <button
                    type="button"
                    onClick={() => handleDeleteClick(v)}
                    className="text-red-500"
                  >
                    Delete
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
