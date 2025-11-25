import { useContext, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { SubmitHandler, useForm } from "react-hook-form";
import { AppContext } from ".";
import { IRouteLog } from "../routes";
import { Link } from "react-router-dom";
import moment from "moment";
import { functionExecutionEnvironmentMap } from "../routes/constants";
import { LockClosedIcon } from "@heroicons/react/24/solid";
import { XMarkIcon } from "@heroicons/react/20/solid";

type Inputs = {
  accessDateFrom?: string;
  accessDateTo?: string;
  routeIds: number[];
};

export default function AppLogs() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { getAccessTokenSilently } = useAuth0();
  const [logs, setLogs] = useState<IRouteLog[]>([]);
  const { register, handleSubmit, setValue } = useForm<Inputs>();

  function buildQuery(data: Inputs) {
    let query = "";
    const conditions = [];
    if (data.accessDateFrom) {
      conditions.push(`accessDateFrom=${data.accessDateFrom}`);
    }
    if (data.accessDateTo) {
      conditions.push(`accessDateTo=${data.accessDateTo}`);
    }
    if (data.routeIds.length > 0) {
      data.routeIds.forEach((id) => {
        conditions.push(`routeIds=${id}`);
      });
    }
    if (conditions.length > 0) {
      query += `&${conditions.join("&")}`;
    }
    return query;
  }

  const searchLogs = async (data: Inputs) => {
    const accessToken = await getAccessTokenSilently();
    const logs = (await (
      await fetch(`/api/apps/${app.id}/logs?${buildQuery(data)}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      })
    ).json()) as IRouteLog[];
    return logs;
  };
  const onSubmit: SubmitHandler<Inputs> = async (data: Inputs) => {
    const logs = await searchLogs(data);
    setLogs(logs);
  };
  const [routeIdsValue, setRouteIdsValue] = useState("");
  useEffect(() => {
    if (routeIdsValue) {
      setValue(
        "routeIds",
        routeIdsValue.split(",").map((id) => parseInt(id)),
      );
    } else {
      setValue("routeIds", []);
    }
  }, [routeIdsValue]);

  const handleDownloadDisplayingAsCsvClick = () => {
    const csv = logs
      .map((l) => {
        return `${new Date(l.timestamp).toLocaleString()},${l.remoteAddress || "-"},${l.routeId || "-"},${l.method},${l.path},${l.statusCode}`;
      })
      .join("\n");
    const blob = new Blob([csv], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `logs_${moment(new Date()).format("YYYYMMDDHHMMSS")}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const handleDownloadDisplayingAsJsonClick = () => {
    const json = JSON.stringify(logs, null, 2);
    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `logs_${moment(new Date()).format("YYYYMMDDHHMMSS")}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };
  const onDownloadAsJson = async (data: Inputs) => {
    const logs = await searchLogs(data);
    const json = JSON.stringify(logs, null, 2);
    const blob = new Blob([json], { type: "application/json" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `logs_${moment(new Date()).format("YYYYMMDDHHMMSS")}.json`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const onDownloadAsCsv = async (data: Inputs) => {
    const logs = await searchLogs(data);
    const csv = logs
      .map((l) => {
        return `${new Date(l.timestamp).toLocaleString()},${l.remoteAddress || "-"},${l.routeId || "-"},${l.method},${l.path},${l.statusCode}`;
      })
      .join("\n");
    const blob = new Blob([csv], { type: "text/csv" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `logs_${moment(new Date()).format("YYYYMMDDHHMMSS")}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const [log, setLog] = useState<IRouteLog | null>();
  console.log("rendering...", log);
  return (
    <>
      <form onSubmit={handleSubmit(onSubmit)} className="border p-2">
        <div>
          <label className="me-2">Access Date</label>
          <input type="date" {...register("accessDateFrom")} />
          <span>~</span>
          <input type="date" {...register("accessDateTo")} />
        </div>
        <div>
          <label className="me-2">Route</label>
          <input
            type="text"
            value={routeIdsValue}
            onChange={(e) => setRouteIdsValue(e.target.value)}
            className="border border-gray-200"
          />
        </div>
        <div className="flex justify-end space-x-1">
          <button type="submit" className="bg-primary px-2 py-0.5 text-white">
            Filter
          </button>
        </div>
      </form>
      <div className="mt-2 flex justify-end space-x-1 p-2">
        <button
          type="button"
          onClick={handleSubmit(onDownloadAsCsv)}
          className="bg-primary px-2 py-0.5 text-white"
        >
          Download as CSV
        </button>
        <button
          type="button"
          onClick={handleSubmit(onDownloadAsJson)}
          className="bg-primary px-2 py-0.5 text-white"
        >
          Download as JSON
        </button>
        <button
          type="button"
          onClick={handleDownloadDisplayingAsCsvClick}
          className="bg-primary px-2 py-0.5 text-white"
        >
          Download displaying logs as CSV
        </button>
        <button
          type="button"
          onClick={handleDownloadDisplayingAsJsonClick}
          className="bg-primary px-2 py-0.5 text-white"
        >
          Download displaying logs as JSON
        </button>
      </div>
      <div className="flex">
        <table className="flex-1">
          <thead className="border">
            <tr>
              <th className="min-w-16 text-start p-2">Time</th>
              <th className="text-start">Method</th>
              <th className="text-start">Path</th>
              <th className="text-start">Status Code</th>
              {/* <th className="text-start">Function Execution Environment</th> */}
            </tr>
          </thead>
          <tbody>
            {logs.map((log) => (
              <tr
                onClick={() => {
                  console.log("click");
                  setLog(log);
                }}
                key={log.id}
                className="cursor-pointer border hover:bg-gray-100"
              >
                <td className="py-1.5">
                  {new Date(log.timestamp).toLocaleString()}
                </td>
                <td className="py-1.5">
                  {log.method}
                </td>
                <td className="py-1.5">
                  {log.path}
                </td>
                <td className="py-1.5">
                  {log.statusCode}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="w-80 border p-2">
          {!log ? (
            <div className="text-gray-500">Click log to view details</div>
          ) : (
            <div className="">
              <div className="flex items-center">
                <p className="border p-0.5 rounded">{log.method}</p>
                <p className="ps-2">{log.path}</p>
                <button className="ms-auto" onClick={() => setLog(null)}>
                  <XMarkIcon width={20} />
                </button>
              </div>
              <div className="mt-2">
                <p className="font-semibold text-slate-800">Logs</p>
                <p>{log.additionalLogMessage}</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
