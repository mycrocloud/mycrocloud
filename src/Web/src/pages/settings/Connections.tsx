import { getConfig } from "@/config";

const { GITHUB_CLIENTID, SLACK_CLIENT_ID } = getConfig();

export default function Connections() {
  const connectGitHub = async () => {
    const redirectUri =
      window.location.origin + "/integrations/callback/github";
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${GITHUB_CLIENTID}&redirect_uri=${redirectUri}&scope=${scope}`;
    window.location.href = authUrl;
  };

  const connectSlack = async () => {
    const redirect_uri = window.location.origin + "/integrations/slack/oauth/callback";
    const scope = "chat:write,commands,channels:read,users:read";
    const url =`https://slack.com/oauth/v2/authorize?client_id=${SLACK_CLIENT_ID}&scope=${scope}&redirect_uri=${redirect_uri}`;

    window.location.href = url;
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

      <button
        onClick={connectSlack}
        className="rounded-sm border bg-gray-900 px-2 py-1.5 text-white"
      >
        Connect Slack
      </button>
    </section>
  );
}
