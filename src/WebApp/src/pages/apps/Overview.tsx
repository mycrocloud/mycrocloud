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

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const domain = getAppDomain(app.id);

  return (
    <div className="p-2">
      <h2 className="font-bold">Overview</h2>
      <table className="mt-1">
        <tbody>
          <tr>
            <td>Name</td>
            <td>{app.name}</td>
          </tr>
          <tr>
            <td>Description</td>
            <td>{app.description}</td>
          </tr>
          <tr>
            <td>Status</td>
            <td className="inline-flex">
              {app.status === "Active" ? (
                <PlayCircleIcon className="h-4 w-4 text-green-500" />
              ) : (
                <StopCircleIcon className="h-4 w-4 text-red-500" />
              )}
              {app.status}
            </td>
          </tr>
          <tr>
            <td>Created at</td>
            <td>{new Date(app.createdAt).toDateString()}</td>
          </tr>
          <tr>
            <td>Updated at</td>
            <td>
              {app.updatedAt ? new Date(app.updatedAt!).toDateString() : "-"}
            </td>
          </tr>
          <tr>
            <td>Domain</td>
            <td className="flex">
              <p className="text-blue-500 hover:underline">{domain}</p>
              <TextCopyButton text={domain} />
            </td>
          </tr>
        </tbody>
      </table>
      <hr className="mt-2" />
      <div className="mt-2">
        <RenameSection />
      </div>
      <hr className="mt-2" />
      <div className="mt-2">
        <ChangeStateSection />
      </div>
      <hr className="mt-2" />
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
    <>
      <h3 className="font-semibold">App name</h3>
      <form onSubmit={handleSubmit(onSubmit)} className="mt-1">
        <div className="flex">
          <div>
            <input
              type="text"
              {...register("name")}
              className="block border px-2 py-0.5"
              autoComplete="off"
            />
            {errors.name && (
              <span className="text-red-500">{errors.name.message}</span>
            )}
          </div>
          <div className="relative ms-1">
            <button
              type="submit"
              className="absolute top-0 my-auto bg-primary px-2 py-0.5 text-white"
            >
              Rename
            </button>
          </div>
        </div>
      </form>
    </>
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
  function getChangeStatusButtonClass(status: string) {
    switch (status) {
      case "Active":
        return "text-red-500";
      case "Inactive":
        return "text-green-500";
      case "Blocked":
        return "text-gray-500";
      default:
        return "";
    }
  }
  return (
    <div>
      <h2 className="font-semibold">Change status</h2>
      <button
        type="button"
        className={`${getChangeStatusButtonClass(app.status)} border px-2 py-1`}
        disabled={app.status === "Blocked"}
        onClick={handleChangeStatusClick}
      >
        {app.status === "Active" ? "Deactivate" : "Activate"}
      </button>
    </div>
  );
}
