import { useState } from "react";
import { useFormContext } from "react-hook-form";
import { RouteCreateUpdateInputs } from "./CreateUpdateFormInputs";
import CodeEditor from "@/components/CodeEditor";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/utils";

type SchemaTab = "query" | "headers" | "body";

const TAB_TRIGGER_CLASS =
  "rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:shadow-none";

export default function RequestValidation() {
  const {
    watch,
    setValue,
    formState: { errors },
  } = useFormContext<RouteCreateUpdateInputs>();
  const [isOpen, setIsOpen] = useState(false);
  const [tab, setTab] = useState<SchemaTab>("query");

  const schemas = {
    query: watch("requestQuerySchema") || "",
    headers: watch("requestHeaderSchema") || "",
    body: watch("requestBodySchema") || "",
  };

  const handleChange = (value: string) => {
    const fieldMap = {
      query: "requestQuerySchema",
      headers: "requestHeaderSchema",
      body: "requestBodySchema",
    } as const;
    setValue(fieldMap[tab], value);
  };

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen}>
      <CollapsibleTrigger asChild>
        <Button variant="ghost" size="sm" className="w-full justify-start gap-2">
          <ChevronDown
            className={cn("h-4 w-4 transition-transform", isOpen && "rotate-180")}
          />
          Request Validation (JSON Schema)
        </Button>
      </CollapsibleTrigger>
      <CollapsibleContent className="mt-2">
        <div className="rounded-md border">
          <Tabs value={tab} onValueChange={(v) => setTab(v as SchemaTab)}>
            <TabsList className="w-full justify-start rounded-none border-b bg-transparent p-0">
              <TabsTrigger value="query" className={TAB_TRIGGER_CLASS}>
                Query Params
              </TabsTrigger>
              <TabsTrigger value="headers" className={TAB_TRIGGER_CLASS}>
                Headers
              </TabsTrigger>
              <TabsTrigger value="body" className={TAB_TRIGGER_CLASS}>
                Body
              </TabsTrigger>
            </TabsList>
            <TabsContent value={tab} className="m-0">
              {isOpen && (
                <CodeEditor
                  value={schemas[tab]}
                  onChange={handleChange}
                  language="json"
                  height="180px"
                  placeholder="Enter JSON schema..."
                  className="rounded-none border-0"
                />
              )}
            </TabsContent>
          </Tabs>
        </div>
        {errors.requestQuerySchema && (
          <p className="mt-1 text-sm text-destructive">{errors.requestQuerySchema.message}</p>
        )}
        {errors.requestHeaderSchema && (
          <p className="mt-1 text-sm text-destructive">{errors.requestHeaderSchema.message}</p>
        )}
        {errors.requestBodySchema && (
          <p className="mt-1 text-sm text-destructive">{errors.requestBodySchema.message}</p>
        )}
      </CollapsibleContent>
    </Collapsible>
  );
}
