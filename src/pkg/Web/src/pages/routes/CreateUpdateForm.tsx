import { useContext, useEffect, useRef, useState } from "react";
import { FieldErrors, FormProvider, useForm } from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { AppContext } from "../apps";
import IRoute from "./Route";
import { methods } from "./constants";
import {
  RouteCreateUpdateInputs,
  routeCreateUpdateInputsSchema,
} from "./CreateUpdateFormInputs";
import RequestValidation from "./RequestValidation";
import StaticResponse from "./StaticResponse";
import FunctionHandler from "./FunctionHandler";
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
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  ExternalLink,
  Copy,
  Check,
  AlertTriangle,
  Shield,
  Loader2,
} from "lucide-react";

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
      responseHeaders: route?.responseHeaders?.map(({ name, value }) => ({ name, value })) || [],
      response: route?.response,
      responseBodyLanguage: route?.responseBodyLanguage || "plaintext",
      functionHandlerDependencies: route?.functionHandlerDependencies || [],
      fileId: route?.fileId,
      enabled: route?.enabled ?? true,
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
  const url = `https://${app.domain}${watch("path")}`;

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
