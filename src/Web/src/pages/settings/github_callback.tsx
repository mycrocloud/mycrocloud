import { useApiClient } from "@/hooks";
import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useMemo } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

export default function GitHubCallback() {
  const {isAuthenticated, isLoading} = useAuth0();
  const { post } = useApiClient();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const state = searchParams.get("state");
  const installation_id = searchParams.get("installation_id");
  const setup_action = searchParams.get("setup_action");

  const navigatePath = useMemo(() => {
    let pathName = "/";
    if (state) {
      try {
        pathName = JSON.parse(decodeURIComponent(state)).pathname;
      } catch (error) { }
    }

    return pathName;
  }, [state]);

  useEffect(() => {
    if (isLoading || !isAuthenticated) {
      return;
    }

    if (!installation_id || !setup_action || (setup_action !== "install" && setup_action !== "update")) {
      navigate("/");
      return;
    }

    (async () => {
      await post("/api/integrations/github/callback", { installation_id, setup_action });
      navigate(navigatePath);
    })();

  }, [isLoading, isAuthenticated, installation_id, setup_action, navigatePath]);

  return <h1>Loading...</h1>;
}
