import { useContext, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { Controller, SubmitHandler, useForm } from "react-hook-form";
import { AppContext } from ".";
import { IRouteLog } from "../routes";
import moment from "moment";
import { Badge, Button, Card, Datepicker, Drawer, DrawerHeader, DrawerItems, Dropdown, DropdownItem, HelperText, Label, Pagination, PaginationNavigation, Table, TableBody, TableCell, TableHead, TableHeadCell, TableRow, TextInput } from "flowbite-react";
import { tryParseDate } from "@/utils";
import { useApiClient } from "@/hooks";
import { PaginatedResponse } from "@/models/Pagination";

type Inputs = {
  accessDateFrom?: string;
  accessDateTo?: string;
  routeIds: number[];
};

export default function AppLogs() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, getPagination } = useApiClient();
  const { handleSubmit, setValue, control, formState } = useForm<Inputs>();

  function buildQuery(data: Inputs) {
    let query = "pageSize=10";
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

  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [result, setResult] = useState<PaginatedResponse<IRouteLog> | null>(null);

  const onPageChange = async (page: number) => {
    setCurrentPage(page);
  }

  const searchLogs = async (data: Inputs) => {
    return await get<IRouteLog[]>(`/api/apps/${app.id}/logs?${buildQuery(data)}`)
  };

  const onSubmit: SubmitHandler<Inputs> = async (data: Inputs) => {
    const response = await getPagination<IRouteLog>(`/api/apps/${app.id}/logs?${buildQuery(data)}`, { page: currentPage, per_page: pageSize })
    setResult(response);
  };

  const [routeIdsValue, setRouteIdsValue] = useState("");
  useEffect(() => {
    if (routeIdsValue) {
      try {
        const routeIds = routeIdsValue.split(",").map((id) => parseInt(id)).filter((id) => !isNaN(id));
        setValue("routeIds", routeIds);
      } catch {
        formState.errors.routeIds = { type: "manual", message: "Invalid route ids" };
        setValue("routeIds", []);
      }
    } else {
      setValue("routeIds", []);
    }
  }, [routeIdsValue]);

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
  const [drawerOpen, setDrawlerOpen] = useState(false);
  const closeDetails = () => {
    setDrawlerOpen(false)
  }
  return (
    <div>
      <Card>
        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-2">
          <div>
            <Label>Access Date</Label>
            <div className="flex items-center gap-1">
              <Controller
                control={control}
                name="accessDateFrom"
                render={({ field: { value, onChange } }) => {
                  return <Datepicker value={tryParseDate(value)} onChange={(val) => onChange(val?.toString())} />
                }}
              />
              <span>~</span>
              <Controller
                control={control}
                name="accessDateTo"
                render={({ field: { value, onChange } }) => {
                  return <Datepicker value={tryParseDate(value)} onChange={(val) => onChange(val?.toString())} />
                }}
              />
            </div>
          </div>
          <div>
            <Label>Route Ids</Label>
            <TextInput
              value={routeIdsValue}
              onChange={(e) => setRouteIdsValue(e.target.value)}
            />
            <HelperText>Comma seperated route ids.</HelperText>
            {formState.errors.routeIds && (
              <HelperText color="failure">
                {formState.errors.routeIds.message}
              </HelperText>
            )}
          </div>
          <div className="flex justify-end">
            <Button type="submit">
              Filter
            </Button>
          </div>
        </form>
      </Card>

      {result && <div>
        <div className="flex justify-end">
          <Dropdown label="Download" dismissOnClick={false}>
            <DropdownItem onClick={handleSubmit(onDownloadAsCsv)}>CSV</DropdownItem>
            <DropdownItem onClick={handleSubmit(onDownloadAsJson)}>JSON</DropdownItem>
          </Dropdown>
        </div>
        <div className="flex overflow-x-auto sm:justify-center mb-2">
          <Pagination currentPage={currentPage} totalPages={Math.ceil(result.meta.total_count / result.meta.per_page)} onPageChange={onPageChange} showIcons />
        </div>
        <Table hoverable>
          <TableHead>
            <TableRow>
              <TableHeadCell>Time</TableHeadCell>
              <TableHeadCell>Method</TableHeadCell>
              <TableHeadCell>Path</TableHeadCell>
              <TableHeadCell>Status Code</TableHeadCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {result.data.map((log) => (
              <TableRow
                onClick={() => {
                  setDrawlerOpen(true);
                  setLog(log);
                }}
                key={log.id}
                role="button"
              >
                <TableCell>{new Date(log.timestamp).toLocaleString()}</TableCell>
                <TableCell>{log.method}</TableCell>
                <TableCell>{log.path}</TableCell>
                <TableCell>{log.statusCode}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>}
      <Drawer open={drawerOpen} onClose={closeDetails} position="right">
        <DrawerHeader title="Details" />
        {log && <DrawerItems>
          <div className="flex items-center">
            <Badge>{log.method}</Badge>
            <p className="ps-2">{log.path}</p>
          </div>
          <div className="mt-3">
            <div className="mb-2 flex items-center justify-between">
              <p className="text-sm font-semibold text-slate-800 dark:text-slate-200">
                Logs
              </p>
              <span className="text-xs text-slate-500">
                {log.functionLogs?.length || 0} entries
              </span>
            </div>

            <div className="max-h-64 space-y-1 overflow-y-auto rounded-lg border border-slate-200 bg-slate-50 p-2 font-mono text-xs dark:border-slate-700 dark:bg-slate-900">
              {log.functionLogs?.length ? (
                log.functionLogs.map((fl, idx) => (
                  <div
                    key={idx}
                    className="flex items-start gap-2 rounded px-2 py-1 hover:bg-slate-100 dark:hover:bg-slate-800"
                  >
                    {/* Type badge */}
                    <span
                      className={[
                        "mt-0.5 inline-block min-w-13 rounded px-1.5 py-0.5 text-center text-[10px] font-semibold uppercase",
                        fl.type === "error" &&
                        "bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300",
                        fl.type === "warn" &&
                        "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300",
                        fl.type === "info" &&
                        "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300",
                        fl.type === "debug" &&
                        "bg-slate-200 text-slate-700 dark:bg-slate-700 dark:text-slate-200",
                      ].join(" ")}
                    >
                      {fl.type}
                    </span>

                    {/* Message */}
                    <p className="whitespace-pre-wrap wrap-break-word text-slate-800 dark:text-slate-100">
                      {fl.message}
                    </p>
                  </div>
                ))
              ) : (
                <div className="py-6 text-center text-slate-400">
                  No logs available
                </div>
              )}
            </div>
          </div>
        </DrawerItems>}
      </Drawer>
    </div>
  );
}
