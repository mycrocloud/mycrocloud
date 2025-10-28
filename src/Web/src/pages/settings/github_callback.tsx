import { useAuth0 } from "@auth0/auth0-react";
import { useEffect } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";

export default function GitHubCallback() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const code = searchParams.get("code");
  const state = searchParams.get("state");

  useEffect(() => {
    if (!code) {
      navigate("/");
    }
    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch("/api/integrations/github/callback", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ code }),
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
  }, [code]);

  return <h1>Loading...</h1>;
}
