import { useContext, useEffect, useState } from "react";
import { IAppIntegration } from "../App";
import { Button, Select, Spinner } from "flowbite-react";
import { useApiClient } from "@/hooks";
import { AppContext } from "..";
import { NotFoundError } from "@/errors";
import { getConfig } from "@/config";
import { ArrowTopRightOnSquareIcon } from "@heroicons/react/24/outline";

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

export default function GitRepo() {
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
      <div className="flex items-center justify-center p-6">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-slate-900">Git Repository</h3>
        <p className="mt-1 text-sm text-slate-500">
          Connect your app to a GitHub repository for automatic deployments
        </p>
      </div>

      {link ? (
        <div className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <svg className="h-5 w-5 text-slate-600" fill="currentColor" viewBox="0 0 24 24">
                <path fillRule="evenodd" d="M12 2C6.477 2 2 6.484 2 12.017c0 4.425 2.865 8.18 6.839 9.504.5.092.682-.217.682-.483 0-.237-.008-.868-.013-1.703-2.782.605-3.369-1.343-3.369-1.343-.454-1.158-1.11-1.466-1.11-1.466-.908-.62.069-.608.069-.608 1.003.07 1.531 1.032 1.531 1.032.892 1.53 2.341 1.088 2.91.832.092-.647.35-1.088.636-1.338-2.22-.253-4.555-1.113-4.555-4.951 0-1.093.39-1.988 1.029-2.688-.103-.253-.446-1.272.098-2.65 0 0 .84-.27 2.75 1.026A9.564 9.564 0 0112 6.844c.85.004 1.705.115 2.504.337 1.909-1.296 2.747-1.027 2.747-1.027.546 1.379.202 2.398.1 2.651.64.7 1.028 1.595 1.028 2.688 0 3.848-2.339 4.695-4.566 4.943.359.309.678.92.678 1.855 0 1.338-.012 2.419-.012 2.747 0 .268.18.58.688.482A10.019 10.019 0 0022 12.017C22 6.484 17.522 2 12 2z" clipRule="evenodd" />
              </svg>
              <div className="flex flex-col">
                <span className="text-xs text-slate-500">Connected to</span>
                <a
                  href={`https://github.com/${link.org}/${link.repo}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 font-semibold text-slate-900 hover:text-blue-600 transition-colors"
                >
                  {link.org}/{link.repo}
                  <ArrowTopRightOnSquareIcon className="h-3.5 w-3.5" />
                </a>
              </div>
            </div>

            <Button
              color="light"
              size="sm"
              onClick={onDisconnect}
              className="text-red-600 hover:bg-red-50"
            >
              Disconnect
            </Button>
          </div>
        </div>
      ) : (
        <div className="space-y-4 rounded-lg border border-slate-200 bg-slate-50 p-4">
          <div className="flex flex-col gap-3">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label className="mb-1.5 block text-xs font-medium text-slate-700">
                  Organization
                </label>
                <Select
                  onChange={(e) => {
                    if (e.target.value === "") setInstallationId(null);
                    else setInstallationId(Number(e.target.value));
                  }}
                  value={installationId?.toString() ?? ""}
                  sizing="sm"
                >
                  <option value="">Select organization</option>
                  {installations.map((inst) => (
                    <option key={inst.installationId} value={inst.installationId}>
                      {inst.accountLogin}
                    </option>
                  ))}
                </Select>
              </div>

              <div>
                <label className="mb-1.5 block text-xs font-medium text-slate-700">
                  Repository
                </label>
                <Select
                  value={repoId?.toString() ?? ""}
                  onChange={(e) => {
                    if (e.target.value === "") setRepoId(null);
                    else setRepoId(Number(e.target.value));
                  }}
                  sizing="sm"
                  disabled={!installationId || repos.length === 0}
                >
                  <option value="">Select repository</option>
                  {repos.map((repo) => (
                    <option key={repo.id} value={repo.id}>
                      {repo.name}
                    </option>
                  ))}
                </Select>
              </div>
            </div>

            {installationId && repoId && (
              <Button
                onClick={onConnect}
                size="sm"
                className="w-full sm:w-auto"
              >
                Connect Repository
              </Button>
            )}
          </div>

          <div className="border-t border-slate-200 pt-3">
            <p className="text-xs text-slate-600">
              Missing some repositories?{" "}
              <button
                type="button"
                className="font-medium text-blue-600 hover:text-blue-700 hover:underline"
                onClick={connectGitHub}
              >
                Manage GitHub access
              </button>
            </p>
          </div>
        </div>
      )}
    </div>
  )
}