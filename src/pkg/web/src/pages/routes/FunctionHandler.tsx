import { useEffect } from "react";
import { useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import CodeEditor from "@/components/CodeEditor";
import { Label } from "@/components/ui/label";

const SAMPLE_FUNCTION = `function handler(req) {
  return {
    statusCode: 200,
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

  const sourceCode = watch("response.functionResponse.sourceCode");

  // Initialize with sample if empty
  useEffect(() => {
    if (!getValues("response.functionResponse.sourceCode")) {
      setValue("response.functionResponse.sourceCode", SAMPLE_FUNCTION);
    }
  }, []);

  return (
    <div className="space-y-2">
      <Label>Function Handler</Label>
      <p className="text-xs text-muted-foreground">
        Write a JavaScript function that returns a response object.
      </p>

      <CodeEditor
        value={sourceCode || ""}
        onChange={(value) => setValue("response.functionResponse.sourceCode", value)}
        language="javascript"
        height="280px"
        placeholder="function handler(req) { ... }"
      />

      {errors.response?.functionResponse?.sourceCode && (
        <p className="text-sm text-destructive">
          {errors.response.functionResponse.sourceCode.message}
        </p>
      )}
    </div>
  );
}
