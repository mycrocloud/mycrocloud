import { useContext, useEffect, useState, useMemo } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { AppContext } from ".";
import { getAppDomain } from "./service";
import { IRouteLog } from "../routes";
import {
  PlayCircleIcon,
  StopCircleIcon,
  GlobeAltIcon,
  CalendarIcon,
  ChartBarIcon,
  ClockIcon,
} from "@heroicons/react/24/solid";
import TextCopyButton from "../../components/ui/TextCopyButton";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
} from "@/components/ui/chart";
import { Area, AreaChart, XAxis, YAxis } from "recharts";

interface DailyStats {
  date: string;
  requests: number;
}

const chartConfig = {
  requests: {
    label: "Requests",
    color: "hsl(var(--chart-1))",
  },
} satisfies ChartConfig;

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { getAccessTokenSilently } = useAuth0();
  const [logs, setLogs] = useState<IRouteLog[]>([]);
  const [loading, setLoading] = useState(true);
  const domain = getAppDomain(app.name);

  useEffect(() => {
    const fetchLogs = async () => {
      try {
        const accessToken = await getAccessTokenSilently();
        const today = new Date();
        const sevenDaysAgo = new Date(today);
        sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 6);

        const formatDate = (d: Date) => d.toISOString().split("T")[0];

        const response = await fetch(
          `/api/apps/${app.id}/logs?accessDateFrom=${formatDate(sevenDaysAgo)}&accessDateTo=${formatDate(today)}`,
          {
            headers: {
              Authorization: `Bearer ${accessToken}`,
            },
          }
        );
        const data = (await response.json()) as IRouteLog[];
        setLogs(data);
      } catch (error) {
        console.error("Failed to fetch logs:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchLogs();
  }, [app.id, getAccessTokenSilently]);

  const dailyStats = useMemo(() => {
    const stats: Record<string, number> = {};

    // Initialize last 7 days with 0
    for (let i = 6; i >= 0; i--) {
      const date = new Date();
      date.setDate(date.getDate() - i);
      const dateStr = date.toISOString().split("T")[0];
      stats[dateStr] = 0;
    }

    // Count requests per day
    logs.forEach((log) => {
      const dateStr = new Date(log.timestamp).toISOString().split("T")[0];
      if (stats[dateStr] !== undefined) {
        stats[dateStr]++;
      }
    });

    // Convert to array and format dates
    return Object.entries(stats).map(([date, requests]) => ({
      date: new Date(date).toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
      }),
      requests,
    })) as DailyStats[];
  }, [logs]);

  const totalRequests = logs.length;
  const todayRequests = useMemo(() => {
    const today = new Date().toISOString().split("T")[0];
    return logs.filter(
      (log) => new Date(log.timestamp).toISOString().split("T")[0] === today
    ).length;
  }, [logs]);

  return (
    <div className="space-y-6 p-4">
      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Status</CardTitle>
            {app.status === "Active" ? (
              <PlayCircleIcon className="h-4 w-4 text-green-500" />
            ) : (
              <StopCircleIcon className="h-4 w-4 text-red-500" />
            )}
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{app.status}</div>
            <p className="text-xs text-muted-foreground">
              {app.status === "Active" ? "Running normally" : "Currently stopped"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
            <ChartBarIcon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? "..." : totalRequests.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">Last 7 days</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Today</CardTitle>
            <ClockIcon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? "..." : todayRequests.toLocaleString()}
            </div>
            <p className="text-xs text-muted-foreground">Requests today</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Created</CardTitle>
            <CalendarIcon className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {new Date(app.createdAt).toLocaleDateString("en-US", {
                month: "short",
                day: "numeric",
              })}
            </div>
            <p className="text-xs text-muted-foreground">
              {new Date(app.createdAt).getFullYear()}
            </p>
          </CardContent>
        </Card>
      </div>

      {/* Request Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Requests Overview</CardTitle>
          <CardDescription>Daily requests for the last 7 days</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex h-[200px] items-center justify-center text-muted-foreground">
              Loading...
            </div>
          ) : (
            <ChartContainer config={chartConfig} className="h-[200px] w-full">
              <AreaChart
                data={dailyStats}
                margin={{ top: 10, right: 30, left: 0, bottom: 0 }}
              >
                <defs>
                  <linearGradient id="fillRequests" x1="0" y1="0" x2="0" y2="1">
                    <stop
                      offset="5%"
                      stopColor="var(--color-requests)"
                      stopOpacity={0.8}
                    />
                    <stop
                      offset="95%"
                      stopColor="var(--color-requests)"
                      stopOpacity={0.1}
                    />
                  </linearGradient>
                </defs>
                <XAxis
                  dataKey="date"
                  tickLine={false}
                  axisLine={false}
                  tickMargin={8}
                />
                <YAxis tickLine={false} axisLine={false} tickMargin={8} />
                <ChartTooltip
                  cursor={false}
                  content={<ChartTooltipContent indicator="line" />}
                />
                <Area
                  type="monotone"
                  dataKey="requests"
                  stroke="var(--color-requests)"
                  fill="url(#fillRequests)"
                  strokeWidth={2}
                />
              </AreaChart>
            </ChartContainer>
          )}
        </CardContent>
      </Card>

      {/* App Details */}
      <Card>
        <CardHeader>
          <CardTitle>App Details</CardTitle>
          <CardDescription>Configuration and information</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-[auto_1fr] gap-x-8 gap-y-3 text-sm">
            <span className="text-muted-foreground">Name</span>
            <span className="font-medium">{app.name}</span>

            <span className="text-muted-foreground">Description</span>
            <span>{app.description || "-"}</span>

            <span className="text-muted-foreground">Version</span>
            <span className="font-mono text-xs">{app.version}</span>

            <span className="text-muted-foreground">Created at</span>
            <span>{new Date(app.createdAt).toLocaleString()}</span>

            <span className="text-muted-foreground">Updated at</span>
            <span>
              {app.updatedAt ? new Date(app.updatedAt).toLocaleString() : "-"}
            </span>

            <span className="text-muted-foreground">Domain</span>
            <span className="flex items-center gap-2">
              <GlobeAltIcon className="h-4 w-4 text-muted-foreground" />
              <a
                href={`https://${domain}`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary hover:underline"
              >
                {domain}
              </a>
              <TextCopyButton text={domain} />
            </span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
