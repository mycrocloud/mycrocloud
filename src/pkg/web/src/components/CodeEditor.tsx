import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";
import { javascript } from "@codemirror/lang-javascript";
import { cn } from "@/lib/utils";

type Language = "json" | "javascript" | "plaintext";

interface CodeEditorProps {
  value: string;
  onChange?: (value: string) => void;
  language?: Language;
  height?: string;
  className?: string;
  placeholder?: string;
  readOnly?: boolean;
}

const languageExtensions = {
  json: [json()],
  javascript: [javascript()],
  plaintext: [],
};

export default function CodeEditor({
  value,
  onChange,
  language = "plaintext",
  height = "200px",
  className,
  placeholder,
  readOnly = false,
}: CodeEditorProps) {
  return (
    <CodeMirror
      value={value}
      height={height}
      extensions={languageExtensions[language]}
      onChange={onChange}
      placeholder={placeholder}
      readOnly={readOnly}
      className={cn("rounded-md border text-sm", className)}
      basicSetup={{
        lineNumbers: false,
        foldGutter: false,
        highlightActiveLine: false,
      }}
    />
  );
}
