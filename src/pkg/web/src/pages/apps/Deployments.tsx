import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { AppContext } from ".";
import { useNavigate, Link } from "react-router-dom";
import { useForm } from "react-hook-form";
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Rocket,
  ChevronRight,
  ChevronLeft,
  Search,
  Loader2,
  Play,
  GitBranch,
  ChevronDown,
  ExternalLink,
  Github,
  ArrowRight,
} from "lucide-react";
import { cn } from "@/lib/utils";

interface IDeployment {
  id: string;
  isActive: boolean;
  name: string | null;
  status: string;
  buildId: string | null;
  build: {
    metadata: Record<string, string>;
  } | null;
  createdAt: string;
  artifactSize: number | null;
}

interface ISourceInfo {
  branch: string;
  repository: string;
  repositoryUrl: string;
  commit: {
    sha: string;
    message: string;
    author: string;
    date: string;
    url: string;
  };
}

type BuildInputs = {
  name?: string;
};

const ITEMS_PER_PAGE = 10;
const DEPLOYMENTS_POLL_INTERVAL_MS = 1000;

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

function getDeploymentDisplayName(deployment: IDeployment): string {
  if (deployment.name) {
    return deployment.name;
  }

  const commitMessage = deployment.build?.metadata?.commitMessage;
  if (commitMessage) {
    const firstLine = commitMessage.split('\n')[0];
    return firstLine.length > 50 ? firstLine.slice(0, 47) + '...' : firstLine;
  }

  const commitSha = deployment.build?.metadata?.commitSha;
  if (commitSha) {
    return commitSha.slice(0, 8);
  }

  return deployment.id.slice(0, 12);
}

function renderNonActiveStatusBadge(status: string) {
  const normalized = status.toLowerCase();

  if (normalized === "building") {
    return (
      <Badge className="bg-blue-600 hover:bg-blue-700">
        Building
      </Badge>
    );
  }

  if (normalized === "failed") {
    return (
      <Badge className="bg-red-600 hover:bg-red-700">
        Failed
      </Badge>
    );
  }

  return null;
}

