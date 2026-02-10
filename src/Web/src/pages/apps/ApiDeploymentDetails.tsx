import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Loader2,
  ArrowLeft,
  GitBranch,
  File,
} from "lucide-react";

interface IApiDeployment {
  id: string;
  isActive: boolean;
  status: string;
  createdAt: string;
  routeCount: number;
  totalFiles: number;
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

export default function ApiDeploymentDetails() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get } = useApiClient();
  const { deploymentId } = useParams();

  const [deployment, setDeployment] = useState<IApiDeployment | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const fetchDeployment = useCallback(async () => {
    try {
      const data = await get<IApiDeployment>(
        `/api/apps/${app.id}/api/deployments/${deploymentId}`
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
          <Link to={`/apps/${app.id}/api/deployments`}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to API Deployments
          </Link>
        </Button>
      </div>
    );
  }

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="border-b">
        <div className="flex items-center justify-between px-4 py-3">
          <div className="flex items-center gap-3">
            <Button asChild variant="ghost" size="sm">
              <Link to={`/apps/${app.id}/api/deployments`}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back
              </Link>
            </Button>
          </div>
          {deployment.isActive && (
            <Badge className="bg-green-600 hover:bg-green-700">
              Active
            </Badge>
          )}
        </div>
        <div className="px-4 pb-4">
          <h1 className="text-2xl font-bold font-mono">
            API Deployment {deployment.id.slice(0, 12)}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            {formatTimestamp(deployment.createdAt)}
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-4">
        <div className="mx-auto max-w-4xl space-y-6">
          {/* Metrics Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Routes Card */}
            <div className="rounded-lg border bg-card p-4">
              <div className="flex items-center gap-2 mb-2">
                <GitBranch className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium text-muted-foreground">Routes</span>
              </div>
              <p className="text-2xl font-bold">{deployment.routeCount}</p>
            </div>

            {/* Files Card */}
            <div className="rounded-lg border bg-card p-4">
              <div className="flex items-center gap-2 mb-2">
                <File className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium text-muted-foreground">Total Files</span>
              </div>
              <p className="text-2xl font-bold">{deployment.totalFiles}</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
