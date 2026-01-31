import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from ".";
import { useForm } from "react-hook-form";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import Ajv, { JSONSchemaType } from "ajv";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";

export default function AppSettings() {
  return (
    <div className="p-4">
      <h2 className="mb-6 text-xl font-semibold">Settings</h2>
      <Tabs defaultValue="general" className="w-full">
        <TabsList className="grid w-full max-w-md grid-cols-3">
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="api">API</TabsTrigger>
          <TabsTrigger value="pages">Pages</TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="mt-6 space-y-6">
          <GeneralTab />
        </TabsContent>

        <TabsContent value="api" className="mt-6 space-y-6">
          <ApiTab />
        </TabsContent>

        <TabsContent value="pages" className="mt-6 space-y-6">
          <PagesTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function GeneralTab() {
  return (
    <>
      <RenameSection />
      <ChangeStateSection />
      <DeleteSection />
    </>
  );
}

function ApiTab() {
  return (
    <>
      <CorsSettingsSection />
    </>
  );
}

function PagesTab() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();

  const [enableCustomErrorPages, setEnableCustomErrorPages] = useState(false);
  const [notFoundPage, setNotFoundPage] = useState("");
  const [errorPage, setErrorPage] = useState("");
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchSettings = async () => {
      try {
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/pages-settings`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        });
        if (res.ok) {
          const data = await res.json();
          setEnableCustomErrorPages(data.enableCustomErrorPages ?? false);
          setNotFoundPage(data.notFoundPage ?? "");
          setErrorPage(data.errorPage ?? "");
        }
      } catch (error) {
        console.error("Failed to fetch pages settings:", error);
      } finally {
        setLoading(false);
      }
    };
    fetchSettings();
  }, [app.id, getAccessTokenSilently]);

  const handleSave = async () => {
    try {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/pages-settings`, {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({
          enableCustomErrorPages,
          notFoundPage,
          errorPage,
        }),
      });
      if (res.ok) {
        toast("Pages settings saved");
      } else {
        toast.error("Failed to save settings");
      }
    } catch (error) {
      toast.error("Failed to save settings");
    }
  };

  if (loading) {
    return <div className="text-muted-foreground">Loading...</div>;
  }

  return (
    <>
      <Card>
        <CardHeader>
          <CardTitle>Custom Error Pages</CardTitle>
          <CardDescription>
            Configure custom HTML pages for error responses
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="enable-custom-pages">Enable Custom Error Pages</Label>
              <p className="text-sm text-muted-foreground">
                Show custom HTML pages instead of default error responses
              </p>
            </div>
            <Switch
              id="enable-custom-pages"
              checked={enableCustomErrorPages}
              onCheckedChange={setEnableCustomErrorPages}
            />
          </div>

          {enableCustomErrorPages && (
            <>
              <div className="space-y-2">
                <Label htmlFor="not-found-page">404 Not Found Page</Label>
                <Textarea
                  id="not-found-page"
                  placeholder="Enter custom HTML for 404 page..."
                  value={notFoundPage}
                  onChange={(e) => setNotFoundPage(e.target.value)}
                  rows={6}
                  className="font-mono text-xs"
                />
                <p className="text-xs text-muted-foreground">
                  HTML content to display when a route is not found
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="error-page">500 Error Page</Label>
                <Textarea
                  id="error-page"
                  placeholder="Enter custom HTML for error page..."
                  value={errorPage}
                  onChange={(e) => setErrorPage(e.target.value)}
                  rows={6}
                  className="font-mono text-xs"
                />
                <p className="text-xs text-muted-foreground">
                  HTML content to display when a server error occurs
                </p>
              </div>
            </>
          )}

          <Button onClick={handleSave}>Save Changes</Button>
        </CardContent>
      </Card>
    </>
  );
}

interface CorsSettings {
  allowedHeaders?: string[];
  allowedMethods?: string[];
  allowedOrigins?: string[];
  exposeHeaders?: string[];
  maxAgeSeconds?: number;
}

