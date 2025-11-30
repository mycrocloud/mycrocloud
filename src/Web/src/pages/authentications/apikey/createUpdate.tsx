import { yupResolver } from "@hookform/resolvers/yup";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import * as yup from "yup";
import type { ObjectSchema } from "yup";
import { generateApiKey } from "./helper";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useEffect } from "react";
import { useApp } from "../../apps";
import { Spinner } from "flowbite-react";

type Inputs = {
  name: string;
  key: string;
  metadata?: string | null;
};

const schema: ObjectSchema<Inputs> = yup.object({
  name: yup.string().required("Name is required"),
  key: yup.string().required("Key is required"),
  metadata: yup.string().nullable(),
});
export default function CreateUpdate() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const { app } = useApp();
    if (!app) return <Spinner aria-label="Loading..." />

  const editKeyId = useParams()["keyId"]
    ? parseInt(useParams()["keyId"]!.toString())
    : undefined;

  const {
    register,
    formState: { errors },
    handleSubmit,
    setValue,
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
  });

  useEffect(() => {
    if (editKeyId) {
      const fetchKey = async () => {
        const accessToken = await getAccessTokenSilently();
        const response = await fetch(
          `/api/apps/${app.id}/apikeys/${editKeyId}`,
          {
            headers: {
              authorization: `Bearer ${accessToken}`,
            },
          },
        );
        const key = await response.json();
        setValue("name", key.name);
        setValue("key", key.key);
        setValue("metadata", key.metadata);
      };
      fetchKey();
    }
  }, [editKeyId]);

  const onSubmit = async (data: Inputs) => {
    const accessToken = await getAccessTokenSilently();
    try {
      const url = editKeyId
        ? `/api/apps/${app.id}/apikeys/${editKeyId}`
        : `/api/apps/${app.id}/apikeys`;

      const method = editKeyId ? "PUT" : "POST";

      const response = await fetch(url, {
        method,
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(data),
      });

      if (response.ok) {
        navigate("../");
      } else {
        throw new Error("Failed to save key");
      }
    } catch (error) {
      toast.error("Failed to save key");
    }
  };

  const handleGenerateKey = () => {
    const key = generateApiKey();
    setValue("key", key);
  };

  return (
    <div className="p-3">
      <h1 className="font-bold">{editKeyId ? "Edit key" : "Create new key"}</h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <div>
          <label className="mt-2 block">Name</label>
          <input
            type="text"
            {...register("name")}
            className="w-full border px-2 py-1"
          />
          {errors.name && (
            <span className="text-red-500">{errors.name.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label className="mt-2 block">Key</label>
          <div className="flex space-x-2">
            <input
              type="text"
              {...register("key")}
              className="w-full border px-2 py-1"
            />
            <button
              type="button"
              onClick={handleGenerateKey}
              className="text-primary hover:underline"
            >
              Generate
            </button>
          </div>
          {errors.key && (
            <span className="text-red-500">{errors.key.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label>Metadata (json)</label>
          <textarea
            {...register("metadata")}
            className="w-full border px-2 py-1"
            rows={5}
          ></textarea>
          {errors.metadata && (
            <span className="text-red-500">{errors.metadata.message}</span>
          )}
        </div>
        <div className="mt-2 flex space-x-1">
          <button
            type="button"
            onClick={() => {
              navigate("../");
            }}
            className="bg-gray-500 px-2 py-1 text-white"
          >
            Cancel
          </button>
          <button type="submit" className="bg-primary px-2 py-1 text-white">
            Save
          </button>
        </div>
      </form>
    </div>
  );
}
