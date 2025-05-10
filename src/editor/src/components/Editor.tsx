import { useRef, useEffect } from "react";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import styles from "./Editor.module.css";

const Editor = ({
  value,
  language,
  onChange
}: {
  value: string;
  language: string;
  onChange?: (value: string) => void;
}) => {
  const editorElementRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(
    null
  );

  useEffect(() => {
    editorRef.current?.dispose();

    editorRef.current = monaco.editor.create(editorElementRef.current!, {
      language: language,
      value: value,
      minimap: { enabled: false },
    });

    editorRef.current.onDidChangeModelContent(() => { 
      const newValue = editorRef.current!.getValue();
      if (newValue && typeof onChange === "function") {
        onChange(newValue);
      }
    });

    return () => {
      editorRef.current?.dispose();
    };
  }, []);

  return <div ref={editorElementRef} className={styles.Editor}></div>;
};

export default Editor;
