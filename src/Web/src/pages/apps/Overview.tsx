import { useContext, useEffect, useState } from "react";
import { useAuth0 } from "@auth0/auth0-react";
import { Link } from "react-router-dom";
import { AppContext } from ".";
import {
  Activity,
  Calendar,
  Clock,
  ExternalLink,
  Globe,
  Loader2,
  Route,
  FileText,
  Settings,
  Package,
  Copy,
  Check,
} from "lucide-react";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  ChartConfig,
  ChartContainer,
  ChartTooltip,
  ChartTooltipContent,
} from "@/components/ui/chart";
import { Area, AreaChart, XAxis, YAxis } from "recharts";
import { cn } from "@/lib/utils";

interface LogsStatsResponse {
  totalRequests: number;
  todayRequests: number;
  dailyStats: {
    date: string;
    count: number;
  }[];
}

interface ChartData {
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
  const [stats, setStats] = useState<LogsStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const accessToken = await getAccessTokenSilently();
        const today = new Date();
        const sevenDaysAgo = new Date(today);
        sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 6);

        const formatDate = (d: Date) => d.toISOString().split("T")[0];

        const response = await fetch(
          `/api/apps/${app.id}/logs/stats?accessDateFrom=${formatDate(sevenDaysAgo)}&accessDateTo=${formatDate(today)}`,
          {
            headers: {
              Authorization: `Bearer ${accessToken}`,
            },
          }
        );
        if (response.ok) {
          const data = (await response.json()) as LogsStatsResponse;
          setStats(data);
        }
      } catch (error) {
        console.error("Failed to fetch stats:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, [app.id, getAccessTokenSilently]);

  const chartData: ChartData[] = stats?.dailyStats.map((s) => ({
    date: new Date(s.date).toLocaleDateString("en-US", {
      month: "short",
      day: "numeric",
    }),
    requests: s.count,
  })) || [];

  const totalRequests = stats?.totalRequests || 0;
  const todayRequests = stats?.todayRequests || 0;

  const [copied, setCopied] = useState(false);
  const handleCopyDomain = () => {
    navigator.clipboard.writeText(`https://${app.domain}`);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const quickLinks = [
    { to: "routes", label: "Routes", icon: Route },
    { to: "logs", label: "Logs", icon: FileText },
    { to: "builds", label: "Builds", icon: Package },
    { to: "settings", label: "Settings", icon: Settings },
  ];

  return (
    <div className="space-y-6 p-4">
      {/* Domain Banner */}
      <Card className="bg-muted/30">
        <CardContent className="flex items-center justify-between py-4">
          <div className="flex items-center gap-3">
            <Globe className="h-5 w-5 text-muted-foreground" />
            <div>
              <p className="text-sm text-muted-foreground">Your app is live at</p>
              <a
                href={`https://${app.domain}`}
                target="_blank"
                rel="noopener noreferrer"
                className="font-medium hover:underline"
              >
                {app.domain}
                <ExternalLink className="ml-1 inline h-3 w-3" />
              </a>
            </div>
          </div>
          <Button variant="outline" size="sm" onClick={handleCopyDomain}>
            {copied ? (
              <Check className="mr-2 h-4 w-4 text-green-500" />
            ) : (
              <Copy className="mr-2 h-4 w-4" />
            )}
            {copied ? "Copied" : "Copy"}
          </Button>
        </CardContent>
      </Card>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Status</CardTitle>
            <Activity className={cn(
              "h-4 w-4",
              app.status === "Active" ? "text-green-500" : "text-muted-foreground"
            )} />
          </CardHeader>
          <CardContent>
            <div className="flex items-center gap-2">
              <Badge
                variant="secondary"
                className={cn(
                  app.status === "Active"
                    ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                    : "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400"
                )}
              >
                {app.status}
              </Badge>
            </div>
            <p className="mt-1 text-xs text-muted-foreground">
              {app.status === "Active" ? "Running normally" : "Currently stopped"}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                totalRequests.toLocaleString()
              )}
            </div>
            <p className="text-xs text-muted-foreground">Last 7 days</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Today</CardTitle>
            <Clock className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">
              {loading ? (
                <Loader2 className="h-5 w-5 animate-spin" />
              ) : (
                todayRequests.toLocaleString()
              )}
            </div>
            <p className="text-xs text-muted-foreground">Requests today</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Created</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
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

      {/* Quick Links */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        {quickLinks.map((link) => {
          const Icon = link.icon;
          return (
            <Link
              key={link.to}
              to={link.to}
              className="flex items-center gap-2 rounded-lg border bg-card p-3 text-sm transition-colors hover:bg-muted"
            >
              <Icon className="h-4 w-4 text-muted-foreground" />
              {link.label}
            </Link>
          );
        })}
      </div>

      {/* Request Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Requests Overview</CardTitle>
          <CardDescription>Daily requests for the last 7 days</CardDescription>
        </CardHeader>
        <CardContent>
          {loading ? (
            <div className="flex h-[200px] items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
            </div>
          ) : (
            <ChartContainer config={chartConfig} className="h-[200px] w-full">
              <AreaChart
                data={chartData}
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
        </CardHeader>
        <CardContent>
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
            <div className="space-y-1">
              <dt className="text-muted-foreground">Name</dt>
              <dd className="font-medium">{app.name}</dd>
            </div>
            <div className="space-y-1">
              <dt className="text-muted-foreground">Description</dt>
              <dd>{app.description || "-"}</dd>
            </div>
            <div className="space-y-1">
              <dt className="text-muted-foreground">Created</dt>
              <dd>{new Date(app.createdAt).toLocaleString()}</dd>
            </div>
            <div className="space-y-1">
              <dt className="text-muted-foreground">Last updated</dt>
              <dd>{app.updatedAt ? new Date(app.updatedAt).toLocaleString() : "-"}</dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
