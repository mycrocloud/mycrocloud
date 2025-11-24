import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import IApiKey from "./IApiKey";
import { useApp } from "../../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { Spinner } from "flowbite-react";

export default function List() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const [keys, setKeys] = useState<IApiKey[]>([]);
  const [showingKeys, setShowingKeys] = useState<number[]>([]);

  useEffect(() => {
    const fetchKeys = async () => {
      const accessToken = await getAccessTokenSilently();
      const response = await fetch(`/api/apps/${app.id}/apikeys`, {
        headers: {
          authorization: `Bearer ${accessToken}`,
        },
      });
      const keys = await response.json();
      setKeys(keys);
    };
    fetchKeys();
  }, []);

  const handleDeleteClick = async (keyId: number) => {
    if (!window.confirm("Are you sure you want to delete this key?")) {
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const response = await fetch(`/api/apps/${app.id}/apikeys/${keyId}`, {
      method: "DELETE",
      headers: { authorization: `Bearer ${accessToken}` },
    });

    if (response.ok) {
      setKeys(keys.filter((key) => key.id !== keyId));
    } else {
      toast("Failed to delete key");
    }
  };
  return (
    <div className="p-3">
      <h1 className="font-bold">API keys</h1>
      <button
        onClick={() => {
          navigate("new");
        }}
        className="mt-2 bg-primary px-2 py-1 text-white"
      >
        Create new key
      </button>
      <div className="mt-3">
        <table className="w-full">
          <thead>
            <tr>
              <th className="w-48 border ps-2 text-start">Name</th>
              <th className="border ps-2 text-start">Key</th>
              <th className="w-36 border ps-2 text-start">Created At</th>
              <th className="w-36 border ps-2 text-start">Updated At</th>
              <th className="w-32 border ps-2 text-start">Actions</th>
            </tr>
          </thead>
          <tbody>
            {keys.map((key) => (
              <tr key={key.id}>
                <td className="px-2 text-start">{key.name}</td>
                <td className="flex px-2">
                  <span>
                    {showingKeys.includes(key.id)
                      ? key.key
                      : "****************"}
                  </span>
                  <div className="ms-auto">
                    <button
                      onClick={() => {
                        if (showingKeys.includes(key.id)) {
                          setShowingKeys(
                            showingKeys.filter((k) => k !== key.id),
                          );
                        } else {
                          setShowingKeys([...showingKeys, key.id]);
                        }
                      }}
                      className="me-1 text-primary hover:underline"
                    >
                      {showingKeys.includes(key.id) ? "Hide" : "Show"}
                    </button>
                    <button
                      onClick={() => {
                        navigator.clipboard.writeText(key.key);
                      }}
                      className="text-primary hover:underline"
                    >
                      Copy
                    </button>
                  </div>
                </td>
                <td className="px-2">
                  {new Date(key.createdAt).toDateString()}
                </td>
                <td className="px-2">
                  {key.updatedAt ? new Date(key.updatedAt).toDateString() : "-"}
                </td>
                <td className="flex space-x-2 px-2">
                  <Link
                    to={`${key.id}/edit`}
                    className="text-primary hover:underline"
                  >
                    Edit
                  </Link>
                  <button
                    onClick={() => handleDeleteClick(key.id)}
                    className="text-red-500 hover:underline"
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
