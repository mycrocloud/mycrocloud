import Connections from "./Connections";
import Tokens from "./Tokens";

export default function Settings() {
  const slack =
    "https://slack.com/oauth/v2/authorize?client_id=8036520278803.9790206079221&scope=chat:write,commands,channels:read,users:read&redirect_uri=https://mycrocloud.info/integrations/slack/oauth/callback";
  return (
    <div className="">
      <Connections />
      <section>
        <a href={slack}>Connect Slack</a>
      </section>
      <Tokens />
    </div>
  );
}
