import { useFieldArray, useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Plus, X } from "lucide-react";

const QUICK_ADD_HEADERS = [
  { label: "JSON", name: "content-type", value: "application/json" },
  { label: "HTML", name: "content-type", value: "text/html" },
  { label: "Plain Text", name: "content-type", value: "text/plain" },
  { label: "CSS", name: "content-type", value: "text/css" },
  { label: "JavaScript", name: "content-type", value: "text/javascript" },
];

export default function StaticResponse() {
  const {
    control,
    register,
    formState: { errors },
  } = useFormContext<RouteCreateUpdateInputs>();

  const {
    fields: headers,
    append,
    remove,
  } = useFieldArray({ control, name: "response.staticResponse.headers" });

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Label htmlFor="response.staticResponse.statusCode">Status Code</Label>
        <Input
          id="response.staticResponse.statusCode"
          type="number"
          {...register("response.staticResponse.statusCode")}
          className="w-24"
        />
        {errors.response?.staticResponse?.statusCode && (
          <p className="text-sm text-destructive">
            {errors.response.staticResponse.statusCode.message}
          </p>
        )}
      </div>

      <div className="space-y-2">
        <Label>Response Headers</Label>
        <div className="space-y-2">
          {headers.map((header, index) => (
            <div key={header.id} className="flex items-center gap-2">
              <Input
                {...register(`response.staticResponse.headers.${index}.name`)}
                placeholder="Header name"
                className="flex-1"
              />
              <Input
                {...register(`response.staticResponse.headers.${index}.value`)}
                placeholder="Header value"
                className="flex-1"
              />
              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => remove(index)}
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
            onClick={() => append({ name: "", value: "" })}
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
              onClick={() => append({ name: header.name, value: header.value })}
              className="text-xs"
            >
              {header.label}
            </Button>
          ))}
        </div>
      </div>

      <div className="space-y-2">
        <Label htmlFor="response.staticResponse.content">Response Content</Label>
        <textarea
          id="response.staticResponse.content"
          {...register("response.staticResponse.content")}
          className="h-[240px] w-full resize-none rounded-md border bg-background px-3 py-2 font-mono text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          placeholder="Enter static response content..."
        />
        {errors.response?.staticResponse?.content && (
          <p className="text-sm text-destructive">
            {errors.response.staticResponse.content.message}
          </p>
        )}
      </div>
    </div>
  );
}
