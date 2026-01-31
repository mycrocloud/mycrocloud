import { useApiClient } from "@/hooks";
import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useRef, useState } from "react";

interface ILogEntry {
    log: string;
    container_id: string | null;
    container_name: string | null;
    source: string; // stdout | stderr
    tag: string;
    time: string;   // timestamp
    uuid: string;
    type: string;   // "history" | "live"
}

export default function BuildLogs({ appId, buildId }: { appId: number; buildId: string }) {
    const { getAccessTokenSilently } = useAuth0();
    const { get } = useApiClient();

    const [logs, setLogs] = useState<ILogEntry[]>([]);
    const logMapRef = useRef<Map<string, ILogEntry>>(new Map());

    // -------------------------
    // MERGE LOG ENTRY
    // -------------------------
    const upsertLog = (entry: ILogEntry) => {
        const map = logMapRef.current;
        const existing = map.get(entry.uuid);

        if (!existing) {
            // hoàn toàn mới
            map.set(entry.uuid, entry);
        } else {
            // Đã có -> xử lý theo type
            if (entry.type === "history") {
                // HISTORY luôn override live
                map.set(entry.uuid, entry);
            } else if (entry.type === "live" && existing.type === "live") {
                // live override live
                map.set(entry.uuid, entry);
            }
            // live KHÔNG override history
        }

        // convert map → array & sort
        const arr = Array.from(map.values());
        arr.sort((a, b) => {
            const ta = a.time;
            const tb = b.time;

            if (ta && tb) return ta.localeCompare(tb);
            if (ta && !tb) return 1;
            if (!ta && tb) return -1;

            // fallback ổn định
            return a.uuid.localeCompare(b.uuid);
        });

        setLogs(arr);
    };

    // -------------------------
    // LOAD HISTORY FIRST
    // -------------------------
    useEffect(() => {
        let isMounted = true;

        (async () => {
            if (!isMounted) return;

            const historyLogs = await get<ILogEntry[]>(`/api/apps/${appId}/builds/${buildId}/logs`);

            historyLogs.forEach((entry) => {
                entry.type = "history";
                upsertLog(entry);
            });
        })();

        return () => {
            isMounted = false;
        };
    }, [appId, buildId]);

    // -------------------------
    // SSE LIVE STREAM
    // -------------------------
    useEffect(() => {
        let isMounted = true;
        const evtRef = { current: null as EventSource | null };

        (async () => {
            const accessToken = await getAccessTokenSilently();
            if (!isMounted) return;

            const evtSource = new EventSource(
                `/api/apps/${appId}/builds/${buildId}/logs/stream?access_token=${accessToken}`
            );

            evtRef.current = evtSource;

            evtSource.onmessage = (event) => {
                if (!isMounted) return;

                try {
                    const parsed: ILogEntry = JSON.parse(event.data);
                    parsed.type = "live";

                    upsertLog(parsed);
                } catch (e) {
                    console.error("Invalid SSE log:", event.data);
                    console.error(e);
                }
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
    }, [appId, buildId]);

    // -------------------------
    // RENDER
    // -------------------------
    return (
        <div className="bg-black p-2">
            {logs.map((log) => (
                <div key={log.uuid} className="mb-1">
                    <span
                        className={
                            log.source === "stderr"
                                ? "text-red-400 text-sm"
                                : "text-gray-200 text-sm"
                        }
                    >
                        {log.log}
                    </span>
                </div>
            ))}
        </div>
    );
}