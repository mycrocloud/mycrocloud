import { useContext, useEffect, useRef, useState } from "react";
import {
  FieldErrors,
  FormProvider,
  useFieldArray,
  useForm,
  useFormContext,
} from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { AppContext } from "../apps";
import IRoute from "./Route";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import { bodyLanguages, methods } from "./constants";
import {
  RouteCreateUpdateInputs,
  routeCreateUpdateInputsSchema,
} from "./CreateUpdateFormInputs";
import { getConfig } from "@/config";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  ExternalLink,
  Copy,
  Check,
  ChevronDown,
  Plus,
  X,
  AlertTriangle,
  Shield,
  Loader2,
} from "lucide-react";
import { cn } from "@/lib/utils";

const { EDITOR_ORIGIN } = getConfig();


export default function RouteCreateUpdate({
  route,
  onSubmit,
}: {
  route?: IRoute;
  onSubmit: (data: RouteCreateUpdateInputs) => void;
}) {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const forms = useForm<RouteCreateUpdateInputs>({
    resolver: yupResolver(routeCreateUpdateInputsSchema),
    defaultValues: {
      name: route?.name || "untitled route",
      method: route?.method || "GET",
      path: route?.path || "/",
      requestQuerySchema: route?.requestQuerySchema,
      requestHeaderSchema: route?.requestHeaderSchema,
      requestBodySchema: route?.requestBodySchema,
      requireAuthorization: route?.requireAuthorization ?? false,
      responseType: route?.responseType || "Static",
      responseStatusCode: route?.responseStatusCode || 200,
      responseHeaders: route?.responseHeaders
        ? route.responseHeaders.map((value) => {
            return { name: value.name, value: value.value };
          })
        : [],
      response: route?.response,
      responseBodyLanguage: route?.responseBodyLanguage || "plaintext",
      functionHandlerDependencies: route?.functionHandlerDependencies || [],
      useDynamicResponse: route?.useDynamicResponse,
      fileId: route?.fileId,
      enabled: route ? route.enabled : true,
    },
  });
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
    watch,
    setValue,
  } = forms;

  const responseType = watch("responseType");
  const enabled = watch("enabled");
  const url = "https://" + app.domain + watch("path");
  const [copied, setCopied] = useState(false);
  const formRef = useRef<HTMLFormElement>(null);

  const onInvalid = (e: FieldErrors<RouteCreateUpdateInputs>) => {
    console.error(e);
  };

  const copyUrl = () => {
    navigator.clipboard.writeText(url);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  // Ctrl+S to save
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        formRef.current?.requestSubmit();
      }
    };
    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <FormProvider {...forms}>
      <form
        ref={formRef}
        className="flex h-full flex-col"
        onSubmit={handleSubmit(onSubmit, onInvalid)}
      >
        <div className="flex-1 overflow-y-auto p-4">
          {route?.status === "Blocked" && (
            <Alert variant="destructive" className="mb-4">
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                This route is blocked and under review by our team.
              </AlertDescription>
            </Alert>
          )}

          {/* Header */}
          <div className="mb-6 flex items-start justify-between gap-4">
            <div className="flex-1 space-y-2">
              <Label htmlFor="name">Route Name</Label>
              <Input
                id="name"
                {...register("name")}
                autoComplete="off"
                className="max-w-md"
              />
              {errors.name && (
                <p className="text-sm text-destructive">{errors.name.message}</p>
              )}
            </div>
            <div className="flex items-center gap-2 pt-7">
              <Switch
                id="enabled"
                checked={enabled}
                onCheckedChange={(checked) => setValue("enabled", checked)}
              />
              <Label htmlFor="enabled" className="text-sm">
                {enabled ? "Enabled" : "Disabled"}
              </Label>
            </div>
          </div>

          {/* Request Section */}
          <Card className="mb-4">
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Request</CardTitle>
              <CardDescription>Configure the endpoint and request settings</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {/* Method and Path */}
              <div className="space-y-2">
                <Label>Endpoint</Label>
                <div className="flex gap-2">
                  <Select
                    value={watch("method")}
                    onValueChange={(value) => setValue("method", value)}
                  >
                    <SelectTrigger className="w-28">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {methods.map((m) => (
                        <SelectItem key={m} value={m.toUpperCase()}>
                          {m.toUpperCase()}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <Input
                    {...register("path")}
                    autoComplete="off"
                    className="flex-1"
                    placeholder="/api/example"
                  />
                </div>
                {errors.method && (
                  <p className="text-sm text-destructive">{errors.method.message}</p>
                )}
                {errors.path && (
                  <p className="text-sm text-destructive">{errors.path.message}</p>
                )}

                {/* URL Preview */}
                <div className="flex items-center gap-2 rounded-md bg-muted px-3 py-2 text-sm">
                  <span className="text-muted-foreground">URL:</span>
                  <a
                    href={url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="flex items-center gap-1 text-primary hover:underline"
                  >
                    {url}
                    <ExternalLink className="h-3 w-3" />
                  </a>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={copyUrl}
                    className="ml-auto h-6"
                  >
                    {copied ? (
                      <Check className="mr-1 h-3 w-3 text-green-500" />
                    ) : (
                      <Copy className="mr-1 h-3 w-3" />
                    )}
                    {copied ? "Copied" : "Copy"}
                  </Button>
                </div>
              </div>

              {/* Authorization */}
              <div className="flex items-center gap-3 rounded-md border p-3">
                <Shield className="h-4 w-4 text-muted-foreground" />
                <div className="flex-1">
                  <Label htmlFor="requireAuthorization" className="text-sm font-medium">
                    Require Authorization
                  </Label>
                  <p className="text-xs text-muted-foreground">
                    Requests must include a valid authentication token
                  </p>
                </div>
                <Switch
                  id="requireAuthorization"
                  checked={watch("requireAuthorization") || false}
                  onCheckedChange={(checked) => setValue("requireAuthorization", checked)}
                />
              </div>

              {/* Validation */}
              <RequestValidation />
            </CardContent>
          </Card>

          {/* Response Section */}
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Response</CardTitle>
              <CardDescription>Configure what this endpoint returns</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label>Response Type</Label>
                <Select
                  value={responseType}
                  onValueChange={(value) => setValue("responseType", value)}
                >
                  <SelectTrigger className="w-40">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Static">Static</SelectItem>
                    <SelectItem value="Function">Function</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              {responseType === "Static" && <StaticResponse />}
              {responseType === "Function" && <FunctionHandler />}
              {errors.response && (
                <p className="text-sm text-destructive">{errors.response.message}</p>
              )}
            </CardContent>
          </Card>
        </div>

        {/* Footer */}
        <div className="border-t bg-background p-4">
          <Button type="submit" disabled={route?.status === "Blocked" || isSubmitting}>
            {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {route ? "Save Changes" : "Create Route"}
          </Button>
          <span className="ml-3 text-xs text-muted-foreground">
            {navigator.platform.includes("Mac") ? "⌘" : "Ctrl"}+S to save
          </span>
        </div>
      </form>
    </FormProvider>
  );
}

const quickAddHeaders = [
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

function RequestValidation() {
  const {
    getValues,
    setValue,
    formState: { errors },
  } = useFormContext<RouteCreateUpdateInputs>();
  const [isOpen, setIsOpen] = useState(false);
  const [tab, setTab] = useState("query");
  const editorEl = useRef<HTMLDivElement>(null);
  const editor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
  const requestQuerySchemaModel = useRef<monaco.editor.ITextModel | null>(null);
  const requestHeaderSchemaModel = useRef<monaco.editor.ITextModel | null>(null);
  const requestBodySchemaModel = useRef<monaco.editor.ITextModel | null>(null);

  useEffect(() => {
    if (!isOpen) return;

    requestQuerySchemaModel.current?.dispose();
    requestHeaderSchemaModel.current?.dispose();
    requestBodySchemaModel.current?.dispose();
    editor.current?.dispose();

    requestQuerySchemaModel.current = monaco.editor.createModel(
      getValues("requestQuerySchema") || "",
      "json"
    );
    requestQuerySchemaModel.current.onDidChangeContent(() => {
      setValue("requestQuerySchema", requestQuerySchemaModel.current!.getValue());
    });

    requestHeaderSchemaModel.current = monaco.editor.createModel(
      getValues("requestHeaderSchema") || "",
      "json"
    );
    requestHeaderSchemaModel.current.onDidChangeContent(() => {
      setValue("requestHeaderSchema", requestHeaderSchemaModel.current!.getValue());
    });

    requestBodySchemaModel.current = monaco.editor.createModel(
      getValues("requestBodySchema") || "",
      "json"
    );
    requestBodySchemaModel.current.onDidChangeContent(() => {
      setValue("requestBodySchema", requestBodySchemaModel.current!.getValue());
    });

    editor.current = monaco.editor.create(editorEl.current!, {
      model: requestQuerySchemaModel.current,
      automaticLayout: true,
      minimap: { enabled: false },
      lineNumbers: "off",
      folding: false,
      scrollBeyondLastLine: false,
    });

    return () => {
      requestQuerySchemaModel.current?.dispose();
      requestHeaderSchemaModel.current?.dispose();
      requestBodySchemaModel.current?.dispose();
      editor.current?.dispose();
    };
  }, [isOpen]);

  useEffect(() => {
    if (!editor.current) return;
    switch (tab) {
      case "query":
        editor.current.setModel(requestQuerySchemaModel.current!);
        break;
      case "headers":
        editor.current.setModel(requestHeaderSchemaModel.current!);
        break;
      case "body":
        editor.current.setModel(requestBodySchemaModel.current!);
        break;
    }
  }, [tab]);

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
          <Tabs value={tab} onValueChange={setTab}>
            <TabsList className="w-full justify-start rounded-none border-b bg-transparent p-0">
              <TabsTrigger
                value="query"
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:shadow-none"
              >
                Query Params
              </TabsTrigger>
              <TabsTrigger
                value="headers"
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:shadow-none"
              >
                Headers
              </TabsTrigger>
              <TabsTrigger
                value="body"
                className="rounded-none border-b-2 border-transparent data-[state=active]:border-primary data-[state=active]:shadow-none"
              >
                Body
              </TabsTrigger>
            </TabsList>
            <TabsContent value={tab} className="m-0">
              <div ref={editorEl} className="h-[180px] w-full" />
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

function StaticResponse() {
  const {
    control,
    register,
    formState: { errors },
    setValue,
    getValues,
    watch,
  } = useFormContext<RouteCreateUpdateInputs>();
  const {
    fields: responseHeaders,
    append: addResponseHeader,
    remove: removeResponseHeader,
  } = useFieldArray({ control, name: "responseHeaders" });

  const bodyEditorRef = useRef<HTMLDivElement>(null);
  const bodyEditor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);

  useEffect(() => {
    bodyEditor.current?.dispose();

    bodyEditor.current = monaco.editor.create(bodyEditorRef.current!, {
      language: getValues("responseBodyLanguage"),
      value: getValues("response") || undefined,
      minimap: { enabled: false },
      automaticLayout: true,
      scrollBeyondLastLine: false,
    });
    bodyEditor.current.onDidChangeModelContent(() => {
      setValue("response", bodyEditor.current!.getValue());
    });

    return () => {
      bodyEditor.current?.dispose();
    };
  }, []);

  const responseBodyLanguage = watch("responseBodyLanguage");
  useEffect(() => {
    if (bodyEditor.current && responseBodyLanguage) {
      monaco.editor.setModelLanguage(
        bodyEditor.current!.getModel()!,
        responseBodyLanguage
      );
    }
  }, [responseBodyLanguage]);

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
          {quickAddHeaders.map((header) => (
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
        <div className="flex items-center justify-between">
          <Label>Response Body</Label>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Switch
                id="useDynamicResponse"
                checked={watch("useDynamicResponse") || false}
                onCheckedChange={(checked) => setValue("useDynamicResponse", checked)}
              />
              <Label htmlFor="useDynamicResponse" className="text-xs text-muted-foreground">
                Dynamic response
              </Label>
            </div>
            <Select
              value={responseBodyLanguage}
              onValueChange={(value) => setValue("responseBodyLanguage", value)}
            >
              <SelectTrigger className="h-8 w-32 text-xs">
                <SelectValue placeholder="Language" />
              </SelectTrigger>
              <SelectContent>
                {bodyLanguages.map((lang) => (
                  <SelectItem key={lang} value={lang}>
                    {lang}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div ref={bodyEditorRef} className="h-[280px] w-full rounded-md border" />
        {errors.response && (
          <p className="text-sm text-destructive">{errors.response.message}</p>
        )}
      </div>
    </div>
  );
}

function validEditorMessage(e: MessageEvent, editor: string) {
  if (e.origin !== EDITOR_ORIGIN) return false;
  if (e.data.editorId !== editor) return false;
  return true;
}

function FunctionHandler() {
  const {
    formState: { errors },
    setValue,
    getValues,
  } = useFormContext<RouteCreateUpdateInputs>();

  const editorId = "functionHandler";
  const editorRef = useRef<HTMLIFrameElement>(null);
  const [editorLoaded, setEditorLoaded] = useState(false);

  const sampleFunctionHandler = `function handler(req) {
  return {
    statusCode: 200,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ message: "Hello, world!" }),
  }
}`;

  useEffect(() => {
    const functionHandler = getValues("response");
    if (!functionHandler) {
      setValue("response", sampleFunctionHandler);
    }

    const onMessage = (e: MessageEvent) => {
      if (!validEditorMessage(e, editorId)) return;

      const { type, payload } = e.data;
      switch (type) {
        case "loaded":
          setEditorLoaded(true);
          break;
        case "changed":
          setValue("response", payload);
          break;
      }
    };

    window.addEventListener("message", onMessage);
    return () => window.removeEventListener("message", onMessage);
  }, []);

  useEffect(() => {
    if (!editorLoaded) return;

    editorRef.current?.contentWindow?.postMessage(
      {
        editorId,
        type: "load",
        payload: {
          value: getValues("response"),
          language: "javascript",
        },
      },
      EDITOR_ORIGIN
    );
  }, [editorLoaded]);

  return (
    <div className="space-y-2">
      <Label>Function Handler</Label>
      <p className="text-xs text-muted-foreground">
        Write a JavaScript function that returns a response object with statusCode, headers, and body.
      </p>
      <iframe
        ref={editorRef}
        src={EDITOR_ORIGIN + "?id=" + editorId}
        className="h-[280px] w-full rounded-md border"
      />
      {errors.response && (
        <p className="text-sm text-destructive">{errors.response.message}</p>
      )}
    </div>
  );
}
