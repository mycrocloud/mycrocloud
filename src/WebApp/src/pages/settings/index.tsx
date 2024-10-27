const CLIENT_ID = import.meta.env.VITE_GITHUB_CLIENTID;
const REDIRECT_URI = import.meta.env.VITE_GITHUB_REDIRECT_URI;

export default function Settings() {
  const onReconnectGitHub = async () => {
    //repo and read:org scope
    const scope = "repo%20read:org";
    const authUrl = `https://github.com/login/oauth/authorize?client_id=${CLIENT_ID}&redirect_uri=${REDIRECT_URI}&scope=${scope}`;
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
