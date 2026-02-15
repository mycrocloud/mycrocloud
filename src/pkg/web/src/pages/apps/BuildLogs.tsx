import { useApiClient } from "@/hooks";
import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useRef, useState } from "react";
import { cn } from "@/lib/utils";

interface ILogEntry {
  log: string;
  container_id: string | null;
  container_name: string | null;
  source: string; // stdout | stderr
  tag: string;
  time: string; // timestamp
  uuid: string;
  type: string; // "history" | "live"
}

export default function BuildLogs({
  appId,
  buildId,
}: {
  appId: number;
  buildId: string;
}) {
  const { getAccessTokenSilently } = useAuth0();
  const { get } = useApiClient();

  const [logs, setLogs] = useState<ILogEntry[]>([]);
  const logMapRef = useRef<Map<string, ILogEntry>>(new Map());
  const scrollRef = useRef<HTMLDivElement>(null);
  const [autoScroll, setAutoScroll] = useState(true);

  // Auto-scroll to bottom
  useEffect(() => {
    if (autoScroll && scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [logs, autoScroll]);

  // Detect manual scroll
  const handleScroll = () => {
    if (!scrollRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = scrollRef.current;
    const isAtBottom = scrollHeight - scrollTop - clientHeight < 50;
    setAutoScroll(isAtBottom);
  };

  // Reset state when buildId changes
  useEffect(() => {
    logMapRef.current.clear();
    setLogs([]);
    setAutoScroll(true);
  }, [buildId]);

  const upsertLog = (entry: ILogEntry) => {
    const map = logMapRef.current;
    const existing = map.get(entry.uuid);

    if (!existing) {
      map.set(entry.uuid, entry);
    } else {
      if (entry.type === "history") {
        map.set(entry.uuid, entry);
      } else if (entry.type === "live" && existing.type === "live") {
        map.set(entry.uuid, entry);
      }
    }

    const arr = Array.from(map.values());
    arr.sort((a, b) => {
      const ta = a.time;
      const tb = b.time;

      if (ta && tb) return ta.localeCompare(tb);
      if (ta && !tb) return 1;
      if (!ta && tb) return -1;

      return a.uuid.localeCompare(b.uuid);
    });

    setLogs(arr);
  };

  // Load history
  useEffect(() => {
    let isMounted = true;

    (async () => {
      if (!isMounted) return;

      const historyLogs = await get<ILogEntry[]>(
        `/api/apps/${appId}/spa/builds/${buildId}/logs`
      );

      historyLogs.forEach((entry) => {
        entry.type = "history";
        upsertLog(entry);
      });
    })();

    return () => {
      isMounted = false;
    };
  }, [appId, buildId, get]);

  // SSE live stream
  useEffect(() => {
    let isMounted = true;
    const evtRef = { current: null as EventSource | null };

    (async () => {
      const accessToken = await getAccessTokenSilently();
      if (!isMounted) return;

      const evtSource = new EventSource(
        `/api/apps/${appId}/spa/builds/${buildId}/logs/stream?access_token=${accessToken}`
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
  }, [appId, buildId, getAccessTokenSilently]);

  return (
    <div
      ref={scrollRef}
      onScroll={handleScroll}
      className="h-full overflow-y-auto bg-zinc-950 font-mono text-sm"
    >
      <div className="p-4">
        {logs.length === 0 ? (
          <div className="flex items-center gap-2 text-zinc-500">
            <span className="inline-block h-2 w-2 animate-pulse rounded-full bg-zinc-500" />
            Waiting for logs...
          </div>
        ) : (
          logs.map((log, index) => (
            <div
              key={log.uuid}
              className={cn(
                "group flex py-0.5 hover:bg-zinc-900/50",
                log.source === "stderr" && "bg-red-950/20"
              )}
            >
              <span className="mr-4 select-none text-zinc-600 group-hover:text-zinc-500">
                {String(index + 1).padStart(4, " ")}
              </span>
              <span
                className={cn(
                  "flex-1 whitespace-pre-wrap break-all",
                  log.source === "stderr" ? "text-red-400" : "text-zinc-300"
                )}
              >
                {log.log}
              </span>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
