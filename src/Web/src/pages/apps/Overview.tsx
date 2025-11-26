import { useEffect, useRef, useState } from "react";
import { useApp } from ".";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import Ajv, { JSONSchemaType } from "ajv";
import { Spinner } from "flowbite-react";
import { default as AppOverviewComponent } from "./components/AppOverview"

export default function AppOverview() {
  return (
    <div className="p-2">
      <AppOverviewComponent />
      <div className="mt-2">
        <CorsSettingsSection />
      </div>
      <hr className="mt-2" />
      <div className="mt-2">
        <DeleteSection />
      </div>
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
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />

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
    <>
      <h3 className="font-semibold">CORS Settings</h3>
      <div className="mt-1">
        <div className="h-[160px] w-full" ref={editorElRef}></div>
        {error && <span className="text-red-500">{error}</span>}
        <button
          type="button"
          onClick={handleSaveClick}
          className="ms-auto bg-primary px-2 py-1 text-white"
        >
          Save
        </button>
      </div>
    </>
  );
}

function DeleteSection() {
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />

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
    <>
      <h3 className="font-semibold">Delete the app</h3>
      <button
        type="button"
        className="bg-red-500 px-2 py-1 text-white"
        onClick={handleDeleteClick}
      >
        Delete
      </button>
    </>
  );
}
