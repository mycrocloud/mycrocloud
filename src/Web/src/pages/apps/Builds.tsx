import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { useAuth0 } from "@auth0/auth0-react";
import BuildLogs from "./BuildLogs";
import { useForm } from "react-hook-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Play,
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  Package,
  Terminal,
  ChevronRight,
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

export default function Builds() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { getAccessTokenSilently } = useAuth0();

  const [builds, setBuilds] = useState<IBuild[]>([]);
  const [buildId, setBuildId] = useState<string>();
  const [showBuildModal, setShowBuildModal] = useState(false);

  const fetchBuilds = useCallback(async () => {
    const data = await get<IBuild[]>(`/api/apps/${app.id}/builds`);
    setBuilds(data);
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

      // Initial load
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

  return (
    <div className="flex h-full flex-col gap-4 p-4">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Package className="h-5 w-5 text-muted-foreground" />
          <h2 className="text-lg font-semibold">Builds</h2>
          <Badge variant="secondary" className="ml-2">
            {builds.length}
          </Badge>
        </div>
        <Button onClick={() => setShowBuildModal(true)} size="sm">
          <Play className="mr-2 h-4 w-4" />
          New Build
        </Button>
      </div>

      {/* Main Content */}
      <div className="flex flex-1 gap-4 overflow-hidden">
        {/* Builds List */}
        <Card className="w-80 shrink-0 overflow-hidden">
          <CardHeader className="pb-3">
            <CardTitle className="text-sm font-medium">Build History</CardTitle>
          </CardHeader>
          <CardContent className="max-h-[calc(100vh-280px)] overflow-y-auto p-0">
            {builds.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-8 text-center">
                <Package className="h-10 w-10 text-muted-foreground/50" />
                <p className="mt-2 text-sm text-muted-foreground">
                  No builds yet
                </p>
                <p className="text-xs text-muted-foreground">
                  Click "New Build" to start
                </p>
              </div>
            ) : (
              <div className="divide-y">
                {builds.map((build) => {
                  const statusConfig = getStatusConfig(build.status);
                  const StatusIcon = statusConfig.icon;
                  const isSelected = buildId === build.id;

                  return (
                    <div
                      key={build.id}
                      onClick={() => setBuildId(build.id)}
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
                          {build.name || "Unnamed build"}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {formatTimestamp(build.createdAt)}
                        </p>
                      </div>
                      <Badge
                        variant="secondary"
                        className={cn("shrink-0 text-xs", statusConfig.className)}
                      >
                        {statusConfig.label}
                      </Badge>
                      {isSelected && (
                        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </CardContent>
        </Card>

        {/* Build Logs */}
        <Card className="flex-1 overflow-hidden">
          <CardHeader className="pb-3">
            <div className="flex items-center gap-2">
              <Terminal className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-sm font-medium">Build Logs</CardTitle>
            </div>
            {selectedBuild && (
              <CardDescription>
                {selectedBuild.name || "Unnamed build"}
              </CardDescription>
            )}
          </CardHeader>
          <CardContent className="h-[calc(100%-80px)] p-0">
            {buildId ? (
              <BuildLogs appId={app.id} buildId={buildId} />
            ) : (
              <div className="flex h-full flex-col items-center justify-center text-center">
                <Terminal className="h-12 w-12 text-muted-foreground/50" />
                <p className="mt-4 text-sm text-muted-foreground">
                  Select a build to view logs
                </p>
              </div>
            )}
          </CardContent>
        </Card>
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