export default function DeploymentsList() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const navigate = useNavigate();

  const [deployments, setDeployments] = useState<IDeployment[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [showBuildModal, setShowBuildModal] = useState(false);

  // Source info state
  const [sourceInfo, setSourceInfo] = useState<ISourceInfo | null>(null);
  const [loadingSourceInfo, setLoadingSourceInfo] = useState(false);
  const [sourceInfoError, setSourceInfoError] = useState<string | null>(null);
  const [sourceInfoOpen, setSourceInfoOpen] = useState(false);

  // Filter & Pagination state
  const [searchQuery, setSearchQuery] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const isFetchingDeploymentsRef = useRef(false);

  const fetchDeployments = useCallback(async () => {
    if (isFetchingDeploymentsRef.current) return;

    isFetchingDeploymentsRef.current = true;
    try {
      const data = await get<IDeployment[]>(`/api/apps/${app.id}/spa/deployments`);
      setDeployments(data);
    } catch (error) {
      console.error("Failed to fetch deployments:", error);
    } finally {
      setIsLoading(false);
      isFetchingDeploymentsRef.current = false;
    }
  }, [app.id, get]);

  // Initial fetch + polling
  useEffect(() => {
    fetchDeployments();
    const timer = setInterval(fetchDeployments, DEPLOYMENTS_POLL_INTERVAL_MS);

    return () => {
      clearInterval(timer);
    };
  }, [fetchDeployments]);

  // Filter deployments
  const filteredDeployments = useMemo(() => {
    return deployments.filter((deployment) => {
      // Search filter
      if (searchQuery) {
        const query = searchQuery.toLowerCase();
        const name = (deployment.name || "").toLowerCase();
        const id = deployment.id.toLowerCase();
        const commitMessage = (deployment.build?.metadata?.commitMessage || "").toLowerCase();
        const commitSha = (deployment.build?.metadata?.commitSha || "").toLowerCase();

        if (
          !name.includes(query) &&
          !id.includes(query) &&
          !commitMessage.includes(query) &&
          !commitSha.includes(query)
        ) {
          return false;
        }
      }

      return true;
    });
  }, [deployments, searchQuery]);

  // Pagination
  const totalPages = Math.ceil(filteredDeployments.length / ITEMS_PER_PAGE);
  const paginatedDeployments = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredDeployments.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredDeployments, currentPage]);

  // Reset page when filters change
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery]);

  const hasFilters = searchQuery !== "";

  const {
    register,
    formState: { errors },
    handleSubmit,
    reset,
  } = useForm<BuildInputs>();

  const onSubmit = async (inputs: BuildInputs) => {
    try {
      await post(`/api/apps/${app.id}/spa/builds/build`, inputs);
      setShowBuildModal(false);
      // Refresh deployments list after successful build trigger
      await fetchDeployments();
    } catch {
      alert("Something went wrong...");
    }
  };

  const fetchSourceInfo = async () => {
    setLoadingSourceInfo(true);
    setSourceInfoError(null);
    try {
      const data = await get<ISourceInfo>(`/api/apps/${app.id}/source-info`);
      setSourceInfo(data);
    } catch (error: any) {
      setSourceInfoError(
        error.message || "Failed to load source information"
      );
    } finally {
      setLoadingSourceInfo(false);
    }
  };

  useEffect(() => {
    if (showBuildModal) {
      reset();
      // Reset source info when modal opens
      setSourceInfo(null);
      setSourceInfoError(null);
      setSourceInfoOpen(false);
    }
  }, [showBuildModal, reset]);

  const hasGitHubIntegration = !!app.gitIntegration;

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
        <Button
          onClick={() => setShowBuildModal(true)}
          size="sm"
          disabled={!hasGitHubIntegration}
          title={!hasGitHubIntegration ? "Connect a GitHub repository first" : ""}
        >
          <Play className="mr-2 h-4 w-4" />
          New Deployment
        </Button>
      </div>

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
                Connect your GitHub repository to enable automatic deployments when you push code.
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
        {hasFilters && (
          <Button
            variant="ghost"
            size="sm"
            onClick={() => {
              setSearchQuery("");
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
                <TableHead>Deployment</TableHead>
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
                    onClick={() => navigate(`/apps/${app.id}/spa/deployments/${deployment.id}`)}
                  >
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <div>
                          <p className="font-medium text-sm">
                            {getDeploymentDisplayName(deployment)}
                          </p>
                          {deployment.build?.metadata?.commitSha && (
                            <p className="text-xs text-muted-foreground font-mono">
                              {deployment.build.metadata.commitSha.slice(0, 8)}
                            </p>
                          )}
                        </div>
                        {deployment.isActive ? (
                          <Badge className="bg-green-600 hover:bg-green-700">
                            Active
                          </Badge>
                        ) : (
                          renderNonActiveStatusBadge(deployment.status)
                        )}
                      </div>
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

      {/* New Build Dialog */}
      <Dialog open={showBuildModal} onOpenChange={setShowBuildModal}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New Deployment</DialogTitle>
            <DialogDescription>
              Build your application and deploy it
            </DialogDescription>
          </DialogHeader>
          <form id="build-form" onSubmit={handleSubmit(onSubmit)}>
            <div className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="build-name">Deployment Name (Optional)</Label>
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

              {/* Source Information Collapsible */}
              <Collapsible
                open={sourceInfoOpen}
                onOpenChange={setSourceInfoOpen}
                className="space-y-2"
              >
                <CollapsibleTrigger asChild>
                  <Button
                    variant="ghost"
                    size="sm"
                    className="flex w-full items-center justify-between p-2 text-sm"
                    type="button"
                  >
                    <div className="flex items-center gap-2">
                      <GitBranch className="h-4 w-4" />
                      <span>Source Information (Optional)</span>
                    </div>
                    <ChevronDown
                      className={cn(
                        "h-4 w-4 transition-transform",
                        sourceInfoOpen && "rotate-180"
                      )}
                    />
                  </Button>
                </CollapsibleTrigger>
                <CollapsibleContent className="space-y-3 rounded-md border p-4">
                  {!sourceInfo && !sourceInfoError && (
                    <div className="text-center">
                      <p className="mb-3 text-sm text-muted-foreground">
                        Preview the latest commit from your repository
                      </p>
                      <Button
                        onClick={fetchSourceInfo}
                        disabled={loadingSourceInfo}
                        size="sm"
                        variant="outline"
                        type="button"
                      >
                        {loadingSourceInfo ? (
                          <>
                            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                            Loading...
                          </>
                        ) : (
                          <>
                            <GitBranch className="mr-2 h-4 w-4" />
                            Load Source Info
                          </>
                        )}
                      </Button>
                    </div>
                  )}

                  {sourceInfoError && (
                    <div className="rounded-md bg-destructive/10 p-3">
                      <p className="text-sm text-destructive">
                        {sourceInfoError}
                      </p>
                    </div>
                  )}

                  {sourceInfo && (
                    <div className="space-y-3 text-sm">
                      <div>
                        <p className="text-xs font-medium text-muted-foreground">
                          Repository
                        </p>
                        <a
                          href={sourceInfo.repositoryUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center gap-1 font-mono text-sm text-blue-600 hover:underline dark:text-blue-400"
                        >
                          {sourceInfo.repository}
                          <ExternalLink className="h-3 w-3" />
                        </a>
                      </div>
                      <div>
                        <p className="text-xs font-medium text-muted-foreground">
                          Branch
                        </p>
                        <p className="font-mono">{sourceInfo.branch}</p>
                      </div>
                      <div>
                        <p className="text-xs font-medium text-muted-foreground">
                          Commit
                        </p>
                        <a
                          href={sourceInfo.commit.url}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="inline-flex items-center gap-1 font-mono text-xs text-blue-600 hover:underline dark:text-blue-400"
                        >
                          {sourceInfo.commit.sha.slice(0, 8)}
                          <ExternalLink className="h-3 w-3" />
                        </a>
                      </div>
                      <div>
                        <p className="text-xs font-medium text-muted-foreground">
                          Message
                        </p>
                        <p className="line-clamp-2">
                          {sourceInfo.commit.message}
                        </p>
                      </div>
                      <div className="flex items-center justify-between">
                        <div>
                          <p className="text-xs font-medium text-muted-foreground">
                            Author
                          </p>
                          <p>{sourceInfo.commit.author}</p>
                        </div>
                        <div>
                          <p className="text-xs font-medium text-muted-foreground">
                            Date
                          </p>
                          <p>{formatTimestamp(sourceInfo.commit.date)}</p>
                        </div>
                      </div>
                    </div>
                  )}
                </CollapsibleContent>
              </Collapsible>
            </div>
          </form>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowBuildModal(false)}>
              Cancel
            </Button>
            <Button type="submit" form="build-form">
              <Play className="mr-2 h-4 w-4" />
              Deploy
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
