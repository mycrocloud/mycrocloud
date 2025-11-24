import { useEffect, useState } from "react";
import ITextStorage from "./ITextStorage";
import { useAuth0 } from "@auth0/auth0-react";
import { useApp } from "../../apps";
import { Link } from "react-router-dom";
import { toast } from "react-toastify";
import { Spinner } from "flowbite-react";

export default function List() {
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const { getAccessTokenSilently } = useAuth0();

  const [storages, setStorages] = useState<ITextStorage[]>([]);

  useEffect(() => {
    const getStorages = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/textstorages`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      const storages = (await res.json()) as ITextStorage[];
      setStorages(storages);
    };
    getStorages();
  }, []);

  const handleDeleteClick = async (storage: ITextStorage) => {
    if (
      !window.confirm(
        `Are you sure you want to delete the storage "${storage.name}"?`,
      )
    ) {
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const response = await fetch(
      `/api/apps/${app.id}/textstorages/${storage.id}`,
      {
        method: "DELETE",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      },
    );

    if (response.ok) {
      toast.success(`Storage "${storage.name}" has been deleted.`);
      setStorages(storages.filter((s) => s.id !== storage.id));
    }
  };

  return (
    <div className="p-2">
      <h1 className="font-bold">Text Storages</h1>
      <div className="mt-2">
        <Link to={"new"} className="bg-primary px-3 py-1 text-white">
          Create Storage
        </Link>
        <table className="mt-2 w-full p-2">
          <thead>
            <tr>
              <th className="text-start">Name</th>
              <th className="text-start">Size (bytes)</th>
              <th className="text-start">Created At</th>
              <th className="text-start">Updated At</th>
              <th className="text-start">Actions</th>
            </tr>
          </thead>
          <tbody>
            {storages.map((s) => (
              <tr key={s.id}>
                <td>
                  <Link to={`${s.id}`} className="text-blue-500">
                    {s.name}
                  </Link>
                </td>
                <td>{s.size}</td>
                <td>{new Date(s.createdAt).toLocaleString()}</td>
                <td>
                  {s.updatedAt ? new Date(s.updatedAt).toLocaleString() : "-"}
                </td>
                <td className="flex space-x-2 divide-x">
                  <Link to={`${s.id}/logon`} className="text-blue-500">
                    Logon
                  </Link>
                  <button
                    onClick={() => handleDeleteClick(s)}
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