const corsSettingsSchema: JSONSchemaType<CorsSettings> = {
  type: "object",
  properties: {
    allowedHeaders: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    allowedMethods: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    allowedOrigins: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    exposeHeaders: { type: "array", nullable: true, items: { type: "string" } },
    maxAgeSeconds: { type: "number", nullable: true },
  },
  additionalProperties: false,
};

function CorsSettingsSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();

  const editorElRef = useRef(null);
  const editor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
  const [error, setError] = useState<string>();

  useEffect(() => {
    editor.current?.dispose();

    editor.current = monaco.editor.create(editorElRef.current!, {
      language: "json",
      value: "",
      minimap: { enabled: false },
    });

    return () => {
      editor.current?.dispose();
    };
  }, []);

  useEffect(() => {
    if (!editor.current) {
      return;
    }
    const fetchCorsSettings = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/cors`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        const json = await res.json();
        editor.current!.setValue(JSON.stringify(json, null, 2));
      }
    };

    fetchCorsSettings();
  }, [editor.current]);

  const handleSaveClick = async () => {
    if (!editor.current) return;
    if (error) {
      setError(undefined);
    }
    const json = editor.current.getValue();
    let data;
    try {
      data = JSON.parse(json);
    } catch (e) {
      setError("Invalid JSON");
      return;
    }
    const ajv = new Ajv();
    const validate = ajv.compile(corsSettingsSchema);
    const valid = validate(data);
    if (!valid) {
      setError(validate.errors?.[0].message!);
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/cors`, {
      method: "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: json,
    });
    if (res.ok) {
      toast("CORS settings saved");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>CORS Settings</CardTitle>
        <CardDescription>
          Configure Cross-Origin Resource Sharing for your API
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="h-[200px] w-full rounded-md border" ref={editorElRef}></div>
        {error && <p className="text-sm text-destructive">{error}</p>}
        <Button onClick={handleSaveClick}>Save Changes</Button>
      </CardContent>
    </Card>
  );
}

type RenameFormInput = { name: string };

function RenameSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const schema = yup.object({ name: yup.string().required() });
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RenameFormInput>({
    resolver: yupResolver(schema),
    defaultValues: { name: app.name },
  });
  const onSubmit = async (input: RenameFormInput) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/rename`, {
      method: "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
        "If-Match": app.version,
      },
      body: JSON.stringify(input),
    });
    if (res.ok) {
      toast("Renamed app");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>App Name</CardTitle>
        <CardDescription>Change the display name of your app</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="flex items-start gap-3">
          <div className="space-y-1">
            <Input
              type="text"
              {...register("name")}
              autoComplete="off"
              className="w-64"
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>
          <Button type="submit">Rename</Button>
        </form>
      </CardContent>
    </Card>
  );
}

function DeleteSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const handleDeleteClick = async () => {
    if (confirm("Are you sure want to delete this app?")) {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        toast("Deleted app");
        navigate("/apps");
      }
    }
  };

  return (
    <Card className="border-destructive/50">
      <CardHeader>
        <CardTitle className="text-destructive">Danger Zone</CardTitle>
        <CardDescription>
          Permanently delete this app and all its data
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button variant="destructive" onClick={handleDeleteClick}>
          Delete App
        </Button>
      </CardContent>
    </Card>
  );
}

function ChangeStateSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const handleChangeStatusClick = async () => {
    if (
      app.status === "Active" &&
      !confirm("Are you sure want to deactivate the app?")
    ) {
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const status = app.status === "Active" ? "Inactive" : "Active";
    const res = await fetch(`/api/apps/${app.id}/status?status=${status}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      app.status = status;
      toast("Status changed");
      navigate(".");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>App Status</CardTitle>
        <CardDescription>
          {app.status === "Active"
            ? "Your app is currently active and receiving traffic"
            : "Your app is currently inactive"}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button
          variant={app.status === "Active" ? "outline" : "default"}
          disabled={app.status === "Blocked"}
          onClick={handleChangeStatusClick}
        >
          {app.status === "Active" ? "Deactivate App" : "Activate App"}
        </Button>
      </CardContent>
    </Card>
  );
}
