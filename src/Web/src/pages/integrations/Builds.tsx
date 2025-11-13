import { useApiClient } from "@/hooks";
import { useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";

interface IBuildJob {
    id: string;
    name: string;
    status: string;
    createdAt: string;
    finishedAt: string;
}

interface ILogEntry {
    message: string;
    timestamp: string;
    level: string;
}

export default function Builds() {
    const { app } = useContext(AppContext)!;
    if (!app) throw new Error();

    const { get } = useApiClient();
    const { getAccessTokenSilently} = useAuth0();
    
    const [buildId, setBuildId] = useState<string>();
    const [logs, setLogs] = useState<ILogEntry[]>([]);

    useEffect(() => {
        let evtSource: EventSource;
        
        if (!buildId) return;

        (async () => {
            const accessToken = await getAccessTokenSilently();

            evtSource = new EventSource(`/api/apps/${app.id}/builds/${buildId}/logs/stream?access_token=${accessToken}`);
            evtSource.onmessage = function (event) { 
                console.log("Log event:", event.data);
            }

        })();

        return () => {
            if (evtSource) {
                evtSource.close();
            }
        };

    }, [buildId]);

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

    const [jobs, setJobs] = useState<IBuildJob[]>([]);
    const fetchBuilds = async () => {
        const builds = await get<IBuildJob[]>(`/api/apps/${app.id}/builds`);
        setJobs(builds);
    };

    return <section>
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
                                    (buildId === build.id ? " bg-slate-200" : "")
                                }
                                onClick={() => setBuildId(build.id)}
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
                {buildId &&
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
}