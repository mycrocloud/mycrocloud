import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from ".";
import { Link, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  Loader2,
  ArrowLeft,
  GitBranch,
  ChevronDown,
  ChevronRight,
  Lock,
  FileJson,
  Download,
  ExternalLink,
} from "lucide-react";
import { useAuth0 } from "@auth0/auth0-react";

interface IRoute {
  id: number;
  name: string;
  method: string;
  path: string;
  description?: string;
  responseType: string;
  responseStatusCode?: number;
  requireAuthorization: boolean;
}

interface IApiDeployment {
  id: string;
  name: string;
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
  const { getAccessTokenSilently } = useAuth0();
  const { deploymentId } = useParams();

  const [deployment, setDeployment] = useState<IApiDeployment | null>(null);
  const [routes, setRoutes] = useState<IRoute[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingRoutes, setIsLoadingRoutes] = useState(false);
  const [showRoutes, setShowRoutes] = useState(false);
  const [isDownloadingOpenApi, setIsDownloadingOpenApi] = useState(false);
  const isReady = deployment?.status === "Ready";

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

  const fetchRoutes = useCallback(async () => {
    if (!showRoutes || routes.length > 0) return;
    
    setIsLoadingRoutes(true);
    try {
      const data = await get<IRoute[]>(
        `/api/apps/${app.id}/api/deployments/${deploymentId}/routes`
      );
      setRoutes(data);
    } catch (error) {
      console.error("Failed to fetch routes:", error);
    } finally {
      setIsLoadingRoutes(false);
    }
  }, [app.id, deploymentId, get, showRoutes, routes.length]);

  useEffect(() => {
    fetchDeployment();
  }, [fetchDeployment]);

  useEffect(() => {
    if (showRoutes) {
      fetchRoutes();
    }
  }, [showRoutes, fetchRoutes]);

