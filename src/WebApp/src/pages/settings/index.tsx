const CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENTID;

export default function Settings() {
  const onReconnectGitHub = async () => {
    const redirectUri =
      window.location.origin + "/integrations/callback/github";
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${redirectUri}&scope=${scope}`;
    window.location.href = authUrl;
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
    </div>
  );
}
