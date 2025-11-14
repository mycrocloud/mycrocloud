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
    <section className="mt-4">
      <h2 className="mt-4 font-semibold">Connections</h2>
      <div className="flex space-x-2 items-center mt-2">
        <button
          onClick={connectSlack}
        >
          <img alt="Add to Slack" height="40" width="139" src="https://platform.slack-edge.com/img/add_to_slack.png" srcSet="https://platform.slack-edge.com/img/add_to_slack.png 1x, https://platform.slack-edge.com/img/add_to_slack@2x.png 2x" />
        </button>
      </div>
    </section>
  );
}
