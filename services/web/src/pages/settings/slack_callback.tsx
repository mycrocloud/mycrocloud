import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { useApiClient } from "@/hooks";
import { useAuth0 } from "@auth0/auth0-react";


export default function SlackCallback() {
  const navigate = useNavigate();
  const {isAuthenticated, isLoading} = useAuth0();
  const { post } = useApiClient();

  const [searchParams] = useSearchParams();
  const code = searchParams.get("code");
  const state = searchParams.get("state");

  useEffect(() => {
    if (isLoading || !isAuthenticated) {
      return;
    }

    if (!code) {
      navigate("/");
      return;
    }

    (async () => {
      const redirect_uri = window.location.origin + "/integrations/slack/oauth/callback";
      await post("/api/integrations/slack/callback", { code, redirect_uri });
      let pathName = "/";
      if (state) {
        try {
          pathName = JSON.parse(decodeURIComponent(state)).pathname;
        } catch { /* ignore parse errors */ }
      }
      navigate(pathName);
    })();
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isLoading, isAuthenticated, code]);

  return <h1>Loading...</h1>;
}
