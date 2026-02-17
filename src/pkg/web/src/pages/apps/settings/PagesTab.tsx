import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from "..";
import { useForm, Controller } from "react-hook-form";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { toast } from "react-toastify";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Pencil,
  Loader2,
  GitBranch,
  Github,
  Link2,
  Unlink,
} from "lucide-react";
import { useApiClient } from "@/hooks";
import { getConfig } from "@/config";
import { IAppIntegration } from "../App";
import {
  IGitHubInstallation,
  GitHubRepo,
  IBuildConfig,
  NODE_VERSIONS,
} from "./types";

const { GITHUB_APP_NAME } = getConfig();

export default function PagesTab() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [autoOpenBuildSettings, setAutoOpenBuildSettings] = useState(false);

  return (
    <>
      <GitHubLinkSection onConnected={() => setAutoOpenBuildSettings(true)} />
      {app.gitIntegration && (
        <BuildSettingsSection
          autoOpen={autoOpenBuildSettings}
          onAutoOpenHandled={() => setAutoOpenBuildSettings(false)}
        />
      )}
    </>
  );
}

function GitHubLinkSection({ onConnected }: { onConnected: () => void }) {
  const { get, post, del } = useApiClient();
  const { app, setApp } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [link, setLink] = useState<IAppIntegration | null>();
  const [installations, setInstallations] = useState<IGitHubInstallation[]>([]);
  const [repos, setRepos] = useState<GitHubRepo[]>([]);
  const [installationId, setInstallationId] = useState<number | null>(null);
  const [repoId, setRepoId] = useState<number | null>(null);
  const [showDisconnectDialog, setShowDisconnectDialog] = useState(false);
  const [disconnecting, setDisconnecting] = useState(false);

  useEffect(() => {
    (async () => {
      const installations = await get<IGitHubInstallation[]>(`/api/integrations/github/installations`);

      setLink(app.gitIntegration || null);
      setInstallations(installations);

      if (installations.length === 1) {
        setInstallationId(installations[0].installationId);
      }

      setLoading(false);
    })();
  }, [app.id, app.gitIntegration, get]);

  useEffect(() => {
    if (!installationId) return;

    (async () => {
      const repos = await get<GitHubRepo[]>(
        `/api/integrations/github/installations/${installationId}/repos`
      );
      setRepos(repos);

      if (repos.length === 1) {
        setRepoId(repos[0].id);
      }
    })();
  }, [installationId, get]);

  const refreshInstallations = async () => {
    const installations = await get<IGitHubInstallation[]>(
      `/api/integrations/github/installations`
    );
    setInstallations(installations);
    if (installations.length === 1) {
      setInstallationId(installations[0].installationId);
    }
  };

  const connectGitHub = () => {
    const state = btoa(JSON.stringify({
      from: window.location.pathname,
      next: "/integrations/github/installed",
    }));
    const githubAppUrl = `https://github.com/apps/${GITHUB_APP_NAME}/installations/new?state=${state}`;
    const width = 800;
    const height = 700;
    const left = window.screenX + (window.outerWidth - width) / 2;
    const top = window.screenY + (window.outerHeight - height) / 2;
    const popup = window.open(githubAppUrl, "github-install", `width=${width},height=${height},left=${left},top=${top}`);
    const timer = setInterval(() => {
      if (popup?.closed) {
        clearInterval(timer);
        refreshInstallations();
      }
    }, 500);
  };

  const onConnect = async () => {
    if (!installationId || !repoId) return;

    await post(`/api/apps/${app.id}/link/github`, { installationId, repoId });

    const newLink = {
      type: "GitHub",
      org: installations.find((i) => i.installationId === installationId)!
        .accountLogin,
      repoId: repoId,
      repo: repos.find((r) => r.id === repoId)!.name,
    };

    setLink(newLink);
    // Update app context with new gitIntegration
    setApp({ ...app, gitIntegration: newLink });
    onConnected();
  };

  const onDisconnect = async () => {
    setDisconnecting(true);
    try {
      await del(`/api/apps/${app.id}/link`);
      setLink(null);
      // Update app context to remove gitIntegration
      setApp({ ...app, gitIntegration: null });
      setShowDisconnectDialog(false);
    } finally {
      setDisconnecting(false);
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Github className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">GitHub Integration</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading GitHub integrations...
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Github className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">GitHub Integration</CardTitle>
        </div>
        <CardDescription>
          Connect your app to a GitHub repository for automatic deployments
        </CardDescription>
      </CardHeader>
      <CardContent>
        {link ? (
          <>
            <div className="flex items-center justify-between rounded-lg border bg-muted/30 p-4">
              <div className="flex items-center gap-3">
                <Link2 className="h-5 w-5 text-green-500" />
                <div>
                  <a
                    href={`https://github.com/${link.org}/${link.repo}`}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-sm font-medium hover:underline"
                  >
                    {link.org}/{link.repo}
                  </a>
                  <p className="text-xs text-muted-foreground">
                    Connected repository
                  </p>
                </div>
              </div>
              <Button variant="ghost" size="sm" onClick={() => setShowDisconnectDialog(true)}>
                <Unlink className="mr-2 h-4 w-4" />
                Disconnect
              </Button>
            </div>

            <Dialog open={showDisconnectDialog} onOpenChange={setShowDisconnectDialog}>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Disconnect Repository</DialogTitle>
                  <DialogDescription>
                    Are you sure you want to disconnect this repository?
                  </DialogDescription>
                </DialogHeader>

                <div className="py-4">
                  <div className="flex items-center gap-3 rounded-lg border p-3">
                    <Github className="h-5 w-5" />
                    <div>
                      <p className="text-sm font-medium">{link.org}/{link.repo}</p>
                      <p className="text-xs text-muted-foreground">This repository will be unlinked</p>
                    </div>
                  </div>
                  <p className="mt-3 text-sm text-muted-foreground">
                    Automatic deployments will stop. You can reconnect at any time.
                  </p>
                </div>

                <DialogFooter>
                  <Button
                    variant="outline"
                    onClick={() => setShowDisconnectDialog(false)}
                    disabled={disconnecting}
                  >
                    Cancel
                  </Button>
                  <Button
                    variant="destructive"
                    onClick={onDisconnect}
                    disabled={disconnecting}
                  >
                    {disconnecting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                    <Unlink className="mr-2 h-4 w-4" />
                    Disconnect
                  </Button>
                </DialogFooter>
              </DialogContent>
            </Dialog>
          </>
        ) : installations.length === 0 ? (
          <div className="flex flex-col items-center gap-4 rounded-lg border border-dashed p-8 text-center">
            <Github className="h-10 w-10 text-muted-foreground" />
            <div className="space-y-1">
              <p className="text-sm font-medium">No GitHub App installed</p>
              <p className="text-sm text-muted-foreground">
                Install the GitHub App on your account or organization to connect repositories.
              </p>
            </div>
            <Button onClick={connectGitHub}>
              <Github className="mr-2 h-4 w-4" />
              Install GitHub App
            </Button>
          </div>
        ) : (
          <div className="space-y-4">
            <div className="flex flex-wrap items-center gap-3">
              <Select
                value={installationId?.toString() ?? ""}
                onValueChange={(value) => {
                  if (value === "") setInstallationId(null);
                  else setInstallationId(Number(value));
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select organization" />
                </SelectTrigger>
                <SelectContent>
                  {installations.map((inst) => (
                    <SelectItem
                      key={inst.installationId}
                      value={inst.installationId.toString()}
                    >
                      {inst.accountLogin}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Select
                value={repoId?.toString() ?? ""}
                onValueChange={(value) => {
                  if (value === "") setRepoId(null);
                  else setRepoId(Number(value));
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select repository" />
                </SelectTrigger>
                <SelectContent>
                  {repos.map((repo) => (
                    <SelectItem key={repo.id} value={repo.id.toString()}>
                      {repo.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              {installationId && repoId && (
                <Button onClick={onConnect}>
                  <Link2 className="mr-2 h-4 w-4" />
                  Link
                </Button>
              )}
            </div>
            <p className="text-sm text-muted-foreground">
              Missing some repositories?{" "}
              <a
                href="#"
                className="font-medium text-primary underline hover:no-underline"
                onClick={connectGitHub}
              >
                Manage GitHub access
              </a>
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

const buildConfigSchema = yup.object({
  branch: yup.string().required("Branch is required"),
  directory: yup.string().default(""),
  installCommand: yup.string().required("Install command is required"),
  buildCommand: yup.string().required("Build command is required"),
  outDir: yup.string().required("Output directory is required"),
  nodeVersion: yup.string().default("20"),
});

const FRAMEWORK_PRESETS: Record<string, IBuildConfig> = {
  vite: {
    branch: "main",
    directory: "",
    installCommand: "npm install",
    buildCommand: "npm run build",
    outDir: "dist",
    nodeVersion: "22",
  },
};

function BuildSettingsSection({
  autoOpen,
  onAutoOpenHandled,
}: {
  autoOpen: boolean;
  onAutoOpenHandled: () => void;
}) {
  const { get, post } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [buildConfig, setBuildConfig] = useState<IBuildConfig | null>(null);
  const [showReadyHint, setShowReadyHint] = useState(false);
  const originalData = useRef<IBuildConfig | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    control,
    setValue,
    clearErrors,
    formState: { errors },
  } = useForm<IBuildConfig>({
    resolver: yupResolver(buildConfigSchema),
  });

  useEffect(() => {
    (async () => {
      try {
        const data = await get<IBuildConfig | null>(`/api/apps/${app.id}/spa/builds/config`);
        const normalizedData: IBuildConfig = {
          branch: data?.branch ?? "main",
          directory: data?.directory ?? "",
          installCommand: data?.installCommand ?? "npm install",
          buildCommand: data?.buildCommand ?? "npm run build",
          outDir: data?.outDir ?? "dist",
          nodeVersion: data?.nodeVersion ?? "20",
        };
        originalData.current = normalizedData;
        setBuildConfig(normalizedData);
        reset(normalizedData);
      } finally {
        setLoading(false);
      }
    })();
  }, [app.id, get, reset]);

  useEffect(() => {
    if (autoOpen) {
      setIsModalOpen(true);
      onAutoOpenHandled();
    }
  }, [autoOpen, onAutoOpenHandled]);

  const onSubmit = async (data: IBuildConfig) => {
    setSaving(true);
    try {
      await post(`/api/apps/${app.id}/spa/builds/config`, data);
      originalData.current = data;
      setBuildConfig(data);
      toast.success("Build settings saved");
      setShowReadyHint(true);
      setIsModalOpen(false);
    } catch (err) {
      console.error("Failed to save build settings:", err);
      toast.error("Failed to save build settings");
    } finally {
      setSaving(false);
    }
  };

  const onInvalid = () => {
    toast.error("Please fix validation errors before saving");
  };

  const handleCancel = () => {
    if (originalData.current) {
      reset(originalData.current);
    }
    setIsModalOpen(false);
  };

  const applyPreset = (presetKey: keyof typeof FRAMEWORK_PRESETS) => {
    const preset = FRAMEWORK_PRESETS[presetKey];

    setValue("branch", preset.branch);
    setValue("directory", preset.directory);
    setValue("installCommand", preset.installCommand);
    setValue("buildCommand", preset.buildCommand);
    setValue("outDir", preset.outDir);
    setValue("nodeVersion", preset.nodeVersion);
    clearErrors();
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <GitBranch className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">Build Settings</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading...
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <div className="flex items-center gap-2">
              <GitBranch className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-base">Build Settings</CardTitle>
            </div>
            <CardDescription>
              Configure how your application is built
            </CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={() => setIsModalOpen(true)}>
            <Pencil className="mr-2 h-4 w-4" />
            Edit
          </Button>
        </CardHeader>
        <CardContent>
          {showReadyHint && (
            <div className="mb-4 flex items-center justify-between rounded-md border border-green-200 bg-green-50 px-3 py-2 text-sm text-green-800">
              <span>Ready to create your first deployment.</span>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-6 px-2 text-green-800 hover:bg-green-100"
                onClick={() => setShowReadyHint(false)}
              >
                Dismiss
              </Button>
            </div>
          )}
          {buildConfig && (
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Branch</p>
                <p className="text-sm font-medium">{buildConfig.branch}</p>
              </div>
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Root Directory</p>
                <p className="text-sm font-medium">{buildConfig.directory}</p>
              </div>
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Install Command</p>
                <p className="text-sm font-mono">{buildConfig.installCommand}</p>
              </div>
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Build Command</p>
                <p className="text-sm font-mono">{buildConfig.buildCommand}</p>
              </div>
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Output Directory</p>
                <p className="text-sm font-medium">{buildConfig.outDir}</p>
              </div>
              <div className="space-y-1">
                <p className="text-xs text-muted-foreground">Node.js Version</p>
                <p className="text-sm font-medium">Node.js {buildConfig.nodeVersion}</p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={isModalOpen} onOpenChange={setIsModalOpen}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Build Settings</DialogTitle>
            <DialogDescription>
              Configure build settings for deployments from your connected repository.
            </DialogDescription>
          </DialogHeader>
          <form
            id="build-settings-form"
            onSubmit={handleSubmit(onSubmit, onInvalid)}
            className="space-y-4"
          >
            <div className="flex items-center gap-2 rounded-md border bg-muted/30 p-2">
              <span className="text-xs text-muted-foreground">Quick Fill:</span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => applyPreset("vite")}
              >
                Vite
              </Button>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="branch">Branch</Label>
                <Input
                  id="branch"
                  {...register("branch")}
                  placeholder="main"
                />
                {errors.branch && (
                  <p className="text-sm text-destructive">{errors.branch.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="directory">Root Directory</Label>
                <Input
                  id="directory"
                  {...register("directory")}
                  placeholder="."
                />
                {errors.directory && (
                  <p className="text-sm text-destructive">{errors.directory.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="installCommand">Install Command</Label>
                <Input
                  id="installCommand"
                  {...register("installCommand")}
                  placeholder="npm ci"
                  className="font-mono text-sm"
                />
                {errors.installCommand && (
                  <p className="text-sm text-destructive">{errors.installCommand.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="buildCommand">Build Command</Label>
                <Input
                  id="buildCommand"
                  {...register("buildCommand")}
                  placeholder="npm run build"
                  className="font-mono text-sm"
                />
                {errors.buildCommand && (
                  <p className="text-sm text-destructive">{errors.buildCommand.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="outDir">Output Directory</Label>
                <Input
                  id="outDir"
                  {...register("outDir")}
                  placeholder="dist"
                />
                {errors.outDir && (
                  <p className="text-sm text-destructive">{errors.outDir.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="nodeVersion">Node.js Version</Label>
                <Controller
                  name="nodeVersion"
                  control={control}
                  render={({ field }) => (
                    <Select
                      value={field.value || "20"}
                      onValueChange={field.onChange}
                    >
                      <SelectTrigger>
                        <SelectValue placeholder="Select Node.js version" />
                      </SelectTrigger>
                      <SelectContent>
                        {NODE_VERSIONS.map((version) => (
                          <SelectItem key={version} value={version}>
                            Node.js {version}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={handleCancel} disabled={saving}>
                Cancel
              </Button>
              <Button type="submit" form="build-settings-form" disabled={saving}>
                {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Save
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}
