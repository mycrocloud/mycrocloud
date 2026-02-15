import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { useAuth0 } from "@auth0/auth0-react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Loader2,
  ArrowLeft,
  FileArchive,
  GitBranch,
  ExternalLink,
  RotateCcw,
  ChevronDown,
  ChevronRight,
  File,
  Search,
  User,
  Hash,
  Download,
} from "lucide-react";

interface IDeployment {
  id: string;
  isActive: boolean;
  buildId: string | null;
  buildName: string | null;
  createdAt: string;
  artifactSize: number;
  artifactHash: string;
  artifactId?: string;
  build?: {
    metadata: Record<string, string>;
  };
}

interface IDeploymentFile {
  path: string;
  sizeBytes: number;
  eTag: string;
  contentType: string;
  createdAt: string;
}

interface IDeploymentFiles {
  totalFiles: number;
  totalSize: number;
  files: IDeploymentFile[];
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

function getDeploymentTitle(deployment: IDeployment): string {
  const commitMessage = deployment.build?.metadata?.commitMessage;
  if (commitMessage) {
    const firstLine = commitMessage.split('\n')[0];
    return firstLine.length > 60 ? firstLine.slice(0, 57) + '...' : firstLine;
  }

  const branch = deployment.build?.metadata?.branch;
  if (branch) {
    return `Deploy from ${branch}`;
  }

  return `Deployment ${deployment.id.slice(0, 8)}`;
}

function getDeploymentSubtitle(deployment: IDeployment): string | null {
  const commitSha = deployment.build?.metadata?.commitSha;
  const branch = deployment.build?.metadata?.branch;

  if (commitSha && branch) {
    return `${branch} • ${commitSha.slice(0, 8)}`;
  }

  if (commitSha) {
    return commitSha.slice(0, 8);
  }

  if (branch) {
    return branch;
  }

  return null;
}

export default function DeploymentDetails() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { getAccessTokenSilently } = useAuth0();
  const { deploymentId } = useParams();
  const navigate = useNavigate();

