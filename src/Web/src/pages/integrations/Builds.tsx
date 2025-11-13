import { useApiClient } from "@/hooks";
import { useCallback, useContext, useEffect, useState } from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";

interface IBuild {
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
    const { getAccessTokenSilently } = useAuth0();

    const [builds, setBuilds] = useState<IBuild[]>([]);

    const fetchBuilds = useCallback(async () => {
        const data = await get<IBuild[]>(`/api/apps/${app.id}/builds`);
        setBuilds(data);
    }, [app.id]);

    // SSE subscription
    useEffect(() => {
        let isMounted = true;
        const evtRef = { current: null as EventSource | null };

        (async () => {
            const accessToken = await getAccessTokenSilently();
            if (!isMounted) return;

            const evtSource = new EventSource(
                `/api/apps/${app.id}/builds/subscription?access_token=${accessToken}`
            );
            evtRef.current = evtSource;

            // Initial load
            fetchBuilds();

            evtSource.onmessage = () => {
                if (!isMounted) return;
                fetchBuilds();
            };

            evtSource.onerror = (error) => {
                console.error("SSE error:", error);
            };
        })();

        return () => {
            isMounted = false;

            if (evtRef.current) {
                evtRef.current.close();
                evtRef.current = null;
            }
        };
    }, [app.id, fetchBuilds]);

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

    return <section>
        <div className="mt-4 flex items-center">
            <h2 className="font-semibold">Builds</h2>
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
                        {builds.map((build) => (
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