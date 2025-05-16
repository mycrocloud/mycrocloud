import { useEffect, useRef } from "react";
import * as monaco from "monaco-editor";
import { EditorAPI } from "./types/editorBridge";

function App() {
  const editorId = new URLSearchParams(window.location.search).get('id') || 'default';

  const editorElementRef = useRef<HTMLDivElement>(null);
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(
    null
  );

  useEffect(() => {
    editorRef.current?.dispose();

    const editor = monaco.editor.create(editorElementRef.current!, {
      language: 'javascript',
      value: '',
      minimap: { enabled: false },
    });

    editorRef.current = editor;

    (window as any).editorAPI = {
      setValue: (val: string) => editor.setValue(val),
      getValue: () => editor.getValue(),
      focus: () => editor.focus(),
      setLanguage: (lang: string) => monaco.editor.setModelLanguage(editor.getModel()!, lang),
    } satisfies EditorAPI;

    editor.onDidChangeModelContent(() => {
      const value = editor.getValue();
      (window.parent as any).onEditorContentChange?.(editorId, value);
    });

    return () => {
      editorRef.current?.dispose();
    };
  }, []);

  return <div ref={editorElementRef} className="editor"></div>;
}

export default App;
