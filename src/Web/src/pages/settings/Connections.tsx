import { getConfig } from "../../config";

const { GITHUB_CLIENTID } = getConfig();

export default function Connections() {
  const connectGitHub = async () => {
    const redirectUri =
      window.location.origin + "/integrations/callback/github";
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${GITHUB_CLIENTID}&redirect_uri=${redirectUri}&scope=${scope}`;
    window.location.href = authUrl;
  };

  return (
    <section className="mt-4">
      <h1 className="font-bold">Settings</h1>
      <h2>Connections</h2>
      <button
        onClick={connectGitHub}
        className="rounded-sm border bg-gray-900 px-2 py-1.5 text-white"
      >
        Connect GitHub
      </button>
    </section>
  );
}
