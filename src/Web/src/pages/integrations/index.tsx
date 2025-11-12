import { useAuth0 } from "@auth0/auth0-react";
import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from "../apps";
import { Modal } from "flowbite-react";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { ChevronRightIcon } from "@heroicons/react/24/solid";
import { useAuthRequest } from "@/hooks";

interface GitHubRepo {
  id: number;
  name: string;
  fullName: string;
  description: string;
  createdAt: string;
  updatedAt: string;
}

interface IBuildJob {
  id: string;
  name: string;
  status: string;
  createdAt: string;
  finishedAt: string;
}

type BuildConfig = {
  branch: string;
  directory: string;
  buildCommand: string;
  outDir: string;
};

interface ILogEntry {
  message: string;
  timestamp: string;
  level: string;
}

interface IGitHubInstallation {
  installationId: number;
  accountLogin: string;
  accountType: string;
}

export default function Integrations() {
  const { getAccessTokenSilently } = useAuth0();
  const { get } = useAuthRequest();
  const { app, setApp } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [repoFullName, setRepoFullName] = useState(app.gitHubRepoFullName);
  const [githubRepos, setGitHubRepos] = useState<GitHubRepo[]>([]);
  const [installations, setInstallations] = useState<IGitHubInstallation[]>([]);
  const [installationId, setInstallationId] = useState<number | null>(null);

  useEffect(() => {
    (async () => {
      const installations = await get<IGitHubInstallation[]>(`/api/integrations/github/installations`);
      setInstallations(installations);
    })();
  }, []);

  useEffect(() => {
    if (!installationId) return;
    (async () => {
      const repos = await get<GitHubRepo[]>(`/api/integrations/github/installations/${installationId}/repos`);
      setGitHubRepos(repos);
    })();
  }, [installationId]);

  const onConnectClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      `/api/apps/${app.id}/integrations/github?repoFullName=${repoFullName}`,
      { method: "POST", headers: { Authorization: `Bearer ${accessToken}` } },
    );
    if (res.ok) {
      toast.success("Connected to GitHub");
      setRepoFullName(repoFullName);
      setApp((prev) => ({ ...prev!, gitHubRepoFullName: repoFullName }));
      fetchBuilds();
    }
  };

  const [showDisconnectConfirmModal, setShowDisconnectConfirmModal] =
    useState(false);
  const onDisconnectClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/integrations/github`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      toast.success("Disconnected from GitHub");
      setRepoFullName(undefined);
      setShowDisconnectConfirmModal(false);
      setApp((prev) => ({ ...prev!, gitHubRepoFullName: undefined }));
    }
  };

  const [jobs, setJobs] = useState<IBuildJob[]>([]);

  const fetchBuilds = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/builds`, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      const builds = (await res.json()) as IBuildJob[];
      setJobs(builds);
    }
  };

  const interval = useRef<number | null>(null);
  useEffect(() => {
    fetchBuilds();

    //todo: use websockets instead of polling
    interval.current = window.setInterval(() => {
      //fetchBuilds();
    }, 2000);

    return () => {
      if (interval.current) window.clearInterval(interval.current);
    };
  }, []);

  const [jobId, setJobId] = useState<string>();
  const [logs, setLogs] = useState<ILogEntry[]>([]);
  useEffect(() => {
    if (!jobId) return;

    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/builds/${jobId}/logs`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        const logs = (await res.json()) as ILogEntry[];
        setLogs(logs);
      }
    })();
  }, [jobId]);

  function statusClass(status: string) {
    if (status === "pending") {
      return "text-yellow-500";
    } else if (status === "success" || status === "done") {
      return "text-sky-500";
    } else if (status === "failed") {
      return "text-red-500";
    } else {
      return "text-gray-300";
    }
  }

  const [showBuildConfig, setShowBuildConfig] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<BuildConfig>({
    defaultValues: {
      branch: "default",
      directory: ".",
      buildCommand: "npm run build",
      outDir: "dist",
    },
  });

  useEffect(() => {
    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/builds/config`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });

      if (res.ok) {
        const config = await res.json();
        setValue("branch", config.branch);
        setValue("directory", config.directory);
        setValue("buildCommand", config.buildCommand);
        setValue("outDir", config.outDir);
      }
    })();
  }, []);

  const onSubmitConfig = async (data: BuildConfig) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/builds/config`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(data),
    });
    if (res.ok) {
      toast.success("Build configuration saved");
    }
  };

  return (
    <div className="p-2">
      <h1 className="font-bold">Integrations</h1>

      <section>
        <h2 className="mt-4 font-semibold">GitHub</h2>
        <p className="text-sm text-slate-600">
          Connect your application to a GitHub repository to enable automatic
          builds on code pushes.
        </p>

        <div className="flex">
          <select onChange={(e) => {
            if (e.target.value === "") {
              setInstallationId(null);
            }
            else {
              setInstallationId(Number(e.target.value));
            }
          }} value={installationId?.toString()} className="border px-2 py-1.5 mt-2">
            <option>Select an installation</option>
            {installations.map((inst) => (
              <option key={inst.installationId} value={inst.installationId}>
                {inst.accountLogin}
              </option>
            ))}
          </select>
          <select
            className="border px-2 py-1.5 mt-2 ms-2"
          >
            <option>Select a repository</option>
            {githubRepos.map((repo) => (
              <option key={repo.id} value={repo.id}>
                {repo.name}
              </option>
            ))}
          </select>
        </div>
      </section>

      <div className="mt-4 rounded-sm border p-2">
        {app!.gitHubRepoFullName ? (
          <div>
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
            <h2
              className="mt-4 flex cursor-pointer items-center font-semibold"
              onClick={() => {
                setShowBuildConfig((prev) => !prev);
              }}
            >
              <ChevronRightIcon className="h-4 w-4" />
              Configuration
            </h2>
            {showBuildConfig && (
              <form className="ps-2" onSubmit={handleSubmit(onSubmitConfig)}>
                <div>
                  <label className="block">Branch</label>
                  <input
                    {...register("branch", { required: "Branch is required" })}
                    type="text"
                    placeholder="Branch"
                    className="border px-2 py-1.5"
                    readOnly
                  />
                  {errors.branch && (
                    <span className="text-red-500">
                      {errors.branch.message}
                    </span>
                  )}
                </div>
                <div className="mt-2">
                  <label className="block">Build Directory</label>
                  <p className="text-sm text-slate-600">
                    Path relative to the root of the repository where the build
                    is to be run.
                  </p>
                  <input
                    {...register("directory", {
                      required: "Directory is required",
                    })}
                    type="text"
                    placeholder="/"
                    className="mt-1 border px-2 py-1.5"
                  />
                  {errors.directory && (
                    <span className="text-red-500">
                      {errors.directory.message}
                    </span>
                  )}
                </div>
                <div className="mt-2">
                  <label className="block">Output Directory</label>
                  <p className="text-sm text-slate-600">
                    Path relative to the root of the repository where the build
                    output is located. <br />
                    Default is{" "}
                    <i>
                      <b>dist</b>
                    </i>{" "}
                    for Vite projects and{" "}
                    <i>
                      <b>build</b>
                    </i>{" "}
                    for Create React App projects.
                  </p>
                  <input
                    {...register("outDir")}
                    type="text"
                    className="mt-1 border px-2 py-1.5"
                  />
                  {errors.branch && (
                    <span className="text-red-500">
                      {errors.branch.message}
                    </span>
                  )}
                </div>
                <div className="mt-4">
                  <label className="block">Build Command</label>
                  <input
                    {...register("buildCommand")}
                    type="text"
                    className="border px-2 py-1.5"
                    readOnly
                  />
                  {errors.branch && (
                    <span className="text-red-500">
                      {errors.branch.message}
                    </span>
                  )}
                </div>
                <button
                  type="submit"
                  className="mt-2 bg-primary px-2 py-1 text-white"
                >
                  Save
                </button>
              </form>
            )}
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

      <section>
        <div className="mt-4 flex items-center">
          <h2 className="font-semibold">Builds</h2>
          <button
            className="ms-2 text-sm text-sky-500 hover:underline"
            onClick={() => fetchBuilds()}
          >
            Refresh
          </button>
        </div>
        <div className="flex">
          <div className="">
            <table className="mt-2 table-fixed">
              <thead>
                <tr className="border">
                  <th className="w-80 p-2 text-start">Name</th>
                  <th className="w-20 text-start">Status</th>
                  <th className="w-60 text-start">Started At</th>
                </tr>
              </thead>
              <tbody>
                {jobs.map((build) => (
                  <tr
                    key={build.id}
                    className={
                      "cursor-pointer border hover:bg-slate-100" +
                      (jobId === build.id ? " bg-slate-200" : "")
                    }
                    onClick={() => setJobId(build.id)}
                  >
                    <td className="p-2">{build.name}</td>
                    <td className={statusClass(build.status)}>
                      {build.status}
                    </td>
                    <td>{build.createdAt}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="flex-1 p-2">
            {jobId &&
              (logs.length > 0 ? (
                <>
                  <div className="mt-2 max-h-[400px] overflow-auto bg-black p-4 text-white">
                    {logs.map((log, i) => (
                      <div key={i} className="log-item mb-2">
                        <span className="mr-2 text-xs text-gray-500">
                          {log.timestamp}
                        </span>
                        <span className="font-mono text-sm text-white">
                          {log.message}
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <p>
                  No logs are available. The build may have been executed before
                  the system started logging for this feature.
                </p>
              ))}
          </div>
        </div>
      </section>
    </div>
  );
}
