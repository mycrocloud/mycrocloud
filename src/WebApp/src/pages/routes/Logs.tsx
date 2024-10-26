import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { Link, useParams } from "react-router-dom";
import { IRouteLog } from ".";

export default function RouteLogs() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [logs, setLogs] = useState<IRouteLog[]>([]);
  const routeId = parseInt(useParams()["routeId"]!);
  useEffect(() => {
    getLogs();
  }, []);

  const getLogs = async () => {
    const accessToken = await getAccessTokenSilently();
    const logs = (await (
      await fetch(`/api/apps/${app.id}/logs?routeIds=${routeId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
    ).json()) as IRouteLog[];
    setLogs(logs);
  };
  const handleRefreshClick = async () => {
    getLogs();
  };
  return (
    <div className="p-2">
      <div className="flex">
        <Link to={`../${routeId}`} className="text-gray-600">
          Back
        </Link>
        <button
          type="button"
          className="ms-auto text-primary"
          onClick={handleRefreshClick}
        >
          Refresh
        </button>
      </div>
      <table className="w-full">
        <thead>
          <tr>
            <th className="text-start">Timestamp</th>
            <th className="text-start">Remote Address</th>
            <th className="text-start">Path</th>
            <th className="text-start">Status Code</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((l) => (
            <tr key={l.id} className="border">
              <td>{new Date(l.timestamp).toLocaleString()}</td>
              <td>{l.remoteAddress || "-"}</td>
              <td>{l.path}</td>
              <td>{l.statusCode}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
