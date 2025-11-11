import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

export default function GitHubCallback() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const state = searchParams.get("state");
  const installation_id = searchParams.get("installation_id");
  const setup_action = searchParams.get("setup_action");

  useEffect(() => {
    if (!installation_id || !setup_action || (setup_action !== "install" && setup_action !== "update")) {
      navigate("/");
      return;
    }

    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch("/api/integrations/github/callback", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ installation_id, setup_action }),
      });
      if (res.ok) {
        let pathName = "/";
        if (state) {
          try {
            pathName = JSON.parse(decodeURIComponent(state)).pathname;
          } catch (error) {}
        }
        navigate(pathName);
      }
    })();
    
  }, [installation_id, setup_action, state]);

  return <h1>Loading...</h1>;
}
