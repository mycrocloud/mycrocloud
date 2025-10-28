import { useEffect, useState } from "react";
import { CodeSnippet } from "../components/code-snippet";
import { useAuth0 } from "@auth0/auth0-react";

export default function AppInfo() {
  const { getAccessTokenSilently } = useAuth0();
  const message = JSON.stringify({ ...import.meta.env, __COMMIT_HASH__ }, null, 4);
  const [apiAssemblyInfoJson, setApiAssemblyInfoJson] = useState('apiAssemblyInfoJson');
  useEffect(() => {
    const getApiAssemblyInfo = async () => {
      const accessToken = await getAccessTokenSilently();
      const response = await fetch('/api/_assembly', {
        headers: {
          Authorization: `Bearer ${accessToken}`
        }
      });
      const json = await response.json();
      setApiAssemblyInfoJson(JSON.stringify(json, null, 4));
    };

    getApiAssemblyInfo();
  }, []);
  return (
    <div>
      <h1>App Info</h1>
      <div>
        <CodeSnippet key={"env"} title="env" code={message} />
      </div>
      <div>
        <CodeSnippet key={"ApiAssemblyInfo"} title="ApiAssemblyInfo" code={apiAssemblyInfoJson} />
      </div>
    </div>
  );
}
