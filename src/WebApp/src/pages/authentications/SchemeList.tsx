import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import IScheme from "./IScheme";
import { useAuth0 } from "@auth0/auth0-react";
import { Link } from "react-router-dom";

export default function SchemeList() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const [schemes, setSchemes] = useState<IScheme[]>([]);
  const { getAccessTokenSilently } = useAuth0();
  const getSchemes = async () => {
    const accessToken = await getAccessTokenSilently();
    const schemes = (await (
      await fetch(`/api/apps/${app.id}/authentications/schemes`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
    ).json()) as IScheme[];
    setSchemes(schemes);
  };
  useEffect(() => {
    getSchemes();
  }, []);

  const handleDeleteClick = async (id: number) => {
    if (confirm("Are you sure you want to delete this scheme?")) {
      try {
        await fetch(`/api/apps/${app.id}/authentications/schemes/${id}`, {
          method: "DELETE",
          headers: {
            Authorization: `Bearer ${await getAccessTokenSilently()}`,
          },
        });
      } finally {
        getSchemes();
      }
    }
  };
  return (
    <div className="p-2">
      <h1 className="font-semibold">Authentication Schemes</h1>
      <div className="flex">
        <Link to="new" className="ms-auto bg-primary px-2 py-1 text-white">
          New
        </Link>
      </div>
      <table className="mt-2 w-full">
        <thead>
          <tr>
            <th>Name</th>
            <th>Type</th>
            <th>Created At</th>
            <th>Updated At</th>
            <th>Enabled</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
          {schemes.map((s) => (
            <tr key={s.id} className="border">
              <td>{s.name}</td>
              <td>{s.type}</td>
              <td>{new Date(s.createdAt).toLocaleString()}</td>
              <td>
                {s.updatedAt ? new Date(s.updatedAt).toLocaleString() : "-"}
              </td>
              <td>{s.enabled ? "Yes" : "No"}</td>
              <td className="flex space-x-1">
                <Link to={`${s.id}`} className="text-primary">
                  Edit
                </Link>
                <button
                  type="button"
                  onClick={() => handleDeleteClick(s.id)}
                  className={`${!s.enabled ? "text-red-500" : "text-gray-500"}`}
                  disabled={s.enabled}
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
