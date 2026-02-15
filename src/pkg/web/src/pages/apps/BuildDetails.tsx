import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { useAuth0 } from "@auth0/auth0-react";
import BuildLogs from "./BuildLogs";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  ArrowLeft,
  Timer,
  Copy,
  Check,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IBuild {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  finishedAt: string;
}

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
  return date.toLocaleString("en-US", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
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

export default function BuildDetails() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get } = useApiClient();
  const { getAccessTokenSilently } = useAuth0();
  const { buildId } = useParams();
  const navigate = useNavigate();

  const [build, setBuild] = useState<IBuild | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [copied, setCopied] = useState(false);

  const fetchBuild = useCallback(async () => {
    const data = await get<IBuild[]>(`/api/apps/${app.id}/spa/builds`);
    const found = data.find((b) => b.id === buildId);
    setBuild(found || null);
    setIsLoading(false);
  }, [app.id, buildId, get]);

  // SSE subscription for real-time updates
  useEffect(() => {
    let isMounted = true;
    const evtRef = { current: null as EventSource | null };

    (async () => {
      const accessToken = await getAccessTokenSilently();
      if (!isMounted) return;

      const evtSource = new EventSource(
        `/api/apps/${app.id}/spa/builds/stream?access_token=${accessToken}`
      );
      evtRef.current = evtSource;

      fetchBuild();

      evtSource.onmessage = () => {
        if (!isMounted) return;
        fetchBuild();
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
  }, [app.id, fetchBuild, getAccessTokenSilently]);

  const handleCopyLink = async () => {
    await navigator.clipboard.writeText(window.location.href);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!build) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Build not found</p>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Go Back
          </Button>
          <Button asChild variant="outline">
            <Link to={`/apps/${app.id}/spa/deployments`}>
              View Deployments
            </Link>
          </Button>
        </div>
      </div>
    );
  }

  const statusConfig = getStatusConfig(build.status);
  const StatusIcon = statusConfig.icon;

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b px-4 py-3">
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back
          </Button>
          <div className="h-6 w-px bg-border" />
          <div>
            <div className="flex items-center gap-2">
              <h2 className="font-semibold">
                {build.name || `Build ${build.id.slice(0, 8)}`}
              </h2>
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
            </div>
            <div className="flex items-center gap-4 text-xs text-muted-foreground">
              <span>ID: {build.id.slice(0, 8)}</span>
              <span className="flex items-center gap-1">
                <Timer className="h-3 w-3" />
                {formatDuration(build.createdAt, build.finishedAt)}
              </span>
              <span>Started: {formatTimestamp(build.createdAt)}</span>
            </div>
          </div>
        </div>
        <Button variant="outline" size="sm" onClick={handleCopyLink}>
          {copied ? (
            <Check className="h-4 w-4 text-green-500" />
          ) : (
            <Copy className="h-4 w-4" />
          )}
        </Button>
      </div>

      {/* Logs - Full Height */}
      <div className="flex-1 overflow-hidden">
        <BuildLogs appId={app.id} buildId={build.id} />
      </div>
    </div>
  );
}
