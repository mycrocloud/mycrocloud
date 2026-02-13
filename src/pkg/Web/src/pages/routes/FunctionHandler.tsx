import { useEffect } from "react";
import { useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import CodeEditor from "@/components/CodeEditor";
import { Label } from "@/components/ui/label";

const SAMPLE_FUNCTION = `function handler(req) {
  return {
    statusCode: 200,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ message: "Hello, world!" }),
  }
}`;

export default function FunctionHandler() {
  const {
    formState: { errors },
    setValue,
    getValues,
    watch,
  } = useFormContext<RouteCreateUpdateInputs>();

  const response = watch("response");

  // Initialize with sample if empty
  useEffect(() => {
    if (!getValues("response")) {
      setValue("response", SAMPLE_FUNCTION);
    }
  }, []);

  return (
    <div className="space-y-2">
      <Label>Function Handler</Label>
      <p className="text-xs text-muted-foreground">
        Write a JavaScript function that returns a response object with statusCode, headers, and body.
      </p>

      <CodeEditor
        value={response || ""}
        onChange={(value) => setValue("response", value)}
        language="javascript"
        height="280px"
        placeholder="function handler(req) { ... }"
      />

      {errors.response && (
        <p className="text-sm text-destructive">{errors.response.message}</p>
      )}
    </div>
  );
}
