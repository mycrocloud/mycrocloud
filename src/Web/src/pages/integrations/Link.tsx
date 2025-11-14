import { useContext, useEffect, useState } from "react";
import { IAppIntegration } from "../apps/App";
import { Spinner } from "flowbite-react";
import { useApiClient } from "@/hooks";
import { AppContext } from "../apps";
import { NotFoundError } from "@/errors";
import { getConfig } from "@/config";

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
      <div className="flex items-center gap-2 text-gray-600">
        <Spinner />
        <span>Loading GitHub integrations...</span>
      </div>
    );
  }

  return <div className="mt-2">
    {link ? (
      <div className="flex items-center justify-between p-3 border rounded-lg text-slate-700 text-sm">
        <div>
          Connected to <strong>{link.org}/{link.repo}</strong>
        </div>

        <button
          onClick={onDisconnect}
          className="text-red-600 hover:text-red-700 font-medium underline hover:no-underline"
        >
          Disconnect
        </button>
      </div>

    ) : (
      <div>
        <p className="text-sm text-gray-600">
          Connect your app to GitHub to access your repositories.
        </p>
        <div className="flex items-center gap-3 mt-2">
          <div className="flex flex-col">
            <select
              onChange={(e) => {
                if (e.target.value === "") setInstallationId(null);
                else setInstallationId(Number(e.target.value));
              }}
              value={installationId?.toString() ?? ""}
              className="border rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary"
            >
              {installations.map((inst) => (
                <option key={inst.installationId} value={inst.installationId}>
                  {inst.accountLogin}
                </option>
              ))}
            </select>
          </div>
          <div className="flex flex-col">
            <select
              className="border rounded-lg px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary"
              value={repoId?.toString() ?? ""}
              onChange={(e) => {
                if (e.target.value === "") setRepoId(null);
                else setRepoId(Number(e.target.value));
              }}
            >
              {repos.map((repo) => (
                <option key={repo.id} value={repo.id}>
                  {repo.name}
                </option>
              ))}
            </select>
          </div>

          {installationId && repoId && (
            <button
              className="self-end bg-primary hover:bg-primary/90 text-white px-4 py-2 rounded-lg font-medium transition-colors"
              onClick={onConnect}
            >
              Link
            </button>
          )}
        </div>
        <p className="text-body text-sm text-gray-700 mt-2">
          Missing some repositories?&nbsp;
          <a
            href="#"
            className="font-medium text-fg-brand underline hover:no-underline"
            onClick={connectGitHub}
          >
            Manage GitHub access
          </a>
        </p>
      </div>
    )}
  </div>
}