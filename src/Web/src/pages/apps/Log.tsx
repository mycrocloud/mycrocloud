import { useContext, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { SubmitHandler, useForm } from "react-hook-form";
import { AppContext } from ".";
import { IRouteLog } from "../routes";
import moment from "moment";
import { XMarkIcon } from "@heroicons/react/20/solid";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

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
      <form onSubmit={handleSubmit(onSubmit)} className="rounded-lg border p-4 space-y-4">
        <div className="flex items-center gap-2">
          <Label className="min-w-24">Access Date</Label>
          <Input type="date" {...register("accessDateFrom")} className="w-auto" />
          <span>~</span>
          <Input type="date" {...register("accessDateTo")} className="w-auto" />
        </div>
        <div className="flex items-center gap-2">
          <Label className="min-w-24">Route</Label>
          <Input
            type="text"
            value={routeIdsValue}
            onChange={(e) => setRouteIdsValue(e.target.value)}
            className="w-auto"
          />
        </div>
        <div className="flex justify-end">
          <Button type="submit" size="sm">
            Filter
          </Button>
        </div>
      </form>
      <div className="mt-4 flex justify-end gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleSubmit(onDownloadAsCsv)}
        >
          Download as CSV
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleSubmit(onDownloadAsJson)}
        >
          Download as JSON
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleDownloadDisplayingAsCsvClick}
        >
          Download displaying logs as CSV
        </Button>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={handleDownloadDisplayingAsJsonClick}
        >
          Download displaying logs as JSON
        </Button>
      </div>
      <Table className="mt-4">
        <TableHeader>
          <TableRow>
            <TableHead className="min-w-32">Time</TableHead>
            <TableHead>Method</TableHead>
            <TableHead>Path</TableHead>
            <TableHead>Status Code</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {logs.map((log) => (
            <TableRow
              onClick={() => {
                console.log("click");
                setLog(log);
              }}
              key={log.id}
              className="cursor-pointer"
            >
              <TableCell>
                {new Date(log.timestamp).toLocaleString()}
              </TableCell>
              <TableCell>
                {log.method}
              </TableCell>
              <TableCell>
                {log.path}
              </TableCell>
              <TableCell>
                {log.statusCode}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {!log ? (
        <div className="mt-4 text-muted-foreground">Click a log to view details</div>
      ) : (
        <div className="mt-4 rounded-lg border p-4">
          <div className="flex items-center">
            <span className="rounded border px-2 py-0.5 text-sm font-medium">{log.method}</span>
            <span className="ps-2">{log.path}</span>
            <Button variant="ghost" size="icon" className="ms-auto" onClick={() => setLog(null)}>
              <XMarkIcon className="h-5 w-5" />
            </Button>
          </div>
          <div className="mt-4">
            <p className="font-semibold text-foreground">Logs</p>
            {log.functionLogs?.map((fl, i) => (
              <p key={i} className="text-sm text-muted-foreground">[{fl.type}] {fl.message}</p>
            ))}
          </div>
        </div>
      )}
    </>
  );
}
