import { useEffect, useState } from "react";
import Editor from "./components/Editor";

const parentOrigin = import.meta.env.VITE_PARENT_ORIGIN;

function isValidMessage(event: MessageEvent, editorId: string ) { 
  return event.origin === parentOrigin && event.data.editorId === editorId;
}

function App() {
  const editorId = new URLSearchParams(window.location.search).get("id") || "editor";

  const [value, setValue] = useState<string>("");
  const [language, setLanguage] = useState<string>("");
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const onMessage = (event: MessageEvent) => {
      if (!isValidMessage(event, editorId)) return;
    
      const { type, payload } = event.data;
     
      if (type === "load") {
        const { value, language } = payload;
        setValue(value);
        setLanguage(language);
        setLoading(false);
      }
    };

    window.addEventListener("message", onMessage);

    window.parent.postMessage({ editorId, type: "loaded" }, parentOrigin);

    return () => {
      window.removeEventListener("message", onMessage);
    };
  }, []);

  const handleChange = (newValue: string) => {
    window.parent.postMessage({ editorId, type: "change", payload: newValue }, parentOrigin);
  };

  if (loading) {
    return null;
  }

  return <Editor value={value} language={language} onChange={handleChange} />;
}

export default App;
