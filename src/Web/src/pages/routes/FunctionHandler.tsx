import { useEffect, useRef, useState } from "react";
import { useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import CodeEditor from "@/components/CodeEditor";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { getConfig } from "@/config";

const { EDITOR_ORIGIN } = getConfig();

const SAMPLE_FUNCTION = `function handler(req) {
  return {
    statusCode: 200,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ message: "Hello, world!" }),
  }
}`;

type EditorMode = "simple" | "advanced";

const EDITOR_ID = "functionHandler";

export default function FunctionHandler() {
  const {
    formState: { errors },
    setValue,
    getValues,
    watch,
  } = useFormContext<RouteCreateUpdateInputs>();

  const [mode, setMode] = useState<EditorMode>("simple");
  const editorRef = useRef<HTMLIFrameElement>(null);
  const [editorLoaded, setEditorLoaded] = useState(false);

  const response = watch("response");

  // Initialize with sample if empty
  useEffect(() => {
    if (!getValues("response")) {
      setValue("response", SAMPLE_FUNCTION);
    }
  }, []);

  // Send code to iframe
  const syncToIframe = () => {
    editorRef.current?.contentWindow?.postMessage(
      {
        editorId: EDITOR_ID,
        type: "load",
        payload: {
          value: getValues("response"),
          language: "javascript",
        },
      },
      EDITOR_ORIGIN
    );
  };

  // Advanced mode: iframe message handling
  useEffect(() => {
    if (mode !== "advanced") return;

    const onMessage = (e: MessageEvent) => {
      if (e.origin !== EDITOR_ORIGIN || e.data.editorId !== EDITOR_ID) return;

      const { type, payload } = e.data;
      if (type === "loaded") {
        setEditorLoaded(true);
      } else if (type === "changed") {
        setValue("response", payload);
      }
    };

    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, [mode, setValue]);

  // Sync to iframe when loaded
  useEffect(() => {
    if (mode === "advanced" && editorLoaded) {
      syncToIframe();
    }
  }, [mode, editorLoaded]);

  const handleModeChange = (newMode: EditorMode) => {
    if (newMode === "advanced" && editorLoaded) {
      syncToIframe();
    }
    setMode(newMode);
  };

  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between">
        <Label>Function Handler</Label>
        <div className="flex items-center gap-1 rounded-md border p-1">
          <Button
            type="button"
            variant={mode === "simple" ? "secondary" : "ghost"}
            size="sm"
            className="h-6 px-2 text-xs"
            onClick={() => handleModeChange("simple")}
          >
            Simple
          </Button>
          <Button
            type="button"
            variant={mode === "advanced" ? "secondary" : "ghost"}
            size="sm"
            className="h-6 px-2 text-xs"
            onClick={() => handleModeChange("advanced")}
          >
            Advanced
          </Button>
        </div>
      </div>
      <p className="text-xs text-muted-foreground">
        Write a JavaScript function that returns a response object with statusCode, headers, and body.
      </p>

      {mode === "simple" ? (
        <CodeEditor
          value={response || ""}
          onChange={(value) => setValue("response", value)}
          language="javascript"
          height="280px"
          placeholder="function handler(req) { ... }"
        />
      ) : (
        <iframe
          ref={editorRef}
          src={`${EDITOR_ORIGIN}?id=${EDITOR_ID}`}
          className="h-[280px] w-full rounded-md border"
        />
      )}

      {errors.response && (
        <p className="text-sm text-destructive">{errors.response.message}</p>
      )}
    </div>
  );
}
