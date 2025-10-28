import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import { Link, useParams } from "react-router-dom";
import { IRouteLog } from ".";
import { functionExecutionEnvironmentMap } from "./constants";

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

  const [log, setLog] = useState<string>();
  const handleViewClick = async (e: React.MouseEvent<HTMLSpanElement>, log?: string) => {
    e.stopPropagation();
    if (log) {
      setLog(log);
    }
  }
  return (
    <div className="p-2 relative">
      {log && (
        // todo: design
        <div className="absolute top-0 left-0 w-full h-full bg-gray-800 bg-opacity-50 flex items-center justify-center">
          <div className="bg-white p-4 rounded shadow-lg">
            <h2 className="text-lg font-bold">Log Details</h2>
            <p>{log}</p>
            <button
              type="button"
              className="mt-4 text-red-500"
              onClick={() => setLog(undefined)}
            >
              Close
            </button>
          </div>
        </div>
      )}
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
            <th className="text-start">Function Execution Environment</th>
            <th className="text-start">Function Execution Duration</th>
            <th className="text-start">Logs</th>
          </tr>
        </thead>
        <tbody>
          {logs.map((l) => (
            <tr key={l.id} className="border">
              <td>{new Date(l.timestamp).toLocaleString()}</td>
              <td>{l.remoteAddress || "-"}</td>
              <td>{l.path}</td>
              <td>{l.statusCode}</td>
              <td>{l.functionExecutionEnvironment ? functionExecutionEnvironmentMap.get(l.functionExecutionEnvironment) : "-"}</td>
              <td>{l.functionExecutionDuration}</td>
              <td>
                {l.additionalLogMessage ? (
                  <span title={l.additionalLogMessage}>
                    {l.additionalLogMessage.length > 20
                      ? l.additionalLogMessage.slice(0, 20) + "..."
                      : l.additionalLogMessage}
                  </span>
                ) : (
                  "-"
                )}
                <span onClick={(e) => handleViewClick(e, l.additionalLogMessage)} className="text-blue-500 underline cursor-pointer">
                  {l.additionalLogMessage ? "View" : ""}
                </span>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
