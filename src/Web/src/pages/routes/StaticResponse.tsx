import { useFieldArray, useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Plus, X } from "lucide-react";

const QUICK_ADD_HEADERS = [
  { label: "JSON", name: "content-type", value: "application/json" },
  { label: "HTML", name: "content-type", value: "text/html" },
  { label: "Plain Text", name: "content-type", value: "text/plain" },
  { label: "CSS", name: "content-type", value: "text/css" },
  { label: "JavaScript", name: "content-type", value: "text/javascript" },
  { label: "PNG", name: "content-type", value: "image/png" },
  { label: "JPEG", name: "content-type", value: "image/jpeg" },
  { label: "SVG", name: "content-type", value: "image/svg+xml" },
  { label: "PDF", name: "content-type", value: "application/pdf" },
];

export default function StaticResponse() {
  const {
    control,
    register,
    formState: { errors },
    setValue,
    watch,
  } = useFormContext<RouteCreateUpdateInputs>();

  const {
    fields: responseHeaders,
    append: addResponseHeader,
    remove: removeResponseHeader,
  } = useFieldArray({ control, name: "responseHeaders" });

  return (
    <div className="space-y-4">
      {/* Status Code */}
      <div className="space-y-2">
        <Label htmlFor="responseStatusCode">Status Code</Label>
        <Input
          id="responseStatusCode"
          type="number"
          {...register("responseStatusCode")}
          className="w-24"
        />
        {errors.responseStatusCode && (
          <p className="text-sm text-destructive">{errors.responseStatusCode.message}</p>
        )}
      </div>

      {/* Headers */}
      <div className="space-y-2">
        <Label>Response Headers</Label>
        <div className="space-y-2">
          {responseHeaders.map((header, index) => (
            <div key={header.id} className="flex items-center gap-2">
              <Input
                {...register(`responseHeaders.${index}.name`)}
                placeholder="Header name"
                className="flex-1"
              />
              <Input
                {...register(`responseHeaders.${index}.value`)}
                placeholder="Header value"
                className="flex-1"
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => removeResponseHeader(index)}
                className="h-9 w-9 text-destructive hover:text-destructive"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>
          ))}
        </div>
        <div className="flex flex-wrap gap-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => addResponseHeader({ name: "", value: "" })}
          >
            <Plus className="mr-1 h-3 w-3" />
            Add Header
          </Button>
          {QUICK_ADD_HEADERS.map((header) => (
            <Button
              key={header.label}
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => addResponseHeader({ name: header.name, value: header.value })}
              className="text-xs"
            >
              {header.label}
            </Button>
          ))}
        </div>
      </div>

      {/* Body */}
      <div className="space-y-2">
        <Label htmlFor="response">Response Body</Label>
        <textarea
          id="response"
          {...register("response")}
          className="h-[280px] w-full resize-none rounded-md border bg-background px-3 py-2 font-mono text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          placeholder="Enter response body..."
        />
        {errors.response && (
          <p className="text-sm text-destructive">{errors.response.message}</p>
        )}
      </div>
    </div>
  );
}