  const handleDownloadOpenApi = async () => {
    setIsDownloadingOpenApi(true);
    try {
      const token = await getAccessTokenSilently();
      const response = await fetch(
        `/api/apps/${app.id}/api/deployments/${deploymentId}/openapi.json`,
        {
          headers: {
            Authorization: `Bearer ${token}`,
          },
        }
      );

      if (!response.ok) {
        throw new Error("Failed to download OpenAPI specification");
      }

      const blob = await response.blob();
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = `${app.name.toLowerCase().replace(/\s+/g, '-')}-openapi.json`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);
    } catch (error) {
      console.error("Failed to download OpenAPI spec:", error);
    } finally {
      setIsDownloadingOpenApi(false);
    }
  };

  const handleViewOpenApi = async () => {
    try {
      const spec = await get<any>(
        `/api/apps/${app.id}/api/deployments/${deploymentId}/openapi.json`
      );

      const html = `
        <!DOCTYPE html>
        <html>
          <head>
            <title>OpenAPI - ${app.name}</title>
            <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css" />
          </head>
          <body>
            <div id="swagger-ui"></div>
            <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
            <script>
              SwaggerUIBundle({
                spec: ${JSON.stringify(spec)},
                dom_id: '#swagger-ui',
                presets: [
                  SwaggerUIBundle.presets.apis,
                  SwaggerUIBundle.SwaggerUIStandalonePreset
                ],
              });
            </script>
          </body>
        </html>
      `;
      
      const blob = new Blob([html], { type: 'text/html' });
      const url = URL.createObjectURL(blob);
      window.open(url, '_blank');
      
      // Clean up the blob URL after a short delay
      setTimeout(() => URL.revokeObjectURL(url), 100);
    } catch (error) {
      console.error("Failed to view OpenAPI spec:", error);
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
          <h1 className="text-2xl font-bold">
            {deployment.name}
          </h1>
          <p className="text-sm text-muted-foreground mt-1">
            <span className="font-mono">{deployment.id.slice(0, 12)}</span> · {formatTimestamp(deployment.createdAt)}
          </p>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-auto p-4">
        <div className="mx-auto max-w-4xl space-y-6">
          {/* OpenAPI Specification */}
          <div className="rounded-lg border bg-card p-4">
            <div className="flex items-center justify-between mb-3">
              <div className="flex items-center gap-2">
                <FileJson className="h-4 w-4 text-muted-foreground" />
                <h2 className="font-semibold">OpenAPI Specification</h2>
              </div>
            </div>
            <p className="text-sm text-muted-foreground mb-4">
              View or download the OpenAPI specification for this deployment
            </p>
            {!isReady && (
              <p className="text-xs text-muted-foreground mb-4">
                OpenAPI is available after deployment status is Ready.
              </p>
            )}
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={handleViewOpenApi}
                disabled={!isReady}
              >
                <ExternalLink className="mr-2 h-4 w-4" />
                View in Swagger UI
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={handleDownloadOpenApi}
                disabled={isDownloadingOpenApi || !isReady}
              >
                {isDownloadingOpenApi ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Download className="mr-2 h-4 w-4" />
                )}
                Download JSON
              </Button>
            </div>
          </div>

          {/* Routes List */}
          <Collapsible open={showRoutes} onOpenChange={setShowRoutes}>
            <div className="rounded-lg border bg-card">
              <CollapsibleTrigger className="w-full">
                <div className="px-4 py-3 flex items-center justify-between hover:bg-muted/50 transition-colors">
                  <div className="flex items-center gap-2">
                    <GitBranch className="h-4 w-4 text-muted-foreground" />
                    <h2 className="font-semibold">Routes</h2>
                    <Badge variant="secondary" className="text-xs">
                      {deployment.routeCount}
                    </Badge>
                  </div>
                  {showRoutes ? (
                    <ChevronDown className="h-4 w-4 text-muted-foreground" />
                  ) : (
                    <ChevronRight className="h-4 w-4 text-muted-foreground" />
                  )}
                </div>
              </CollapsibleTrigger>
              
              <CollapsibleContent>
                {isLoadingRoutes ? (
                  <div className="px-4 py-8 flex items-center justify-center">
                    <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
                  </div>
                ) : routes.length === 0 ? (
                  <div className="px-4 py-8 text-center text-sm text-muted-foreground">
                    No routes found in this deployment
                  </div>
                ) : (
                  <div className="divide-y">
                    {routes.map((route) => {
                      const methodColors: Record<string, string> = {
                        GET: "bg-blue-600 hover:bg-blue-700",
                        POST: "bg-green-600 hover:bg-green-700",
                        PUT: "bg-orange-600 hover:bg-orange-700",
                        PATCH: "bg-yellow-600 hover:bg-yellow-700",
                        DELETE: "bg-red-600 hover:bg-red-700",
                      };
                      
                      return (
                        <div key={route.id} className="px-4 py-3 hover:bg-muted/30 transition-colors">
                          <div className="flex items-start gap-3">
                            <Badge className={`${methodColors[route.method] || 'bg-gray-600'} text-white font-mono text-xs min-w-[60px] justify-center`}>
                              {route.method}
                            </Badge>
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2 mb-1">
                                <code className="text-sm font-mono font-medium">{route.path}</code>
                                {route.requireAuthorization && (
                                  <span title="Requires Authorization">
                                    <Lock className="h-3 w-3 text-muted-foreground" />
                                  </span>
                                )}
                              </div>
                              {route.name && (
                                <p className="text-sm text-muted-foreground mb-1">{route.name}</p>
                              )}
                              {route.description && (
                                <p className="text-xs text-muted-foreground">{route.description}</p>
                              )}
                              <div className="flex items-center gap-2 mt-2">
                                <Badge variant="outline" className="text-xs">
                                  {route.responseType}
                                </Badge>
                              </div>
                            </div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </CollapsibleContent>
            </div>
          </Collapsible>
        </div>
      </div>
    </div>
  );
}
