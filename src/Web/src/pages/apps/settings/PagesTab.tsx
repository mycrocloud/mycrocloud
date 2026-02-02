import { useContext, useEffect, useState } from "react";
import { AppContext } from "..";
import { useForm } from "react-hook-form";
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
import { NotFoundError } from "@/errors";
import { IAppIntegration } from "../App";
import { IGitHubInstallation, GitHubRepo, IBuildConfig } from "./types";

const { GITHUB_APP_NAME } = getConfig();

export default function PagesTab() {
  return (
    <>
      <GitHubLinkSection />
      <BuildSettingsSection />
    </>
  );
}

function GitHubLinkSection() {
  const { get, post, del } = useApiClient();
  const { app } = useContext(AppContext)!;
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
      const [link, installations] = await Promise.all([
        get<IAppIntegration>(`/api/apps/${app.id}/link`).catch((err) =>
          err instanceof NotFoundError ? null : Promise.reject(err)
        ),
        get<IGitHubInstallation[]>(`/api/integrations/github/installations`),
      ]);

      setLink(link);
      setInstallations(installations);

      if (installations.length === 1) {
        setInstallationId(installations[0].installationId);
      }

      setLoading(false);
    })();
  }, [app.id, get]);

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

  const connectGitHub = async () => {
    const githubAppUrl = `https://github.com/apps/${GITHUB_APP_NAME}/installations/new?state=foo`;
    window.location.href = githubAppUrl;
  };

  const onConnect = async () => {
    if (!installationId || !repoId) return;

    await post(`/api/apps/${app.id}/link/github`, { installationId, repoId });

    setLink({
      type: "GitHub",
      org: installations.find((i) => i.installationId === installationId)!
        .accountLogin,
      repoId: repoId,
      repo: repos.find((r) => r.id === repoId)!.name,
    });
  };

  const onDisconnect = async () => {
    setDisconnecting(true);
    try {
      await del(`/api/apps/${app.id}/link`);
      setLink(null);
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
        ) : (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Connect your app to GitHub to access your repositories.
            </p>
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

function BuildSettingsSection() {
  const { get, post } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [isEditing, setIsEditing] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
  } = useForm<IBuildConfig>();

  useEffect(() => {
    (async () => {
      try {
        const data = await get<IBuildConfig>(`/api/apps/${app.id}/builds/config`);
        reset(data);
      } finally {
        setLoading(false);
      }
    })();
  }, [app.id, get, reset]);

  const onSubmit = async (data: IBuildConfig) => {
    setSaving(true);
    try {
      await post(`/api/apps/${app.id}/builds/config`, data);
      toast.success("Build settings saved");
      setIsEditing(false);
    } catch {
      toast.error("Failed to save build settings");
    } finally {
      setSaving(false);
    }
  };

  const handleCancel = () => {
    reset();
    setIsEditing(false);
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
        {!isEditing && (
          <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>
            <Pencil className="mr-2 h-4 w-4" />
            Edit
          </Button>
        )}
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="branch">Branch</Label>
              <Input
                id="branch"
                {...register("branch")}
                disabled={!isEditing}
                placeholder="main"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="directory">Root Directory</Label>
              <Input
                id="directory"
                {...register("directory")}
                disabled={!isEditing}
                placeholder="."
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="buildCommand">Build Command</Label>
              <Input
                id="buildCommand"
                {...register("buildCommand")}
                disabled={!isEditing}
                placeholder="npm run build"
                className="font-mono text-sm"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="outDir">Output Directory</Label>
              <Input
                id="outDir"
                {...register("outDir")}
                disabled={!isEditing}
                placeholder="dist"
              />
            </div>
          </div>

          {isEditing && (
            <div className="flex gap-2">
              <Button type="submit" disabled={saving}>
                {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Save
              </Button>
              <Button type="button" variant="outline" onClick={handleCancel} disabled={saving}>
                Cancel
              </Button>
            </div>
          )}
        </form>
      </CardContent>
    </Card>
  );
}
