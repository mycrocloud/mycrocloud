import { useContext, useEffect, useState } from "react";
import { IAppIntegration } from "../apps/App";
import { useApiClient } from "@/hooks";
import { AppContext } from "../apps";
import { NotFoundError } from "@/errors";
import { getConfig } from "@/config";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Loader2 } from "lucide-react";

const { GITHUB_APP_NAME } = getConfig();

interface IGitHubInstallation {
  installationId: number;
  accountLogin: string;
  accountType: string;
}

interface GitHubRepo {
  id: number;
  name: string;
  fullName: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export default function Link() {
  const { get, post, del } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);

  const [link, setLink] = useState<IAppIntegration | null>();
  const [installations, setInstallations] = useState<IGitHubInstallation[]>([]);
  const [repos, setRepos] = useState<GitHubRepo[]>([]);

  const [installationId, setInstallationId] = useState<number | null>(null);
  const [repoId, setRepoId] = useState<number | null>(null);

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

      //TODO: re-thinking about UX/UI
      if (installations.length === 1) {
        setInstallationId(installations[0].installationId); // 👈 auto-select org
      }

      setLoading(false);
    })()
  }, []);

  useEffect(() => {
    if (!installationId) return;

    (async () => {
      const repos = await get<GitHubRepo[]>(`/api/integrations/github/installations/${installationId}/repos`);
      setRepos(repos);

      if (repos.length === 1) {
        setRepoId(repos[0].id)
      }
    })();
  }, [installationId]);

  const connectGitHub = async () => {
    const githubAppUrl = `https://github.com/apps/${GITHUB_APP_NAME}/installations/new?state=foo`; //TODO: add state param?
    window.location.href = githubAppUrl;
  };

  const onConnect = async () => {
    if (!installationId || !repoId) return;

    await post(`/api/apps/${app.id}/link/github`, { installationId, repoId });

    setLink({
      type: "GitHub",
      org: installations.find(i => i.installationId === installationId)!.accountLogin,
      repoId: repoId,
      repo: repos.find(r => r.id == repoId)!.name
    });
  };

  const onDisconnect = async () => {
    if (!confirm("Are you sure you want to disconnect this repository?")) return;

    await del(`/api/apps/${app.id}/link`);

    setLink(null);
  }

  if (loading) {
    return (
      <div className="flex items-center gap-2 text-muted-foreground">
        <Loader2 className="h-4 w-4 animate-spin" />
        <span>Loading GitHub integrations...</span>
      </div>
    );
  }

  return <div className="mt-2">
    {link ? (
      <div className="flex items-center justify-between p-3 border rounded-lg text-sm">
        <div>
          Connected to <strong>{link.org}/{link.repo}</strong>
        </div>
        <Button variant="link" onClick={onDisconnect} className="text-destructive p-0 h-auto">
          Disconnect
        </Button>
      </div>
    ) : (
      <div>
        <p className="text-sm text-muted-foreground">
          Connect your app to GitHub to access your repositories.
        </p>
        <div className="flex items-center gap-3 mt-2">
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
                <SelectItem key={inst.installationId} value={inst.installationId.toString()}>
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
              Link
            </Button>
          )}
        </div>
        <p className="text-sm text-muted-foreground mt-2">
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
  </div>
}