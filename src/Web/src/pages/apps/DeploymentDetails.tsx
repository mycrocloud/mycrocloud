import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { Link, useParams, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  CheckCircle2,
  Loader2,
  ArrowLeft,
  Copy,
  Check,
  Package,
  FileArchive,
  GitBranch,
  ExternalLink,
  RotateCcw,
  Clock,
  ChevronDown,
  ChevronRight,
  File,
  Search,
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

export default function DeploymentDetails() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post } = useApiClient();
  const { deploymentId } = useParams();
  const navigate = useNavigate();

  const [deployment, setDeployment] = useState<IDeployment | null>(null);
  const [files, setFiles] = useState<IDeploymentFiles | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingFiles, setIsLoadingFiles] = useState(false);
  const [showFiles, setShowFiles] = useState(false);
  const [fileSearch, setFileSearch] = useState("");
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

  const fetchFiles = useCallback(async () => {
    if (!showFiles || files) return;
    
    setIsLoadingFiles(true);
    try {
      const data = await get<IDeploymentFiles>(
        `/api/apps/${app.id}/deployments/${deploymentId}/files`
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
          {deployment.isActive && (
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
        {deployment.isActive && (
          <Badge className="bg-green-600 hover:bg-green-700">
            Active
          </Badge>
        )}
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
                {deployment.isActive ? (
                  <Badge className="bg-green-600 hover:bg-green-700">
                    Active
                  </Badge>
                ) : (
                  <Badge variant="secondary">
                    Inactive
                  </Badge>
                )}
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

          {/* Deployment Files */}
          <Collapsible open={showFiles} onOpenChange={setShowFiles}>
            <div className="rounded-lg border bg-card">
              <CollapsibleTrigger className="w-full">
                <div className="border-b px-4 py-3 flex items-center justify-between hover:bg-muted/50 transition-colors">
                  <div className="flex items-center gap-2">
                    <h2 className="font-semibold">Deployment Files</h2>
                    {files && (
                      <Badge variant="secondary" className="text-xs">
                        {files.totalFiles} files
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
                    {/* Summary */}
                    <div className="flex items-center justify-between text-sm text-muted-foreground pb-3 border-b">
                      <span>{files.totalFiles} files</span>
                      <span>{formatBytes(files.totalSize)}</span>
                    </div>

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
                    <div className="max-h-96 overflow-y-auto space-y-1">
                      {files.files
                        .filter((file) => 
                          fileSearch === "" || 
                          file.path.toLowerCase().includes(fileSearch.toLowerCase())
                        )
                        .map((file) => (
                          <div
                            key={file.path}
                            className="flex items-center justify-between px-3 py-2 rounded-md hover:bg-muted/50 transition-colors"
                          >
                            <div className="flex items-center gap-2 flex-1 min-w-0">
                              <File className="h-4 w-4 text-muted-foreground shrink-0" />
                              <span className="text-sm font-mono truncate">
                                {file.path}
                              </span>
                            </div>
                            <span className="text-xs text-muted-foreground shrink-0 ml-2">
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
