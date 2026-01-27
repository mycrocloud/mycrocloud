import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from ".";
import { useForm } from "react-hook-form";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import { getAppDomain } from "./service";
import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import Ajv, { JSONSchemaType } from "ajv";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const domain = getAppDomain(app.id);

  return (
    <div className="space-y-6 p-4">
      <div>
        <h2 className="text-xl font-semibold">Overview</h2>
        <div className="mt-4 grid grid-cols-[auto_1fr] gap-x-8 gap-y-2 text-sm">
          <span className="text-muted-foreground">Name</span>
          <span>{app.name}</span>
          <span className="text-muted-foreground">Description</span>
          <span>{app.description || "-"}</span>
          <span className="text-muted-foreground">Status</span>
          <span className="flex items-center gap-1">
            {app.status === "Active" ? (
              <PlayCircleIcon className="h-4 w-4 text-green-500" />
            ) : (
              <StopCircleIcon className="h-4 w-4 text-red-500" />
            )}
            {app.status}
          </span>
          <span className="text-muted-foreground">Created at</span>
          <span>{new Date(app.createdAt).toDateString()}</span>
          <span className="text-muted-foreground">Updated at</span>
          <span>{app.updatedAt ? new Date(app.updatedAt!).toDateString() : "-"}</span>
          <span className="text-muted-foreground">Domain</span>
          <span className="flex items-center gap-1">
            <span className="text-primary">{domain}</span>
            <TextCopyButton text={domain} />
          </span>
        </div>
      </div>
      <hr />
      <RenameSection />
      <hr />
      <ChangeStateSection />
      <hr />
      <CorsSettingsSection />
      <hr />
      <DeleteSection />
    </div>
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
    <div className="space-y-2">
      <Label className="text-base font-semibold">CORS Settings</Label>
      <div className="h-[160px] w-full rounded-md border" ref={editorElRef}></div>
      {error && <p className="text-sm text-destructive">{error}</p>}
      <Button onClick={handleSaveClick}>Save</Button>
    </div>
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
    <div className="space-y-2">
      <Label className="text-base font-semibold">App name</Label>
      <form onSubmit={handleSubmit(onSubmit)} className="flex items-start gap-2">
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
    </div>
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
    <div className="space-y-2">
      <Label className="text-base font-semibold">Delete the app</Label>
      <div>
        <Button variant="destructive" onClick={handleDeleteClick}>
          Delete
        </Button>
      </div>
    </div>
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
      //TODO: update app status in context
      app.status = status;
      toast("Status changed");
      navigate(".");
    }
  };
  return (
    <div className="space-y-2">
      <Label className="text-base font-semibold">Change status</Label>
      <div>
        <Button
          variant={app.status === "Active" ? "destructive" : "default"}
          disabled={app.status === "Blocked"}
          onClick={handleChangeStatusClick}
        >
          {app.status === "Active" ? "Deactivate" : "Activate"}
        </Button>
      </div>
    </div>
  );
}
