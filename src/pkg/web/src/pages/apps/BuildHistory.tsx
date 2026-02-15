import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AppContext } from ".";
import { useAuth0 } from "@auth0/auth0-react";
import { useForm } from "react-hook-form";
import { useNavigate, Link } from "react-router-dom";
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Play,
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  ChevronRight,
  ChevronLeft,
  Timer,
  Search,
  Filter,
  GitBranch,
  Github,
  ArrowRight,
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

export default function BuildHistory() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();

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

  const hasFilters = statusFilter !== "all" || searchQuery !== "";
  const hasGitHubIntegration = !!app.gitIntegration;

  return (
    <div className="flex h-full flex-col p-4">
      {/* GitHub Integration Warning */}
      {!hasGitHubIntegration && (
        <div className="mb-4 rounded-lg border border-yellow-200 bg-yellow-50 p-4 dark:border-yellow-900 dark:bg-yellow-950/20">
          <div className="flex items-start gap-3">
            <Github className="h-5 w-5 text-yellow-600 dark:text-yellow-400 mt-0.5" />
            <div className="flex-1">
              <p className="text-sm font-medium text-yellow-900 dark:text-yellow-100">
                GitHub Integration Required
              </p>
              <p className="mt-1 text-sm text-yellow-700 dark:text-yellow-300">
                Connect your GitHub repository in settings to start building and deploying your application.
              </p>
              <Button
                variant="outline"
                size="sm"
                className="mt-3 border-yellow-600 text-yellow-700 hover:bg-yellow-100 dark:border-yellow-400 dark:text-yellow-300 dark:hover:bg-yellow-900/30"
                asChild
              >
                <Link to={`/apps/${app.id}/settings/pages`}>
                  Connect GitHub
                  <ArrowRight className="ml-2 h-4 w-4" />
                </Link>
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          {!isLoading && (
            <Badge variant="secondary">
              {builds.length} total
            </Badge>
          )}
        </div>
        <Button 
          onClick={() => setShowBuildModal(true)} 
          size="sm"
          disabled={!hasGitHubIntegration}
          title={!hasGitHubIntegration ? "Connect a GitHub repository first" : ""}
        >
          <Play className="mr-2 h-4 w-4" />
          New Build
        </Button>
      </div>

      {/* Filters */}
      <div className="mb-4 flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder="Search builds..."
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

      {/* Builds Table */}
      <div className="flex-1 overflow-hidden rounded-lg border">
        {isLoading ? (
          <div className="flex h-full items-center justify-center">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : paginatedBuilds.length === 0 ? (
          <div className="flex h-full flex-col items-center justify-center py-12 text-center">
            <GitBranch className="h-12 w-12 text-muted-foreground/50" />
            <p className="mt-4 text-sm text-muted-foreground">
              {hasFilters ? "No builds match filters" : "No builds yet"}
            </p>
            {!hasFilters && (
              <p className="mt-1 text-xs text-muted-foreground">
                Click "New Build" to start your first build
              </p>
            )}
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[300px]">Build</TableHead>
                <TableHead className="w-[120px]">Status</TableHead>
                <TableHead className="w-[120px]">Duration</TableHead>
                <TableHead>Started</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedBuilds.map((build) => {
                const statusConfig = getStatusConfig(build.status);
                const StatusIcon = statusConfig.icon;

                return (
                  <TableRow
                    key={build.id}
                    className="cursor-pointer"
                    onClick={() => navigate(build.id)}
                  >
                    <TableCell>
                      <div>
                        <p className="font-medium">
                          {build.name || `Build ${build.id.slice(0, 8)}`}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {build.id.slice(0, 8)}
                        </p>
                      </div>
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
                      <span className="flex items-center gap-1 text-sm text-muted-foreground">
                        <Timer className="h-3.5 w-3.5" />
                        {formatDuration(build.createdAt, build.finishedAt)}
                      </span>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {formatTimestamp(build.createdAt)}
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
            {filteredBuilds.length} build{filteredBuilds.length !== 1 && "s"}
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
