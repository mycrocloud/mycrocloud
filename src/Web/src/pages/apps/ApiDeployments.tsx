import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AppContext } from ".";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Rocket,
  ChevronRight,
  ChevronLeft,
  Loader2,
  Play,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IApiDeployment {
  id: string;
  isActive: boolean;
  status: string;
  createdAt: string;
  routeCount: number;
}

const ITEMS_PER_PAGE = 10;

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

export default function ApiDeploymentsList() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const navigate = useNavigate();

  const [deployments, setDeployments] = useState<IApiDeployment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showPublishDialog, setShowPublishDialog] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);

  const fetchDeployments = useCallback(async () => {
    const data = await get<IApiDeployment[]>(`/api/apps/${app.id}/api/deployments`);
    setDeployments(data);
    setIsLoading(false);
  }, [app.id, get]);

  useEffect(() => {
    fetchDeployments();
  }, [fetchDeployments]);

  const totalPages = Math.ceil(deployments.length / ITEMS_PER_PAGE);
  const paginatedDeployments = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return deployments.slice(start, start + ITEMS_PER_PAGE);
  }, [deployments, currentPage]);

  const handlePublish = async () => {
    setIsPublishing(true);
    try {
      await post(`/api/apps/${app.id}/api/deployments/publish`, {});
      setShowPublishDialog(false);
      await fetchDeployments();
    } catch {
      alert("Failed to publish API. Please try again.");
    } finally {
      setIsPublishing(false);
    }
  };

  return (
    <div className="flex h-full flex-col p-4">
      {/* Header */}
      <div className="mb-4 flex items-center justify-between">
        <div className="flex items-center gap-2">
          {!isLoading && (
            <Badge variant="secondary">
              {deployments.length} total
            </Badge>
          )}
        </div>
        <Button onClick={() => setShowPublishDialog(true)} size="sm">
          <Play className="mr-2 h-4 w-4" />
          Publish API
        </Button>
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
              No API deployments yet
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Publish your API to create the first deployment
            </p>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Deployment</TableHead>
                <TableHead>Routes</TableHead>
                <TableHead>Created</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {paginatedDeployments.map((deployment) => {
                return (
                  <TableRow
                    key={deployment.id}
                    className={cn(
                      "cursor-pointer transition-colors hover:bg-muted/50",
                      deployment.isActive && "bg-green-50 dark:bg-green-900/10"
                    )}
                    onClick={() => navigate(`/apps/${app.id}/api/deployments/${deployment.id}`)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div>
                          <p className="font-medium text-sm font-mono">
                            {deployment.id.slice(0, 12)}
                          </p>
                        </div>
                        {deployment.isActive && (
                          <Badge className="bg-green-600 hover:bg-green-700">
                            Active
                          </Badge>
                        )}
                      </div>
                    </TableCell>
                    <TableCell>
                      <span className="text-sm text-muted-foreground">
                        {deployment.routeCount} routes
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
            {deployments.length} deployment{deployments.length !== 1 && "s"}
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

      {/* Publish Dialog */}
      <Dialog open={showPublishDialog} onOpenChange={setShowPublishDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Publish API</DialogTitle>
            <DialogDescription>
              This will create a snapshot of all active API routes and publish it as a new deployment. 
              The new deployment will automatically become active.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowPublishDialog(false)} disabled={isPublishing}>
              Cancel
            </Button>
            <Button onClick={handlePublish} disabled={isPublishing}>
              {isPublishing ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Publishing...
                </>
              ) : (
                <>
                  <Play className="mr-2 h-4 w-4" />
                  Publish
                </>
              )}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
