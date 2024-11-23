import { useEffect, useState } from "react";
import RegistrationToken from "../../models/RegistrationToken";
import { useAuth0 } from "@auth0/auth0-react";
import TextCopyButton from "../../components/ui/TextCopyButton";

const CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENTID;
const REDIRECT_URI = import.meta.env.VITE_GITHUB_REDIRECT_URI;

function generateUrl(token: string) {
  const baseUrl = import.meta.env.VITE_WEBAPP_APIGATEWAY_DOMAIN;

  // Replace "__app_id__" with "mycrocloud"
  const modifiedUrl = baseUrl.replace("apigw-__app_id__", "apigw");

  // Insert the token into the URL
  const [protocol, rest] = modifiedUrl.split("://");
  return `${protocol}://${token}@${rest}`;
}

export default function Settings() {
  const { getAccessTokenSilently } = useAuth0();
  const onReconnectGitHub = async () => {
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${REDIRECT_URI}&scope=${scope}`;
    window.location.href = authUrl;
  };

  const [tokens, setTokens] = useState<RegistrationToken[]>([]);
  useEffect(() => {
    const fetchTokens = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/runner/registration-tokens`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      if (res.ok) {
        const json = await res.json();
        setTokens(json);
      }
    };
    fetchTokens();
  }, []);

  const handleGenerateClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/runner/registration-tokens`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
    if (res.ok) {
      const token = (await res.json()) as RegistrationToken;
      setTokens([...tokens, token]);
    }
  };

  return (
    <div>
      <h1 className="font-bold">Settings</h1>
      <section className="mt-4">
        <h2>Connections</h2>
        <button
          onClick={onReconnectGitHub}
          className="rounded-sm border bg-gray-900 px-2 py-1.5 text-white"
        >
          Connect GitHub
        </button>
      </section>
      <section className="mt-4">
        <div>
          <h3 className="font-semibold">Runner</h3>
          <h4 className="font-semibold">Registration Tokens</h4>
          <ul>
            {tokens.map((token) => (
              <li key={token.id}>
                <div className="flex">
                  <p>{token.token}</p>
                  <TextCopyButton text={token.token} title="Copy Token" />
                  <TextCopyButton
                    text={generateUrl(token.token)}
                    title="Copy Url"
                  />
                </div>
              </li>
            ))}
          </ul>
          <button
            onClick={handleGenerateClick}
            className="bg-primary px-2 py-1 text-white"
          >
            Generate
          </button>
        </div>
      </section>
    </div>
  );
}
