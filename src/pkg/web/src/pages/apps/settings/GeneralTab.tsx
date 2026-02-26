import { useContext, useState } from "react";
import { AppContext } from "..";
import { useForm } from "react-hook-form";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
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
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Pencil,
  Trash2,
  AlertTriangle,
  Loader2,
  Power,
  PowerOff,
  Network,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  RenameFormInput,
  RoutingConfig,
} from "./types";
import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";

export default function GeneralTab() {
  return (
    <>
      <RenameSection />
      <RoutingConfigSection />
      <ChangeStateSection />
      <DeleteSection />
    </>
  );
}

function RenameSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [saving, setSaving] = useState(false);
  const schema = yup.object({ name: yup.string().required("Name is required") });
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RenameFormInput>({
    resolver: yupResolver(schema),
    defaultValues: { name: app.name },
  });
  const onSubmit = async (input: RenameFormInput) => {
    setSaving(true);
    try {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/rename`, {
        method: "PATCH",
        headers: {
          "content-type": "application/json",
          Authorization: `Bearer ${accessToken}`,
          "If-Match": app.version,
        },
        body: JSON.stringify(input),
      });
      if (res.ok) {
        toast("Renamed app");
      } else if (res.status === 409) {
        setError("name", { message: "This app name is already taken" });
      } else {
        toast.error("Something went wrong");
      }
    } catch {
      toast.error("Something went wrong");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Pencil className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">App Name</CardTitle>
        </div>
        <CardDescription>Change the display name of your app</CardDescription>
      </CardHeader>
      <CardContent>
        <form
          onSubmit={handleSubmit(onSubmit)}
          className="flex items-start gap-3"
        >
          <div className="space-y-1">
            <Input
              type="text"
              {...register("name")}
              autoComplete="off"
              className="w-64"
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>
          <Button type="submit" disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Rename
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function RoutingConfigSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [saving, setSaving] = useState(false);
  const [loading, setLoading] = useState(true);
  const [jsonConfig, setJsonConfig] = useState("");
  const [jsonError, setJsonError] = useState("");

  // Load existing config on mount
  useState(() => {
    const loadConfig = async () => {
      try {
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/routing-config`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        });

        if (res.ok) {
          const config = await res.json();
          setJsonConfig(JSON.stringify(config, null, 2));
        } else {
          // Set default config
          const defaultConfig = {
            schemaVersion: "1.0",
            routes: [
              {
                priority: 1,
                match: { type: "prefix", path: "/api" },
                target: { type: "api", stripPrefix: true },
              },
              {
                priority: 2,
                match: { type: "prefix", path: "/" },
                target: { type: "spa", fallback: "/index.html" },
              },
            ],
          };
          setJsonConfig(JSON.stringify(defaultConfig, null, 2));
        }
      } catch (err) {
        toast.error("Failed to load routing configuration");
      } finally {
        setLoading(false);
      }
    };

    loadConfig();
  });

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);

    try {
      // Validate JSON
      let routingConfig: RoutingConfig;
      try {
        routingConfig = JSON.parse(jsonConfig);
        setJsonError("");
      } catch (err) {
        setJsonError("Invalid JSON format");
        setSaving(false);
        return;
      }

      // Validate schema
      if (!routingConfig.schemaVersion || !routingConfig.routes || routingConfig.routes.length === 0) {
        setJsonError("Invalid routing config schema");
        setSaving(false);
        return;
      }

      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/routing-config`, {
        method: "POST",
        headers: {
          "content-type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(routingConfig),
      });

      if (res.ok) {
        toast("Routing configuration updated");
      } else {
        toast.error("Failed to update routing configuration");
      }
    } catch {
      toast.error("Something went wrong");
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Network className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">Routing Configuration</CardTitle>
          </div>
          <CardDescription>
            Configure how requests are routed to your app
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Network className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">Routing Configuration</CardTitle>
        </div>
        <CardDescription>
          Configure how requests are routed to your app
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={onSubmit} className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="json-config">Configuration (JSON)</Label>
            <div className="border rounded-md overflow-hidden">
              <CodeMirror
                value={jsonConfig}
                height="400px"
                extensions={[json()]}
                onChange={(value) => {
                  setJsonConfig(value);
                  setJsonError("");
                }}
                theme="light"
                basicSetup={{
                  lineNumbers: true,
                  highlightActiveLineGutter: true,
                  highlightSpecialChars: true,
                  foldGutter: true,
                  drawSelection: true,
                  dropCursor: true,
                  allowMultipleSelections: true,
                  indentOnInput: true,
                  bracketMatching: true,
                  closeBrackets: true,
                  autocompletion: true,
                  rectangularSelection: true,
                  crosshairCursor: true,
                  highlightActiveLine: true,
                  highlightSelectionMatches: true,
                  closeBracketsKeymap: true,
                  searchKeymap: true,
                  foldKeymap: true,
                  completionKeymap: true,
                  lintKeymap: true,
                }}
              />
            </div>
            {jsonError && (
              <p className="text-sm text-destructive">{jsonError}</p>
            )}
            <p className="text-xs text-muted-foreground">
              Edit the routing configuration in JSON format. See{" "}
              <a
                href="https://docs.example.com/routing-config"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                documentation
              </a>{" "}
              for schema details.
            </p>
          </div>

          <Button type="submit" disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Save Configuration
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function DeleteSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const [showDialog, setShowDialog] = useState(false);
  const [confirmName, setConfirmName] = useState("");
  const [deleting, setDeleting] = useState(false);

  const canDelete = confirmName === app.name;

  const handleDelete = async () => {
    if (!canDelete) return;

    setDeleting(true);
    try {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        toast.success("App deleted successfully");
        navigate("/apps");
      } else {
        toast.error("Failed to delete app");
      }
    } catch {
      toast.error("Something went wrong");
    } finally {
      setDeleting(false);
    }
  };

  const handleOpenChange = (open: boolean) => {
    setShowDialog(open);
    if (!open) {
      setConfirmName("");
    }
  };

  return (
    <>
      <Card className="border-destructive/50">
        <CardHeader>
          <div className="flex items-center gap-2">
            <Trash2 className="h-4 w-4 text-destructive" />
            <CardTitle className="text-base text-destructive">
              Danger Zone
            </CardTitle>
          </div>
          <CardDescription>
            Permanently delete this app and all its data
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              This action cannot be undone. This will permanently delete the app,
              including all routes, builds, logs, and configurations.
            </AlertDescription>
          </Alert>
          <Button variant="destructive" onClick={() => setShowDialog(true)}>
            <Trash2 className="mr-2 h-4 w-4" />
            Delete App
          </Button>
        </CardContent>
      </Card>

      <Dialog open={showDialog} onOpenChange={handleOpenChange}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-destructive">Delete App</DialogTitle>
            <DialogDescription>
              This action is permanent and cannot be undone.
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="confirm-name">
                Type <span className="font-mono font-semibold text-destructive">{app.name}</span> to confirm
              </Label>
              <Input
                id="confirm-name"
                value={confirmName}
                onChange={(e) => setConfirmName(e.target.value)}
                placeholder="Enter app name"
                autoComplete="off"
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => handleOpenChange(false)}
              disabled={deleting}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={handleDelete}
              disabled={!canDelete || deleting}
            >
              {deleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete App
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

function ChangeStateSection() {
  const { app, setApp } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [showDeactivateDialog, setShowDeactivateDialog] = useState(false);
  const [changing, setChanging] = useState(false);

  const handleChangeStatus = async (newStatus: "Active" | "Inactive") => {
    setChanging(true);
    try {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/status?status=${newStatus}`, {
        method: "PATCH",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        setApp({ ...app, state: newStatus });
        toast.success(
          newStatus === "Active"
            ? "App activated successfully"
            : "App deactivated successfully"
        );
        setShowDeactivateDialog(false);
      } else {
        toast.error("Failed to change app status");
      }
    } catch {
      toast.error("Something went wrong");
    } finally {
      setChanging(false);
    }
  };

  const isActive = app.state === "Active";
  const statusConfig = isActive
    ? {
        icon: Power,
        iconClass: "text-green-500",
        badgeClass: "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400",
        description: "Your app is live and receiving traffic",
      }
    : {
        icon: PowerOff,
        iconClass: "text-muted-foreground",
        badgeClass: "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400",
        description: "Your app is offline and not receiving traffic",
      };

  const StatusIcon = statusConfig.icon;

  return (
    <>
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <StatusIcon className={cn("h-4 w-4", statusConfig.iconClass)} />
            <CardTitle className="text-base">App Status</CardTitle>
          </div>
          <CardDescription>{statusConfig.description}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between rounded-lg border p-4">
            <div className="flex items-center gap-3">
              <div
                className={cn(
                  "flex h-10 w-10 items-center justify-center rounded-full",
                  isActive
                    ? "bg-green-100 dark:bg-green-900/30"
                    : "bg-gray-100 dark:bg-gray-800"
                )}
              >
                <StatusIcon
                  className={cn("h-5 w-5", statusConfig.iconClass)}
                />
              </div>
              <div>
                <p className="font-medium">Current Status</p>
                <Badge variant="secondary" className={statusConfig.badgeClass}>
                  {app.state}
                </Badge>
              </div>
            </div>

            <Button
              variant={isActive ? "outline" : "default"}
              disabled={changing}
              onClick={() => {
                if (isActive) {
                  setShowDeactivateDialog(true);
                } else {
                  handleChangeStatus("Active");
                }
              }}
            >
              {changing && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {isActive ? (
                <>
                  <PowerOff className="mr-2 h-4 w-4" />
                  Deactivate
                </>
              ) : (
                <>
                  <Power className="mr-2 h-4 w-4" />
                  Activate
                </>
              )}
            </Button>
          </div>

          {isActive ? (
            <div className="rounded-lg bg-green-50 p-3 text-sm dark:bg-green-900/10">
              <p className="font-medium text-green-800 dark:text-green-400">
                App is receiving traffic
              </p>
              <p className="mt-1 text-green-700 dark:text-green-500">
                All API routes and static pages are accessible to users.
              </p>
            </div>
          ) : (
            <div className="rounded-lg bg-gray-50 p-3 text-sm dark:bg-gray-800/50">
              <p className="font-medium text-gray-800 dark:text-gray-300">
                App is offline
              </p>
              <p className="mt-1 text-gray-600 dark:text-gray-400">
                All requests to your app will return a 503 Service Unavailable error.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={showDeactivateDialog} onOpenChange={setShowDeactivateDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Deactivate App</DialogTitle>
            <DialogDescription>
              Are you sure you want to deactivate this app?
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4 py-4">
            <div className="rounded-lg border bg-muted/50 p-4">
              <p className="text-sm font-medium">What happens when you deactivate:</p>
              <ul className="mt-2 space-y-1 text-sm text-muted-foreground">
                <li>All API routes will return 503 errors</li>
                <li>Static pages will be unavailable</li>
                <li>Builds will still be accessible</li>
                <li>You can reactivate at any time</li>
              </ul>
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeactivateDialog(false)}
              disabled={changing}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => handleChangeStatus("Inactive")}
              disabled={changing}
            >
              {changing && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              <PowerOff className="mr-2 h-4 w-4" />
              Deactivate
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
