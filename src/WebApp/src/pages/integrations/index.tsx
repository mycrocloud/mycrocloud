import { useAuth0 } from "@auth0/auth0-react";
import { useContext, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { AppContext } from "../apps";
import { Modal } from "flowbite-react";
import { toast } from "react-toastify";

export interface GitHubRepo {
  name: string;
  fullName: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

export default function Integrations() {
  const { getAccessTokenSilently } = useAuth0();
  const { app, setApp } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [repoFullName, setRepoFullName] = useState(app.gitHubRepoFullName);
  const [githubRepos, setGitHubRepos] = useState<GitHubRepo[]>([]);
  const [githubConnectError, setGitHubConnectError] = useState<string | null>();

  useEffect(() => {
    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/integrations/github/repos`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      if (res.ok) {
        const repos = (await res.json()) as GitHubRepo[];
        setGitHubRepos(repos);
      } else if (res.status === 401) {
        setGitHubConnectError("Unauthorized. Please reconnect GitHub.");
      }
    })();
  }, []);

  const onConnectClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      `/api/apps/${app.id}/integrations/github?repoFullName=${repoFullName}`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      },
    );
    if (res.ok) {
      toast.success("Connected to GitHub");
      setRepoFullName(repoFullName);
      setApp((prev) => ({ ...prev!, gitHubRepoFullName: repoFullName }));
    }
  };

  const [showDisconnectConfirmModal, setShowDisconnectConfirmModal] =
    useState(false);
  const onDisconnectClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/integrations/github`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
    if (res.ok) {
      toast.success("Disconnected from GitHub");
      setRepoFullName(undefined);
      setShowDisconnectConfirmModal(false);
      setApp((prev) => ({ ...prev!, gitHubRepoFullName: undefined }));
    }
  };

  return (
    <div className="p-2">
      <h1 className="font-bold">Integrations</h1>
      <div className="mt-4 rounded-sm border p-2">
        {githubConnectError && (
          <div className="flex">
            <p className="text-red-500">{githubConnectError}</p>
            <Link
              to={"/settings"}
              className="ms-2 text-blue-500 hover:underline"
            >
              Go to Settings
            </Link>
          </div>
        )}
        {app!.gitHubRepoFullName ? (
          <div className="flex items-center p-2">
            <p>
              Connected to{" "}
              <span className="font-bold">{app.gitHubRepoFullName}</span>
            </p>
            <button
              onClick={() => setShowDisconnectConfirmModal(true)}
              className="ms-2 rounded border px-2 py-1.5"
            >
              Disconnect
            </button>
          </div>
        ) : (
          <div className="mt-2 flex">
            <select
              value={repoFullName || ""}
              onChange={(e) => {
                setRepoFullName(e.target.value);
              }}
              className="border px-2 py-1.5"
            >
              <option>Select a repository</option>
              {githubRepos.map((repo) => (
                <option key={repo.fullName} value={repo.fullName}>
                  {repo.fullName}
                </option>
              ))}
            </select>
            <button
              onClick={onConnectClick}
              className="ms-2 bg-primary px-2 text-white disabled:bg-slate-500 disabled:text-slate-200"
              disabled={!repoFullName}
            >
              Connect
            </button>
          </div>
        )}
      </div>
      <Modal
        show={showDisconnectConfirmModal}
        onClose={() => setShowDisconnectConfirmModal(false)}
      >
        <Modal.Header>Do you want to disconnect the integration?</Modal.Header>
        <Modal.Body>
          <div>
            This will disconnect the project integration with GitHub. Are you
            sure you want to proceed?
          </div>
        </Modal.Body>
        <Modal.Footer className="justify-end">
          <button
            onClick={() => {
              setShowDisconnectConfirmModal(false);
            }}
            className="rounded-sm border px-3 py-1.5"
          >
            Cancel
          </button>
          <button
            className="bg-red-600 px-3 py-1.5 text-white"
            onClick={onDisconnectClick}
          >
            Disconnect
          </button>
        </Modal.Footer>
      </Modal>
    </div>
  );
}
