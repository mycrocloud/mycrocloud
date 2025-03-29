import {
  useRef,
  useState,
  useEffect,
  forwardRef,
  useImperativeHandle,
} from "react";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import styles from "./Editor.module.css";

const Editor = forwardRef(function (props: {
    value: string;
    language: string;
}, ref) {
  const [editor, setEditor] =
    useState<monaco.editor.IStandaloneCodeEditor | null>(null);
  const monacoEl = useRef(null);

  useImperativeHandle(ref, () => ({
    getValue: () => editor?.getValue(),
  }));
  useEffect(() => {
    if (monacoEl) {
      setEditor((editor) => {
        if (editor) return editor;

        return monaco.editor.create(monacoEl.current!, {
          value: props.value,
          language: props.language,
        });
      });
    }

    return () => editor?.dispose();
  }, [monacoEl.current]);

  return <div className={styles.Editor} ref={monacoEl}></div>;
});

export default Editor;