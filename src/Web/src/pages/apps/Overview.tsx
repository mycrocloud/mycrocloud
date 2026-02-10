import { useContext, useEffect, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { AppContext } from ".";
import { OnboardingModal } from "./components/OnboardingModal";
import { useApiClient } from "@/hooks";
import {
  Activity,
  ExternalLink,
  Globe,
  Loader2,
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

  const { get } = useApiClient();
  const [stats, setStats] = useState<LogsStatsResponse | null>(null);
  const [loading, setLoading] = useState(true);

  const [searchParams, setSearchParams] = useSearchParams();
  const [onboardingType] = useState<string | null>(searchParams.get("type"));
  const [showOnboarding, setShowOnboarding] = useState(
    searchParams.get("onboard") === "true"
  );

  useEffect(() => {
    if (showOnboarding) {
      searchParams.delete("onboard");
      searchParams.delete("type");
      setSearchParams(searchParams);
    }
  }, [showOnboarding, searchParams, setSearchParams]);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const today = new Date();
        const sevenDaysAgo = new Date(today);
        sevenDaysAgo.setDate(sevenDaysAgo.getDate() - 6);

        const formatDate = (d: Date) => d.toISOString().split("T")[0];

        const data = await get<LogsStatsResponse>(
          `/api/apps/${app.id}/logs/stats?accessDateFrom=${formatDate(sevenDaysAgo)}&accessDateTo=${formatDate(today)}`
        );
        setStats(data);
      } catch (error) {
        console.error("Failed to fetch stats:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchStats();
  }, [app.id, get]);

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

  return (
    <div className="space-y-6 p-4">
      {/* App Info */}
      <Card>
        <CardHeader className="flex flex-row items-start justify-between">
          <div>
            <CardTitle>{app.name}</CardTitle>
            <CardDescription>{app.description || "No description"}</CardDescription>
          </div>
          <Badge
            variant="secondary"
            className={cn(
              app.state === "Active"
                ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                : "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400"
            )}
          >
            {app.state}
          </Badge>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between rounded-lg bg-muted/50 px-3 py-2">
            <div className="flex items-center gap-2">
              <Globe className="h-4 w-4 text-muted-foreground" />
              <a
                href={`https://${app.domain}`}
                target="_blank"
                rel="noopener noreferrer"
                className="text-sm font-medium hover:underline"
              >
                {app.domain}
                <ExternalLink className="ml-1 inline h-3 w-3" />
              </a>
            </div>
            <Button variant="ghost" size="sm" onClick={handleCopyDomain}>
              {copied ? (
                <Check className="h-4 w-4 text-green-500" />
              ) : (
                <Copy className="h-4 w-4" />
              )}
            </Button>
          </div>
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
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

      {/* Traffic */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Traffic</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </div>
          <div className="flex gap-6 pt-2">
            <div>
              <p className="text-2xl font-bold">
                {loading ? (
                  <Loader2 className="h-5 w-5 animate-spin" />
                ) : (
                  totalRequests.toLocaleString()
                )}
              </p>
              <p className="text-xs text-muted-foreground">Last 7 days</p>
            </div>
            <div>
              <p className="text-2xl font-bold">
                {loading ? (
                  <Loader2 className="h-5 w-5 animate-spin" />
                ) : (
                  todayRequests.toLocaleString()
                )}
              </p>
              <p className="text-xs text-muted-foreground">Today</p>
            </div>
          </div>
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

      <OnboardingModal
        open={showOnboarding}
        onOpenChange={setShowOnboarding}
        type={onboardingType}
      />

    </div>
  );
}
