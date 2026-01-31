import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AppContext } from ".";
import { useAuth0 } from "@auth0/auth0-react";
import BuildLogs from "./BuildLogs";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Play,
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  Package,
  Terminal,
  ChevronRight,
  ChevronLeft,
  GitCommit,
  Timer,
  Search,
  Filter,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IBuild {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  finishedAt: string;
}

type BuildInputs = {
  name?: string;
};

type StatusFilter = "all" | "pending" | "running" | "success" | "failed";

const ITEMS_PER_PAGE = 10;

const STATUS_OPTIONS: { value: StatusFilter; label: string }[] = [
  { value: "all", label: "All Status" },
  { value: "pending", label: "Pending" },
  { value: "running", label: "Running" },
  { value: "success", label: "Success" },
  { value: "failed", label: "Failed" },
];

function getStatusConfig(status: string) {
  switch (status) {
    case "pending":
      return {
        label: "Pending",
        icon: Clock,
        className:
          "bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400",
      };
    case "running":
      return {
        label: "Running",
        icon: Loader2,
        className:
          "bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400",
        iconClassName: "animate-spin",
      };
    case "success":
    case "done":
      return {
        label: "Success",
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

function formatDuration(start: string, end: string): string {
  if (!start) return "-";
  const endTime = end ? new Date(end).getTime() : Date.now();
  const diff = endTime - new Date(start).getTime();
  const seconds = Math.floor(diff / 1000);
  const minutes = Math.floor(seconds / 60);

  if (minutes === 0) return `${seconds}s`;
  return `${minutes}m ${seconds % 60}s`;
}

export default function Builds() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const { buildId } = useParams();

  const [builds, setBuilds] = useState<IBuild[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showBuildModal, setShowBuildModal] = useState(false);

  // Filter & Pagination state
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("all");
  const [currentPage, setCurrentPage] = useState(1);

  const fetchBuilds = useCallback(async () => {
    const data = await get<IBuild[]>(`/api/apps/${app.id}/builds`);
    setBuilds(data);
    setIsLoading(false);
  }, [app.id, get]);

  // SSE subscription
  useEffect(() => {
    let isMounted = true;
    const evtRef = { current: null as EventSource | null };

    (async () => {
      const accessToken = await getAccessTokenSilently();
      if (!isMounted) return;

      const evtSource = new EventSource(
        `/api/apps/${app.id}/builds/stream?access_token=${accessToken}`
      );
      evtRef.current = evtSource;

      fetchBuilds();

      evtSource.onmessage = () => {
        if (!isMounted) return;
        fetchBuilds();
      };

      evtSource.onerror = (error) => {
        console.error("SSE error:", error);
      };
    })();

    return () => {
      isMounted = false;

      if (evtRef.current) {
        evtRef.current.close();
        evtRef.current = null;
      }
    };
  }, [app.id, fetchBuilds, getAccessTokenSilently]);

  // Filter builds
  const filteredBuilds = useMemo(() => {
    return builds.filter((build) => {
      // Status filter
      if (statusFilter !== "all") {
        const normalizedStatus = build.status === "done" ? "success" : build.status;
        if (normalizedStatus !== statusFilter) return false;
      }

      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        const name = (build.name || build.id).toLowerCase();
        if (!name.includes(query) && !build.id.toLowerCase().includes(query)) {
          return false;
        }
      }

      return true;
    });
  }, [builds, statusFilter, searchQuery]);

  // Pagination
  const totalPages = Math.ceil(filteredBuilds.length / ITEMS_PER_PAGE);
  const paginatedBuilds = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredBuilds.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredBuilds, currentPage]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [statusFilter, searchQuery]);

  const {
    register,
    formState: { errors },
    handleSubmit,
    reset,
  } = useForm<BuildInputs>();

  const onSubmit = async (inputs: BuildInputs) => {
    try {
      await post(`/api/apps/${app.id}/builds/build`, inputs);
      setShowBuildModal(false);
    } catch {
      alert("Something went wrong...");
    }
  };

  useEffect(() => {
    if (showBuildModal) {
      reset();
    }
  }, [showBuildModal, reset]);

  const selectedBuild = builds.find((b) => b.id === buildId);

  const handleSelectBuild = (id: string) => {
    navigate(id);
  };

  const hasFilters = statusFilter !== "all" || searchQuery !== "";

  return (
    <div className="flex h-full flex-col p-4">
      {/* Header */}
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Package className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Builds</h2>
          {!isLoading && (
            <Badge variant="secondary" className="ml-2">
              {builds.length}
            </Badge>
          )}
        </div>
        <Button onClick={() => setShowBuildModal(true)} size="sm">
          <Play className="mr-2 h-4 w-4" />
          New Build
        </Button>
      </div>

      {/* Main Content */}
      <div className="flex flex-1 gap-4 overflow-hidden rounded-lg border">
        {/* Builds List */}
        <div className="flex w-80 shrink-0 flex-col overflow-hidden border-r bg-muted/30">
          {/* Filters */}
          <div className="space-y-2 border-b p-3">
            <div className="relative">
              <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                placeholder="Search builds..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="h-8 pl-8 text-sm"
              />
            </div>
            <Select
              value={statusFilter}
              onValueChange={(v) => setStatusFilter(v as StatusFilter)}
            >
              <SelectTrigger className="h-8 text-sm">
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
          </div>

          {/* Build List */}
          <div className="flex-1 overflow-y-auto">
            {isLoading ? (
              <div className="flex items-center justify-center py-12">
                <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
              </div>
            ) : paginatedBuilds.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-12 text-center">
                <Package className="h-10 w-10 text-muted-foreground/50" />
                <p className="mt-2 text-sm text-muted-foreground">
                  {hasFilters ? "No builds match filters" : "No builds yet"}
                </p>
                {hasFilters ? (
                  <Button
                    variant="link"
                    size="sm"
                    onClick={() => {
                      setSearchQuery("");
                      setStatusFilter("all");
                    }}
                    className="mt-1 text-xs"
                  >
                    Clear filters
                  </Button>
                ) : (
                  <p className="text-xs text-muted-foreground">
                    Click "New Build" to start
                  </p>
                )}
              </div>
            ) : (
              <div className="divide-y">
                {paginatedBuilds.map((build) => {
                  const statusConfig = getStatusConfig(build.status);
                  const StatusIcon = statusConfig.icon;
                  const isSelected = buildId === build.id;

                  return (
                    <div
                      key={build.id}
                      onClick={() => handleSelectBuild(build.id)}
                      className={cn(
                        "flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-muted/50",
                        isSelected && "bg-muted"
                      )}
                    >
                      <StatusIcon
                        className={cn(
                          "h-4 w-4 shrink-0",
                          statusConfig.className
                            .split(" ")
                            .find((c) => c.startsWith("text-")),
                          statusConfig.iconClassName
                        )}
                      />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">
                          {build.name || `Build ${build.id.slice(0, 8)}`}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {formatTimestamp(build.createdAt)}
                        </p>
                      </div>
                      {isSelected && (
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </div>

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-3 py-2">
              <span className="text-xs text-muted-foreground">
                {filteredBuilds.length} builds
              </span>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  disabled={currentPage === 1}
                  onClick={() => setCurrentPage((p) => p - 1)}
                >
                  <ChevronLeft className="h-4 w-4" />
                </Button>
                <span className="min-w-[4rem] text-center text-xs text-muted-foreground">
                  {currentPage} / {totalPages}
                </span>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-7 w-7"
                  disabled={currentPage === totalPages}
                  onClick={() => setCurrentPage((p) => p + 1)}
                >
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </div>

        {/* Build Details */}
        <div className="flex flex-1 flex-col overflow-hidden">
          {selectedBuild ? (
            <>
              {/* Build Info Header */}
              <div className="flex items-center justify-between border-b px-4 py-3">
                <div className="flex items-center gap-3">
                  <div>
                    <h3 className="font-medium">
                      {selectedBuild.name || `Build ${selectedBuild.id.slice(0, 8)}`}
                    </h3>
                    <div className="flex items-center gap-3 text-xs text-muted-foreground">
                      <span className="flex items-center gap-1">
                        <GitCommit className="h-3 w-3" />
                        {selectedBuild.id.slice(0, 8)}
                      </span>
                      <span className="flex items-center gap-1">
                        <Timer className="h-3 w-3" />
                        {formatDuration(selectedBuild.createdAt, selectedBuild.finishedAt)}
                      </span>
                    </div>
                  </div>
                </div>
                <Badge
                  variant="secondary"
                  className={cn("shrink-0", getStatusConfig(selectedBuild.status).className)}
                >
                  {getStatusConfig(selectedBuild.status).label}
                </Badge>
              </div>

              {/* Build Logs */}
              <div className="flex-1 overflow-hidden">
                <BuildLogs appId={app.id} buildId={selectedBuild.id} />
              </div>
            </>
          ) : (
            <div className="flex h-full flex-col items-center justify-center text-center">
              <Terminal className="h-12 w-12 text-muted-foreground/30" />
              <p className="mt-4 text-sm font-medium text-muted-foreground">
                Select a build to view details
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                Build logs and status will appear here
              </p>
            </div>
          )}
        </div>
      </div>

      {/* New Build Dialog */}
      <Dialog open={showBuildModal} onOpenChange={setShowBuildModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>New Build</DialogTitle>
            <DialogDescription>
              Start a new build for your application
            </DialogDescription>
          </DialogHeader>
          <form id="build-form" onSubmit={handleSubmit(onSubmit)}>
            <div className="space-y-2">
              <Label htmlFor="build-name">Build Name</Label>
              <Input
                id="build-name"
                {...register("name")}
                placeholder="e.g., v1.0.0, feature-update"
              />
              {errors.name && (
                <span className="text-sm text-destructive">
                  {errors.name.message}
                </span>
              )}
            </div>
          </form>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowBuildModal(false)}>
              Cancel
            </Button>
            <Button type="submit" form="build-form">
              <Play className="mr-2 h-4 w-4" />
              Start Build
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
