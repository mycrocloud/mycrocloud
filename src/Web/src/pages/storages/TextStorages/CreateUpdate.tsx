import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { useAuth0 } from "@auth0/auth0-react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useApp } from "../../apps";
import { useEffect } from "react";
import { useForm } from "react-hook-form";
import ITextStorage from "./ITextStorage";
import { Spinner } from "flowbite-react";

type Inputs = {
  name: string;
  description?: string;
};

const schema = yup.object({
  name: yup.string().required(),
  description: yup.string().max(400),
});

export default function CreateUpdate() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const storageId = useParams()["storageId"];
  const isEditMode = storageId !== undefined;

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
  });

  const onSubmit = async (inputs: Inputs) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      !isEditMode
        ? `/api/apps/${app.id}/textstorages`
        : `/api/apps/${app.id}/textstorages/${storageId}`,
      {
        method: !isEditMode ? "POST" : "PUT",
        headers: {
          "content-type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify(inputs),
      },
    );
    if (res.ok) {
      navigate("../");
    }
  };

  useEffect(() => {
    const getStorage = async () => {
      if (!storageId) {
        return;
      }
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/textstorages/${storageId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      const storage = (await res.json()) as ITextStorage;
      setValue("name", storage.name);
      setValue("description", storage.description);
    };

    getStorage();
  }, []);
  return (
    <div className="p-3">
      <h1 className="font-bold">
        {isEditMode ? "Edit Storage" : "Create Storage"}
      </h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="mt-2">
          <label className="block">Name</label>
          <input
            type="text"
            {...register("name")}
            className="block w-full border px-2 py-1"
            autoComplete="off"
          />
          {errors.name && (
            <span className="text-red-500">{errors.name.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label className="block">Description</label>
          <textarea
            {...register("description")}
            rows={3}
            className="block w-full border px-2 py-1"
          ></textarea>
          {errors.description && (
            <span className="text-red-500">{errors.description.message}</span>
          )}
        </div>
        <div className="mt-3 flex">
          <Link to={"../"} className="me-2 bg-gray-500 px-4 py-1.5 text-white">
            Cancel
          </Link>
          <button type="submit" className="bg-primary px-5 py-1.5 text-white">
            Save
          </button>
        </div>
      </form>
    </div>
  );
}
