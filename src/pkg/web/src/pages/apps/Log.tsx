import { useContext, useState, useMemo } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { SubmitHandler, useForm } from "react-hook-form";
import { AppContext } from ".";
import { IRouteLog } from "../routes";
import { functionExecutionEnvironmentMap } from "../routes/constants";
import {
  Search,
  Download,
  FileJson,
  FileSpreadsheet,
  Calendar,
  Clock,
  Filter,
  ChevronRight,
  ChevronLeft,
  ChevronsLeft,
  ChevronsRight,
  RefreshCw,
  AlertCircle,
  SearchX,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";

type Inputs = {
  accessDateFrom?: string;
  accessDateTo?: string;
};

interface Pagination {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

interface PaginatedLogsResponse {
  data: IRouteLog[];
  pagination: Pagination;
}

const methodColors: Record<string, string> = {
  GET: "bg-sky-100 text-sky-700 dark:bg-sky-900/30 dark:text-sky-400",
  POST: "bg-orange-100 text-orange-700 dark:bg-orange-900/30 dark:text-orange-400",
  PUT: "bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400",
  DELETE: "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
  PATCH: "bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-400",
};

const PAGE_SIZE_OPTIONS = [10, 20, 50, 100];

function getStatusColor(statusCode: number): string {
  if (statusCode >= 200 && statusCode < 300) {
    return "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400";
  }
  if (statusCode >= 300 && statusCode < 400) {
    return "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400";
  }
  if (statusCode >= 400 && statusCode < 500) {
    return "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400";
  }
  if (statusCode >= 500) {
    return "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400";
  }
  return "bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400";
}

function formatTimestamp(timestamp: string): string {
  const date = new Date(timestamp);
  const now = new Date();
  const diff = now.getTime() - date.getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);

  if (minutes < 1) return "Just now";
  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  if (days < 7) return `${days}d ago`;

  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString();
}

function downloadFile(content: string, filename: string, type: string) {
  const blob = new Blob([content], { type });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

function getTimestamp(): string {
  const now = new Date();
  return `${now.getFullYear()}${String(now.getMonth() + 1).padStart(2, "0")}${String(now.getDate()).padStart(2, "0")}${String(now.getHours()).padStart(2, "0")}${String(now.getMinutes()).padStart(2, "0")}${String(now.getSeconds()).padStart(2, "0")}`;
}

export default function AppLogs() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { getAccessTokenSilently } = useAuth0();
  const [logs, setLogs] = useState<IRouteLog[]>([]);
  const [pagination, setPagination] = useState<Pagination | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [hasFetched, setHasFetched] = useState(false);
  const [selectedLog, setSelectedLog] = useState<IRouteLog | null>(null);
  const [searchQuery, setSearchQuery] = useState("");
  const [pageSize, setPageSize] = useState(20);
  const { register, handleSubmit, getValues } = useForm<Inputs>();

  const fetchLogs = async (data: Inputs, page: number, size: number) => {
    setLoading(true);
    setError(null);
    try {
      const accessToken = await getAccessTokenSilently();
      const params = new URLSearchParams();
      params.set("page", page.toString());
      params.set("pageSize", size.toString());
      if (data.accessDateFrom) params.set("accessDateFrom", data.accessDateFrom);
      if (data.accessDateTo) params.set("accessDateTo", data.accessDateTo);

      const res = await fetch(`/api/apps/${app.id}/logs?${params.toString()}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      if (!res.ok) {
        setError("Failed to load logs");
        return;
      }
      const response = (await res.json()) as PaginatedLogsResponse;
      setLogs(response.data);
      setPagination(response.pagination);
      setHasFetched(true);
    } catch {
      setError("Failed to load logs");
    } finally {
      setLoading(false);
    }
  };

  const onSubmit: SubmitHandler<Inputs> = async (data: Inputs) => {
    await fetchLogs(data, 1, pageSize);
  };

  const handlePageChange = (newPage: number) => {
    const data = getValues();
    fetchLogs(data, newPage, pageSize);
  };

  const handlePageSizeChange = (newSize: string) => {
    const size = parseInt(newSize);
    setPageSize(size);
    const data = getValues();
    fetchLogs(data, 1, size);
  };

  const filteredLogs = useMemo(() => {
    if (!searchQuery) return logs;
    const query = searchQuery.toLowerCase();
    return logs.filter(
      (log) =>
        log.path.toLowerCase().includes(query) ||
        log.method.toLowerCase().includes(query) ||
        String(log.statusCode).includes(query)
    );
  }, [logs, searchQuery]);

  const handleDownloadCsv = (data: IRouteLog[]) => {
    const header = "Time,Remote Address,Method,Path,Status Code";
    const csv = [
      header,
      ...data.map(
        (l) =>
          `"${formatDate(l.timestamp)}","${l.remoteAddress || "-"}","${l.method}","${l.path}","${l.statusCode}"`
      ),
    ].join("\n");
    downloadFile(csv, `logs_${getTimestamp()}.csv`, "text/csv");
  };

  const handleDownloadJson = (data: IRouteLog[]) => {
    const json = JSON.stringify(data, null, 2);
    downloadFile(json, `logs_${getTimestamp()}.json`, "application/json");
  };

  return (
    <div className="space-y-4 p-4">
      {/* Filter Card */}
      <Card>
        <CardHeader className="pb-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Filter className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-base">Filters</CardTitle>
            </div>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="outline" size="sm" disabled={logs.length === 0}>
                  <Download className="mr-2 h-4 w-4" />
                  Export
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuLabel>Export current page</DropdownMenuLabel>
                <DropdownMenuItem onClick={() => handleDownloadCsv(filteredLogs)}>
                  <FileSpreadsheet className="mr-2 h-4 w-4" />
                  CSV ({filteredLogs.length})
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => handleDownloadJson(filteredLogs)}>
                  <FileJson className="mr-2 h-4 w-4" />
                  JSON ({filteredLogs.length})
                </DropdownMenuItem>
                {searchQuery && filteredLogs.length !== logs.length && (
                  <>
                    <DropdownMenuSeparator />
                    <DropdownMenuLabel>Export page (unfiltered)</DropdownMenuLabel>
                    <DropdownMenuItem onClick={() => handleDownloadCsv(logs)}>
                      <FileSpreadsheet className="mr-2 h-4 w-4" />
                      CSV ({logs.length})
                    </DropdownMenuItem>
                    <DropdownMenuItem onClick={() => handleDownloadJson(logs)}>
                      <FileJson className="mr-2 h-4 w-4" />
                      JSON ({logs.length})
                    </DropdownMenuItem>
                  </>
                )}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
              <div className="space-y-2">
                <label className="text-sm font-medium text-muted-foreground">
                  From Date
                </label>
                <div className="relative">
                  <Calendar className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    type="date"
                    {...register("accessDateFrom")}
                    className="pl-9"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label className="text-sm font-medium text-muted-foreground">
                  To Date
                </label>
                <div className="relative">
                  <Calendar className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    type="date"
                    {...register("accessDateTo")}
                    className="pl-9"
                  />
                </div>
              </div>
              <div className="flex items-end gap-2">
                <Button type="submit" className="flex-1" disabled={loading}>
                  {loading ? "Loading..." : "Apply Filters"}
                </Button>
                {hasFetched && (
                  <Button
                    type="submit"
                    variant="outline"
                    size="icon"
                    disabled={loading}
                    title="Refresh"
                  >
                    <RefreshCw className={cn("h-4 w-4", loading && "animate-spin")} />
                  </Button>
                )}
              </div>
            </div>
          </form>
        </CardContent>
      </Card>

      {/* Stats and Search */}
      {pagination && pagination.totalItems > 0 && (
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="text-sm text-muted-foreground">
            Showing {((pagination.page - 1) * pagination.pageSize) + 1} - {Math.min(pagination.page * pagination.pageSize, pagination.totalItems)} of {pagination.totalItems.toLocaleString()} logs
          </div>
          <div className="relative w-full sm:w-64">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              type="text"
              placeholder="Filter current page..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-9"
            />
          </div>
        </div>
      )}

      {/* Logs Table */}
      <Card>
        <CardContent className="p-0">
          {loading ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <RefreshCw className="h-8 w-8 animate-spin text-muted-foreground/50" />
              <p className="mt-4 text-sm text-muted-foreground">Loading logs...</p>
            </div>
          ) : error ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <AlertCircle className="h-12 w-12 text-destructive/50" />
              <h3 className="mt-4 text-lg font-medium text-destructive">{error}</h3>
              <Button variant="outline" size="sm" className="mt-4" onClick={handleSubmit(onSubmit)}>
                Try again
              </Button>
            </div>
          ) : !hasFetched ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Clock className="h-12 w-12 text-muted-foreground/50" />
              <h3 className="mt-4 text-lg font-medium">No logs loaded</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Select a date range and click "Apply Filters" to view logs
              </p>
            </div>
          ) : pagination && pagination.totalItems === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <Clock className="h-12 w-12 text-muted-foreground/50" />
              <h3 className="mt-4 text-lg font-medium">No logs found</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                No requests were made in the selected date range
              </p>
            </div>
          ) : filteredLogs.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <SearchX className="h-12 w-12 text-muted-foreground/50" />
              <h3 className="mt-4 text-lg font-medium">No matching logs</h3>
              <p className="mt-1 text-sm text-muted-foreground">
                Try adjusting your search query
              </p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="w-[140px]">Time</TableHead>
                  <TableHead className="w-[80px]">Method</TableHead>
                  <TableHead>Path</TableHead>
                  <TableHead className="w-[100px] text-right">Status</TableHead>
                  <TableHead className="w-[40px]" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredLogs.map((log) => (
                  <TableRow
                    key={log.id}
                    onClick={() => setSelectedLog(log)}
                    className="cursor-pointer"
                  >
                    <TableCell className="text-muted-foreground">
                      <span title={formatDate(log.timestamp)}>
                        {formatTimestamp(log.timestamp)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="secondary"
                        className={cn(
                          "font-mono text-xs",
                          methodColors[log.method] || ""
                        )}
                      >
                        {log.method}
                      </Badge>
                    </TableCell>
                    <TableCell className="font-mono text-sm">
                      {log.path}
                    </TableCell>
                    <TableCell className="text-right">
                      <Badge
                        variant="secondary"
                        className={cn("font-mono", getStatusColor(log.statusCode))}
                      >
                        {log.statusCode}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Pagination Controls */}
      {pagination && pagination.totalPages > 1 && (
        <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">Rows per page</span>
            <Select value={pageSize.toString()} onValueChange={handlePageSizeChange}>
              <SelectTrigger className="w-[70px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {PAGE_SIZE_OPTIONS.map((size) => (
                  <SelectItem key={size} value={size.toString()}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="flex items-center gap-1">
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(1)}
              disabled={loading || pagination.page === 1}
              title="First page"
            >
              <ChevronsLeft className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(pagination.page - 1)}
              disabled={loading || pagination.page === 1}
              title="Previous page"
            >
              <ChevronLeft className="h-4 w-4" />
            </Button>
            <span className="px-3 text-sm">
              Page {pagination.page} of {pagination.totalPages}
            </span>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(pagination.page + 1)}
              disabled={loading || pagination.page === pagination.totalPages}
              title="Next page"
            >
              <ChevronRight className="h-4 w-4" />
            </Button>
            <Button
              variant="outline"
              size="icon"
              onClick={() => handlePageChange(pagination.totalPages)}
              disabled={loading || pagination.page === pagination.totalPages}
              title="Last page"
            >
              <ChevronsRight className="h-4 w-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Log Detail Sheet */}
      <Sheet open={!!selectedLog} onOpenChange={() => setSelectedLog(null)}>
        <SheetContent className="w-full overflow-y-auto sm:max-w-lg">
          {selectedLog && (
            <>
              <SheetHeader>
                <div className="flex items-center gap-3">
                  <Badge
                    variant="secondary"
                    className={cn(
                      "font-mono",
                      methodColors[selectedLog.method] || ""
                    )}
                  >
                    {selectedLog.method}
                  </Badge>
                  <Badge
                    variant="secondary"
                    className={cn("font-mono", getStatusColor(selectedLog.statusCode))}
                  >
                    {selectedLog.statusCode}
                  </Badge>
                </div>
                <SheetTitle className="font-mono text-sm break-all text-left">
                  {selectedLog.path}
                </SheetTitle>
                <SheetDescription>
                  {formatDate(selectedLog.timestamp)}
                </SheetDescription>
              </SheetHeader>

              {/* Navigation between logs */}
              <div className="flex items-center justify-between border-b py-3">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    const currentIndex = filteredLogs.findIndex(l => l.id === selectedLog.id);
                    if (currentIndex > 0) {
                      setSelectedLog(filteredLogs[currentIndex - 1]);
                    }
                  }}
                  disabled={filteredLogs.findIndex(l => l.id === selectedLog.id) === 0}
                >
                  <ChevronLeft className="mr-1 h-4 w-4" />
                  Previous
                </Button>
                <span className="text-sm text-muted-foreground">
                  {filteredLogs.findIndex(l => l.id === selectedLog.id) + 1} / {filteredLogs.length}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    const currentIndex = filteredLogs.findIndex(l => l.id === selectedLog.id);
                    if (currentIndex < filteredLogs.length - 1) {
                      setSelectedLog(filteredLogs[currentIndex + 1]);
                    }
                  }}
                  disabled={filteredLogs.findIndex(l => l.id === selectedLog.id) === filteredLogs.length - 1}
                >
                  Next
                  <ChevronRight className="ml-1 h-4 w-4" />
                </Button>
              </div>

              <div className="space-y-4 py-4">
                {/* Request Info */}
                <div className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <span className="text-muted-foreground">Remote Address</span>
                    <p className="mt-1 font-mono">
                      {selectedLog.remoteAddress || "-"}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Content Length</span>
                    <p className="mt-1 font-mono">
                      {selectedLog.requestContentLength
                        ? `${selectedLog.requestContentLength} bytes`
                        : "-"}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Content Type</span>
                    <p className="mt-1 font-mono text-xs">
                      {selectedLog.requestContentType || "-"}
                    </p>
                  </div>
                  <div>
                    <span className="text-muted-foreground">Execution Duration</span>
                    <p className="mt-1 font-mono">
                      {selectedLog.functionExecutionDuration
                        ? `${selectedLog.functionExecutionDuration}ms`
                        : "-"}
                    </p>
                  </div>
                </div>

                {selectedLog.functionExecutionEnvironment && (
                  <div className="text-sm">
                    <span className="text-muted-foreground">Execution Environment</span>
                    <p className="mt-1">
                      {functionExecutionEnvironmentMap.get(selectedLog.functionExecutionEnvironment) || "-"}
                    </p>
                  </div>
                )}

                {/* Function Logs */}
                {selectedLog.functionLogs && selectedLog.functionLogs.length > 0 && (
                  <div>
                    <h4 className="mb-2 font-medium">Function Logs</h4>
                    <div className="max-h-[400px] overflow-auto rounded-lg bg-muted/50 p-3">
                      {selectedLog.functionLogs.map((fl, i) => (
                        <div
                          key={i}
                          className="flex gap-2 py-1 font-mono text-sm"
                        >
                          <span
                            className={cn(
                              "shrink-0",
                              fl.type === "error"
                                ? "text-red-500"
                                : fl.type === "warn"
                                  ? "text-yellow-500"
                                  : "text-muted-foreground"
                            )}
                          >
                            [{fl.type}]
                          </span>
                          <span className="break-all">{fl.message}</span>
                        </div>
                      ))}
                    </div>
                  </div>
                )}

                {(!selectedLog.functionLogs ||
                  selectedLog.functionLogs.length === 0) && (
                  <div className="rounded-lg bg-muted/50 p-4 text-center text-sm text-muted-foreground">
                    No function logs available
                  </div>
                )}

                {/* Request Headers */}
                {selectedLog.requestHeaders && (
                  <div>
                    <h4 className="mb-2 font-medium">Request Headers</h4>
                    <pre className="max-h-[200px] overflow-auto rounded-lg bg-muted/50 p-3 text-xs">
                      {selectedLog.requestHeaders}
                    </pre>
                  </div>
                )}
              </div>
            </>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