  const [deployment, setDeployment] = useState<IDeployment | null>(null);
  const [files, setFiles] = useState<IDeploymentFiles | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingFiles, setIsLoadingFiles] = useState(false);
  const [showFiles, setShowFiles] = useState(false);
  const [showBuildDetails, setShowBuildDetails] = useState(false);
  const [fileSearch, setFileSearch] = useState("");
  const [isRedeploying, setIsRedeploying] = useState(false);

  const fetchDeployment = useCallback(async () => {
    try {
      const data = await get<IDeployment>(
        `/api/apps/${app.id}/spa/deployments/${deploymentId}`
      );
      setDeployment(data);
      setIsLoading(false);
    } catch (error) {
      console.error("Failed to fetch deployment:", error);
      setDeployment(null);
      setIsLoading(false);
    }
  }, [app.id, deploymentId, get]);

  const fetchFiles = useCallback(async () => {
    if (!showFiles || files) return;

    setIsLoadingFiles(true);
    try {
      const data = await get<IDeploymentFiles>(
        `/api/apps/${app.id}/spa/deployments/${deploymentId}/files`
      );
      setFiles(data);
    } catch (error) {
      console.error("Failed to fetch files:", error);
    } finally {
      setIsLoadingFiles(false);
    }
  }, [app.id, deploymentId, get, showFiles, files]);

  useEffect(() => {
    fetchDeployment();
  }, [fetchDeployment]);

  useEffect(() => {
    if (showFiles) {
      fetchFiles();
    }
  }, [showFiles, fetchFiles]);

  const handleRedeploy = async () => {
    if (!deployment || !deployment.artifactId) return;

    setIsRedeploying(true);
    try {
      const result = await post<{ deploymentId: string }>(
        `/api/apps/${app.id}/spa/deployments/redeploy/${deployment.artifactId}`,
        {}
      );

      // Navigate to the new deployment
      if (result.deploymentId) {
        navigate(`/apps/${app.id}/spa/deployments/${result.deploymentId}`);
      }
    } catch (error) {
      alert("Failed to redeploy. Please try again.");
    } finally {
      setIsRedeploying(false);
    }
  };

  const handleDownload = async () => {
    if (!deployment) return;

    try {
      // Get the authentication token
      const token = await getAccessTokenSilently();

      // Create download link with authentication
      const downloadUrl = `/api/apps/${app.id}/spa/builds/${deploymentId}/download`;

      // Use fetch to download with authentication header
      const response = await fetch(downloadUrl, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });

      if (!response.ok) {
        throw new Error('Download failed');
      }

      // Create blob from response
      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);

      // Trigger download
      const link = document.createElement('a');
      link.href = url;
      link.download = `deployment-${deploymentId?.slice(0, 8)}.zip`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);

      // Clean up
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error('Download failed:', error);
      alert('Failed to download artifact. Please try again.');
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
          <Link to={`/apps/${app.id}/spa/deployments`}>
            <ArrowLeft className="mr-2 h-4 w-4" />
            Back to Deployments
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
              <Link to={`/apps/${app.id}/spa/deployments`}>
                <ArrowLeft className="mr-2 h-4 w-4" />
                Back
              </Link>
            </Button>
            {!deployment.isActive && deployment.artifactId && (
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
            {deployment.artifactId && (
              <Button
                variant="outline"
                size="sm"
                onClick={handleDownload}
              >
                <Download className="mr-2 h-4 w-4" />
                Download
              </Button>
            )}
          </div>
          {deployment.isActive && (
            <Badge className="bg-green-600 hover:bg-green-700">
              Active
            </Badge>
          )}
        </div>
        <div className="px-4 pb-4">
          <h1 className="text-2xl font-bold">{getDeploymentTitle(deployment)}</h1>
          {getDeploymentSubtitle(deployment) && (
            <p className="text-sm text-muted-foreground mt-1 font-mono">
              {getDeploymentSubtitle(deployment)}
            </p>
          )}
          <p className="text-sm text-muted-foreground mt-1">
            {formatTimestamp(deployment.createdAt)}
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-4">
        <div className="mx-auto max-w-4xl space-y-6">
          {/* Metrics Cards */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            {/* Size Card */}
            <div className="rounded-lg border bg-card p-4">
              <div className="flex items-center gap-2 mb-2">
                <FileArchive className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium text-muted-foreground">Size</span>
              </div>
              <p className="text-2xl font-bold">{formatBytes(deployment.artifactSize)}</p>
            </div>

            {/* Files Card */}
            <div className="rounded-lg border bg-card p-4">
              <div className="flex items-center gap-2 mb-2">
                <File className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium text-muted-foreground">Files</span>
              </div>
              <p className="text-2xl font-bold">
                {files ? files.totalFiles : (
                  <Button
                    variant="link"
                    size="sm"
                    className="h-auto p-0 text-2xl font-bold"
                    onClick={() => setShowFiles(true)}
                  >
                    View
                  </Button>
                )}
              </p>
            </div>

            {/* Build Logs Card */}
            {deployment.buildId && (
              <div className="rounded-lg border bg-card p-4">
                <div className="flex items-center gap-2 mb-2">
                  <GitBranch className="h-4 w-4 text-muted-foreground" />
                  <span className="text-sm font-medium text-muted-foreground">Build</span>
                </div>
                <Button
                  asChild
                  variant="outline"
                  size="sm"
                  className="w-full"
                >
                  <Link to={`/apps/${app.id}/spa/builds/${deployment.buildId}`}>
                    <ExternalLink className="mr-2 h-4 w-4" />
                    View Logs
                  </Link>
                </Button>
              </div>
            )}
          </div>

          {/* Build Details (Expandable) */}
          {deployment.buildId && deployment.build?.metadata && (
            <Collapsible open={showBuildDetails} onOpenChange={setShowBuildDetails}>
              <div className="rounded-lg border bg-card">
                <CollapsibleTrigger className="w-full">
                  <div className="px-4 py-3 flex items-center justify-between hover:bg-muted/50 transition-colors">
                    <div className="flex items-center gap-2">
                      <GitBranch className="h-4 w-4 text-muted-foreground" />
                      <h2 className="font-semibold">Build Details</h2>
                    </div>
                    {showBuildDetails ? (
                      <ChevronDown className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <ChevronRight className="h-4 w-4 text-muted-foreground" />
                    )}
                  </div>
                </CollapsibleTrigger>

                <CollapsibleContent>
                  <div className="divide-y">
                    {deployment.build.metadata.commitMessage && (
                      <div className="px-4 py-3">
                        <div className="flex items-start gap-2 mb-2">
                          <File className="h-4 w-4 text-muted-foreground mt-0.5" />
                          <span className="text-sm font-medium">Commit Message</span>
                        </div>
                        <p className="text-sm text-muted-foreground whitespace-pre-wrap pl-6">
                          {deployment.build.metadata.commitMessage}
                        </p>
                      </div>
                    )}

                    {deployment.build.metadata.commitSha && (
                      <div className="px-4 py-3 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <Hash className="h-4 w-4 text-muted-foreground" />
                          <span className="text-sm font-medium">Commit SHA</span>
                        </div>
                        <span className="text-sm text-muted-foreground font-mono">
                          {deployment.build.metadata.commitSha}
                        </span>
                      </div>
                    )}

                    {deployment.build.metadata.branch && (
                      <div className="px-4 py-3 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <GitBranch className="h-4 w-4 text-muted-foreground" />
                          <span className="text-sm font-medium">Branch</span>
                        </div>
                        <span className="text-sm text-muted-foreground font-mono">
                          {deployment.build.metadata.branch}
                        </span>
                      </div>
                    )}

                    {deployment.build.metadata.author && (
                      <div className="px-4 py-3 flex items-center justify-between">
                        <div className="flex items-center gap-2">
                          <User className="h-4 w-4 text-muted-foreground" />
                          <span className="text-sm font-medium">Author</span>
                        </div>
                        <span className="text-sm text-muted-foreground">
                          {deployment.build.metadata.author}
                        </span>
                      </div>
                    )}
                  </div>
                </CollapsibleContent>
              </div>
            </Collapsible>
          )}

          {/* Artifact Hash */}
          <div className="rounded-lg border bg-card">
            <div className="px-4 py-3 flex items-center justify-between">
              <div className="flex items-center gap-2">
                <FileArchive className="h-4 w-4 text-muted-foreground" />
                <span className="text-sm font-medium">Artifact Hash</span>
              </div>
              <span className="text-sm text-muted-foreground font-mono">
                {deployment.artifactHash.slice(0, 16)}...
              </span>
            </div>
          </div>

          {/* Deployment Files */}
          <Collapsible open={showFiles} onOpenChange={setShowFiles}>
            <div className="rounded-lg border bg-card">
              <CollapsibleTrigger className="w-full">
                <div className="px-4 py-3 flex items-center justify-between hover:bg-muted/50 transition-colors">
                  <div className="flex items-center gap-2">
                    <File className="h-4 w-4 text-muted-foreground" />
                    <h2 className="font-semibold">Files</h2>
                    {files && (
                      <Badge variant="secondary" className="text-xs">
                        {files.totalFiles}
                      </Badge>
                    )}
                  </div>
                  {showFiles ? (
                    <ChevronDown className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  )}
                </div>
              </CollapsibleTrigger>

              <CollapsibleContent>
                {isLoadingFiles ? (
                  <div className="flex items-center justify-center py-8">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                ) : files ? (
                  <div className="p-4 space-y-3">
                    {/* Search */}
                    <div className="relative">
                      <Search className="absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                      <Input
                        placeholder="Search files..."
                        value={fileSearch}
                        onChange={(e) => setFileSearch(e.target.value)}
                        className="h-9 pl-8"
                      />
                    </div>

                    {/* File List */}
                    <div className="max-h-96 overflow-y-auto space-y-0.5">
                      {files.files
                        .filter((file) =>
                          fileSearch === "" ||
                          file.path.toLowerCase().includes(fileSearch.toLowerCase())
                        )
                        .map((file) => (
                          <div
                            key={file.path}
                            className="flex items-center justify-between px-2 py-1.5 rounded text-sm hover:bg-muted/50 transition-colors"
                          >
                            <span className="font-mono truncate flex-1 min-w-0">
                              {file.path}
                            </span>
                            <span className="text-xs text-muted-foreground shrink-0 ml-3">
                              {formatBytes(file.sizeBytes)}
                            </span>
                          </div>
                        ))}
                    </div>
                  </div>
                ) : null}
              </CollapsibleContent>
            </div>
          </Collapsible>
        </div>
      </div>
    </div>
  );
}
