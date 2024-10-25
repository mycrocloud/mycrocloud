import { useAuth0 } from "@auth0/auth0-react";
import { useEffect, useState } from "react";
const CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENTID;
const REDIRECT_URI = import.meta.env.VITE_GITHUB_REDIRECT_URI;

export default function Settings() {
  const { getAccessTokenSilently } = useAuth0();
  const [githubConnected, setGitHubConnected] = useState(false);
  const onClickConnectGitHub = async () => {
    if (!githubConnected) {
      const authUrl = `https://github.com/login/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${REDIRECT_URI}&scope=repo`;
      window.location.href = authUrl;
    } else {
      //todo: disconnect
    }
  };

  const onReconnectGitHub = async () => {
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${REDIRECT_URI}&scope=${scope}`;
    window.location.href = authUrl;
  };

  useEffect(() => {
    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/me`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      if (res.ok) {
        const { connections } = await res.json();
        setGitHubConnected(
          connections.find((c: any) => c.provider === "GitHub"),
        );
      }
    })();
  }, []);

  return (
    <div>
      <h1 className="font-bold">Settings</h1>
      <section className="mt-4">
        <h2>Connections</h2>
        <button
          onClick={onClickConnectGitHub}
          className="rounded-sm border bg-gray-900 px-2 py-1.5 text-white"
        >
          {githubConnected ? "Disconnect" : "Connect"} GitHub
        </button>
        <button
          onClick={onReconnectGitHub}
          className="rounded-sm border bg-gray-900 px-2 py-1.5 text-white"
        >
          Reconnect GitHub
        </button>
      </section>
    </div>
  );
}
