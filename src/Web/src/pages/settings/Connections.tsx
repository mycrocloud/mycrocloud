import { getConfig } from "@/config";

const { SLACK_CLIENT_ID } = getConfig();

export default function Connections() {
  const connectSlack = async () => {
    const redirect_uri = window.location.origin + "/integrations/slack/oauth/callback";
    const scope = "chat:write,commands,channels:read,users:read";
    const url = `https://slack.com/oauth/v2/authorize?client_id=${SLACK_CLIENT_ID}&scope=${scope}&redirect_uri=${redirect_uri}`;

    window.location.href = url;
  };

  return (
    <section className="space-y-4 rounded-lg border border-slate-200 bg-white p-4">
      <header>
        <h2 className="text-base font-semibold">Connections</h2>
        {/* <p className="text-sm text-slate-500">
          Connect external services to your application
        </p> */}
      </header>

      <div className="flex items-center gap-3">
        <button
          onClick={connectSlack}
        >
          <img alt="Add to Slack" height="40" width="139" src="https://platform.slack-edge.com/img/add_to_slack.png" srcSet="https://platform.slack-edge.com/img/add_to_slack.png 1x, https://platform.slack-edge.com/img/add_to_slack@2x.png 2x" />
        </button>
      </div>
    </section>
  );
}
