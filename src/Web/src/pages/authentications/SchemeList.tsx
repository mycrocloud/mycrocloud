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
        <thead className="text-left border">
          <tr>
            <th className="px-2 py-1">Name</th>
            <th className="px-2 py-1">Type</th>
            <th className="px-2 py-1">Created At</th>
            <th className="px-2 py-1">Updated At</th>
            <th className="px-2 py-1">Enabled</th>
            <th className="px-2 py-1">Actions</th>
          </tr>
        </thead>
        <tbody className="text-left px-2">
          {schemes.map((s) => (
            <tr key={s.id} className="border px-2">
              <td className="px-2 py-1">{s.name}</td>
              <td className="px-2 py-1">{s.type}</td>
              <td className="px-2 py-1">
                {new Date(s.createdAt).toLocaleString()}
              </td>
              <td className="px-2 py-1">
                {s.updatedAt ? new Date(s.updatedAt).toLocaleString() : "-"}
              </td>
              <td className="px-2 py-1">{s.enabled ? "Yes" : "No"}</td>
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
