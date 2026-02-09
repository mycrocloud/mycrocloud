import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  CheckCircle2,
  XCircle,
  Clock,
  Loader2,
  ArrowLeft,
  Copy,
  Check,
  Archive,
  Package,
  FileArchive,
  GitBranch,
  ExternalLink,
  RotateCcw,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IDeployment {
  id: string;
  status: string;
  buildId: string | null;
  buildName: string | null;
  createdAt: string;
  artifactSize: number;
  artifactHash: string;
  artifactId?: string; // Add artifactId
}

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
  return date.toLocaleString("en-US", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

export default function DeploymentDetails() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { deploymentId } = useParams();
  const navigate = useNavigate();

  const [deployment, setDeployment] = useState<IDeployment | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [copied, setCopied] = useState(false);
  const [isRedeploying, setIsRedeploying] = useState(false);

  const fetchDeployment = useCallback(async () => {
    try {
      const data = await get<IDeployment>(
        `/api/apps/${app.id}/deployments/${deploymentId}`
      );
      setDeployment(data);
      setIsLoading(false);
    } catch (error) {
      console.error("Failed to fetch deployment:", error);
      setDeployment(null);
      setIsLoading(false);
    }
  }, [app.id, deploymentId, get]);

  useEffect(() => {
    fetchDeployment();
  }, [fetchDeployment]);

  // Poll for updates every 5 seconds
  useEffect(() => {
    const intervalId = setInterval(() => {
      fetchDeployment();
    }, 5000);

    return () => clearInterval(intervalId);
  }, [fetchDeployment]);

  const handleCopyId = async () => {
    if (deployment) {
      await navigator.clipboard.writeText(deployment.id);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleRedeploy = async () => {
    if (!deployment || !deployment.artifactId) return;
    
    setIsRedeploying(true);
    try {
      const result = await post<{ deploymentId: string }>(
        `/api/apps/${app.id}/deployments/spa/redeploy/${deployment.artifactId}`,
        {}
      );
      
      // Navigate to the new deployment
      if (result.deploymentId) {
        navigate(`/apps/${app.id}/deployments/${result.deploymentId}`);
      }
    } catch (error) {
      alert("Failed to redeploy. Please try again.");
    } finally {
      setIsRedeploying(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
      </div>
    );
  }

  if (!deployment) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-4">
        <p className="text-muted-foreground">Deployment not found</p>
        <Button asChild variant="outline">
          <Link to={`/apps/${app.id}/deployments`}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Deployments
          </Link>
        </Button>
      </div>
    );
  }

  const statusConfig = getStatusConfig(deployment.status);
  const StatusIcon = statusConfig.icon;

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="flex items-center justify-between border-b px-4 py-3">
        <div className="flex items-center gap-4">
          <Button asChild variant="ghost" size="sm">
            <Link to={`/apps/${app.id}/deployments`}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              Back
            </Link>
          </Button>
          {deployment.status === "Ready" && (
            <Button
              variant="outline"
              size="sm"
              onClick={handleRedeploy}
              disabled={isRedeploying}
            >
              {isRedeploying ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <RotateCcw className="mr-2 h-4 w-4" />
              )}
              Redeploy
            </Button>
          )}
          <div>
            <h1 className="font-semibold">Deployment</h1>
            <div className="flex items-center gap-2">
              <p className="text-sm text-muted-foreground font-mono">
                {deployment.id.slice(0, 12)}
              </p>
              <Button
                variant="ghost"
                size="sm"
                className="h-6 w-6 p-0"
                onClick={handleCopyId}
              >
                {copied ? (
                  <Check className="h-3 w-3 text-green-600" />
                ) : (
                  <Copy className="h-3 w-3" />
                )}
              </Button>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-2">
          <Badge
            variant="secondary"
            className={cn("text-sm", statusConfig.className)}
          >
            <StatusIcon
              className={cn("mr-1.5 h-4 w-4", statusConfig.iconClassName)}
            />
            {statusConfig.label}
          </Badge>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-4">
        <div className="mx-auto max-w-4xl space-y-6">
          {/* Deployment Info */}
          <div className="rounded-lg border bg-card">
            <div className="border-b px-4 py-3">
              <h2 className="font-semibold">Deployment Information</h2>
            </div>
            <div className="divide-y">
              <div className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Package className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium">Deployment ID</span>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-sm text-muted-foreground font-mono">
                    {deployment.id}
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 w-6 p-0"
                    onClick={handleCopyId}
                  >
                    {copied ? (
                      <Check className="h-3 w-3 text-green-600" />
                    ) : (
                      <Copy className="h-3 w-3" />
                    )}
                  </Button>
                </div>
              </div>
              
              <div className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <Clock className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium">Created</span>
                </div>
                <span className="text-sm text-muted-foreground">
                  {formatTimestamp(deployment.createdAt)}
                </span>
              </div>

              <div className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium">Status</span>
                </div>
                <Badge
                  variant="secondary"
                  className={cn("text-sm", statusConfig.className)}
                >
                  <StatusIcon
                    className={cn("mr-1.5 h-3 w-3", statusConfig.iconClassName)}
                  />
                  {statusConfig.label}
                </Badge>
              </div>
            </div>
          </div>

          {/* Build Info */}
          {deployment.buildId && (
            <div className="rounded-lg border bg-card">
              <div className="border-b px-4 py-3">
                <h2 className="font-semibold">Build Information</h2>
              </div>
              <div className="divide-y">
                <div className="px-4 py-3 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <GitBranch className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm font-medium">Build Name</span>
                  </div>
                  <span className="text-sm text-muted-foreground">
                    {deployment.buildName || "-"}
                  </span>
                </div>

                <div className="px-4 py-3 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <Package className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm font-medium">Build ID</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-sm text-muted-foreground font-mono">
                      {deployment.buildId.slice(0, 8)}
                    </span>
                    <Button
                      asChild
                      variant="ghost"
                      size="sm"
                      className="h-6 w-6 p-0"
                    >
                      <Link to={`/apps/${app.id}/builds/${deployment.buildId}`}>
                        <ExternalLink className="h-3 w-3" />
                      </Link>
                    </Button>
                  </div>
                </div>
              </div>
            </div>
          )}

          {/* Artifact Info */}
          <div className="rounded-lg border bg-card">
            <div className="border-b px-4 py-3">
              <h2 className="font-semibold">Artifact Information</h2>
            </div>
            <div className="divide-y">
              <div className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileArchive className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium">Size</span>
                </div>
                <span className="text-sm text-muted-foreground">
                  {formatBytes(deployment.artifactSize)}
                </span>
              </div>

              <div className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileArchive className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium">Hash</span>
                </div>
                <span className="text-sm text-muted-foreground font-mono">
                  {deployment.artifactHash.slice(0, 16)}...
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
