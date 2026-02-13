import { useApiClient } from "@/hooks";
import { useEffect, useMemo, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { Loader2, AlertTriangle } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";

export default function GitHubCallback() {
  const { post } = useApiClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [error, setError] = useState<string | null>(null);

  const state = searchParams.get("state");
  const installation_id = searchParams.get("installation_id");
  const setup_action = searchParams.get("setup_action");

  const parsedState = useMemo(() => {
    if (state) {
      try {
        return JSON.parse(atob(state)) as { from?: string; next?: string };
      } catch { }
    }
    return { from: "/" } as { from?: string; next?: string };
  }, [state]);

  useEffect(() => {
    if (!installation_id || !setup_action || (setup_action !== "install" && setup_action !== "update")) {
      navigate("/");
      return;
    }

    (async () => {
      try {
        await post("/api/integrations/github/callback", {
          installation_id: Number(installation_id),
          setup_action,
        });
        if (parsedState.next) {
          navigate(parsedState.next);
          return;
        }
        navigate(parsedState.from || "/");
      } catch (err) {
        setError(err instanceof Error ? err.message : "Failed to complete GitHub integration");
      }
    })();

  }, [installation_id, setup_action, parsedState, navigate, post]);

  return (
    <div className="flex min-h-[50vh] items-center justify-center">
      {error ? (
        <div className="w-full max-w-md">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertTitle>GitHub Integration Failed</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        </div>
      ) : (
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          <p className="text-sm text-muted-foreground">Connecting GitHub...</p>
        </div>
      )}
    </div>
  );
}
