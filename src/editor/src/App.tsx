import { useEffect, useState } from "react";
import Editor from "./components/Editor";

const parentOrigin = import.meta.env.VITE_PARENT_ORIGIN;

function App() {
  const [value, setValue] = useState<string>("");
  const [language, setLanguage] = useState<string>("");
  const [loading, setLoading] = useState<boolean>(true);

  useEffect(() => {
    const onMessage = (event: MessageEvent) => {
      if (event.origin !== parentOrigin) return;
    
      const { type, payload } = event.data;

      if (type === "load") {
        const { value, language } = payload;
        setValue(value);
        setLanguage(language);
        setLoading(false);
      }
    };

    window.addEventListener("message", onMessage);

    window.parent.postMessage({ type: "loaded" }, parentOrigin);

    return () => {
      window.removeEventListener("message", onMessage);
    };
  }, []);

  const handleChange = (newValue: string) => {
    window.parent.postMessage({ type: "change", payload: newValue }, parentOrigin);
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  return <Editor value={value} language={language} onChange={handleChange} />;
}

export default App;
