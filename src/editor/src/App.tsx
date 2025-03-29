import { useEffect, useRef, useState } from "react";
import Editor from "./components/Editor";
import { useAuth0 } from "@auth0/auth0-react";
import IRoute from "./models/route";

function getParams() {
  if (import.meta.env.DEV) {
    return {
      appId: "1",
      routeId: "1",
    };
  }
  const search = window.location.search;
  const params = new URLSearchParams(search);
  return {
    appId: params.get("appId"),
    routeId: params.get("routeId"),
  };
}

function App() {
  // get the route id from query params
  const { appId, routeId } = getParams();

  const { getAccessTokenSilently} = useAuth0();
  const [route, setRoute] = useState<IRoute>();

  const editorRef = useRef<{
    getValue: () => string;
  }>(null);
  useEffect(() => { 
    (async () => {
      const res = await fetch('/api/ping');
      const data = await res.text();
      console.log(data);
     })();
    (async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${appId}/routes/${routeId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      const data = await res.json();
      setRoute(data);
    })();
  }, []);

  const hanldeSaveClick = async () => { 
    const value = editorRef.current!.getValue();
    alert(value);
  }
  if (!route) return <div>Loading...</div>;

  if (!route.functionHandler || !route.responseBodyLanguage) {
    return <div>Route is not configured</div>;
  }

  return (
    <>
      <div>
        <button type="button">Back</button>
        <button type="button" onClick={hanldeSaveClick}>Save</button>
        <p>routeId : {routeId}</p>
      </div>
      <Editor ref={editorRef} value={route.functionHandler} language={route.responseBodyLanguage} />
    </>
  );
}

export default App
