import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AppContext } from ".";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  Rocket,
  ChevronRight,
  ChevronLeft,
  Search,
  Filter,
  Archive,
  GitBranch,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IDeployment {
  id: string;
  status: string;
  buildId: string | null;
  buildName: string | null;
  createdAt: string;
  artifactSize: number;
}

type StatusFilter = "all" | "pending" | "extracting" | "ready" | "failed" | "archived";

const ITEMS_PER_PAGE = 10;

const STATUS_OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "All Status" },
  { value: "pending", label: "Pending" },
  { value: "extracting", label: "Extracting" },
  { value: "ready", label: "Ready" },
  { value: "failed", label: "Failed" },
  { value: "archived", label: "Archived" },
];

function getStatusConfig(status: string) {
  switch (status.toLowerCase()) {
    case "pending":
      return {
        label: "Pending",
        icon: Clock,
        className:
          "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400",
      };
    case "extracting":
      return {
        label: "Extracting",
        icon: Loader2,
        className:
          "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
        iconClassName: "animate-spin",
      };
    case "ready":
      return {
        label: "Ready",
        icon: CheckCircle2,
        className:
          "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400",
      };
    case "failed":
      return {
        label: "Failed",
        icon: XCircle,
        className:
          "bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400",
      };
    case "archived":
      return {
        label: "Archived",
        icon: Archive,
        className:
          "bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400",
      };
    default:
      return {
        label: status,
        icon: Clock,
        className:
          "bg-gray-100 text-gray-700 dark:bg-gray-900/30 dark:text-gray-400",
      };
  }
}

function formatTimestamp(timestamp: string): string {
  if (!timestamp) return "-";
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

function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

export default function Deployments() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get } = useApiClient();
  const navigate = useNavigate();

  const [deployments, setDeployments] = useState<IDeployment[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // Filter & Pagination state
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [currentPage, setCurrentPage] = useState(1);

  const fetchDeployments = useCallback(async () => {
    const data = await get<IDeployment[]>(`/api/apps/${app.id}/deployments`);
    setDeployments(data);
    setIsLoading(false);
  }, [app.id, get]);

  // Initial fetch
  useEffect(() => {
    fetchDeployments();
  }, [fetchDeployments]);

  // SSE subscription for real-time updates
  useEffect(() => {
    let isMounted = true;
    let intervalId: NodeJS.Timeout;

    // Poll for updates every 5 seconds
    intervalId = setInterval(() => {
      if (isMounted) {
        fetchDeployments();
      }
    }, 5000);

    return () => {
      isMounted = false;
      clearInterval(intervalId);
    };
  }, [fetchDeployments]);

  // Filter deployments
  const filteredDeployments = useMemo(() => {
    return deployments.filter((deployment) => {
      // Status filter
      if (statusFilter !== "all") {
        const normalizedStatus = deployment.status.toLowerCase();
        if (normalizedStatus !== statusFilter) return false;
      }

      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        const buildName = (deployment.buildName || "").toLowerCase();
        const id = deployment.id.toLowerCase();
        if (!buildName.includes(query) && !id.includes(query)) {
          return false;
        }
      }

      return true;
    });
  }, [deployments, statusFilter, searchQuery]);

  // Pagination
  const totalPages = Math.ceil(filteredDeployments.length / ITEMS_PER_PAGE);
  const paginatedDeployments = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredDeployments.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredDeployments, currentPage]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [statusFilter, searchQuery]);

  const hasFilters = statusFilter !== "all" || searchQuery !== "";

  return (
    <div className="flex h-full flex-col p-4">
      {/* Header */}
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Rocket className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Deployments</h2>
          {!isLoading && (
            <Badge variant="secondary" className="ml-2">
              {deployments.length}
            </Badge>
          )}
        </div>
        <Button
          variant="outline"
          size="sm"
          onClick={() => navigate("../builds")}
        >
          <GitBranch className="mr-2 h-4 w-4" />
          View Builds
        </Button>
      </div>

      {/* Filters */}
      <div className="mb-4 flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search deployments..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="h-9 pl-8"
          />
        </div>
        <Select
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as StatusFilter)}
        >
          <SelectTrigger className="h-9 w-[150px]">
            <Filter className="mr-2 h-3.5 w-3.5" />
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {hasFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearchQuery("");
              setStatusFilter("all");
            }}
          >
            Clear filters
          </Button>
        )}
      </div>

      {/* Deployments Table */}
      <div className="flex-1 overflow-hidden rounded-lg border">
        {isLoading ? (
          <div className="flex h-full items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : paginatedDeployments.length === 0 ? (
          <div className="flex h-full flex-col items-center justify-center py-12 text-center">
            <Rocket className="h-12 w-12 text-muted-foreground/50" />
            <p className="mt-4 text-sm text-muted-foreground">
              {hasFilters ? "No deployments match filters" : "No deployments yet"}
            </p>
            {!hasFilters && (
              <p className="mt-1 text-xs text-muted-foreground">
                Create a build to generate deployments
              </p>
            )}
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[250px]">Deployment</TableHead>
                <TableHead className="w-[200px]">Build</TableHead>
                <TableHead className="w-[120px]">Status</TableHead>
                <TableHead className="w-[100px]">Size</TableHead>
                <TableHead>Created</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedDeployments.map((deployment) => {
                const statusConfig = getStatusConfig(deployment.status);
                const StatusIcon = statusConfig.icon;

                return (
                  <TableRow
                    key={deployment.id}
                    className="cursor-pointer"
                    onClick={() => navigate(deployment.id)}
                  >
                    <TableCell>
                      <div>
                        <p className="font-medium font-mono text-sm">
                          {deployment.id.slice(0, 12)}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          ID: {deployment.id.slice(0, 8)}
                        </p>
                      </div>
                    </TableCell>
                    <TableCell>
                      {deployment.buildName ? (
                        <div>
                          <p className="font-medium text-sm">
                            {deployment.buildName}
                          </p>
                          {deployment.buildId && (
                            <p className="text-xs text-muted-foreground">
                              {deployment.buildId.slice(0, 8)}
                            </p>
                          )}
                        </div>
                      ) : (
                        <span className="text-sm text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="secondary"
                        className={cn("shrink-0", statusConfig.className)}
                      >
                        <StatusIcon
                          className={cn(
                            "mr-1 h-3 w-3",
                            statusConfig.iconClassName
                          )}
                        />
                        {statusConfig.label}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {formatBytes(deployment.artifactSize)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {formatTimestamp(deployment.createdAt)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="mt-4 flex items-center justify-between">
          <span className="text-sm text-muted-foreground">
            {filteredDeployments.length} deployment{filteredDeployments.length !== 1 && "s"}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === 1}
              onClick={() => setCurrentPage((p) => p - 1)}
            >
              <ChevronLeft className="mr-1 h-4 w-4" />
              Previous
            </Button>
            <span className="text-sm text-muted-foreground">
              Page {currentPage} of {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage === totalPages}
              onClick={() => setCurrentPage((p) => p + 1)}
            >
              Next
              <ChevronRight className="ml-1 h-4 w-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
