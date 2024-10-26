import { useContext, useEffect } from "react";
import { AppContext } from "../apps";
import IScheme from "./IScheme";
import { useAuth0 } from "@auth0/auth0-react";
import {
  FormProvider,
  useFieldArray,
  useForm,
  useFormContext,
} from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router-dom";
import { yupResolver } from "@hookform/resolvers/yup";
import * as yup from "yup";
import { toast } from "react-toastify";

type Inputs = {
  name: string;
  type: string;
  openIdConnectIssuer?: string;
  openIdConnectAudience?: string;
  apiKeys?: ApiKey[];
};

type ApiKey = {
  name: string;
  key: string;
  metadata?: string;
  active: boolean;
};

const schema = yup.object({
  name: yup.string().required("Name is required"),
  type: yup.string().required("Type is required"),
  openIdConnectIssuer: yup.string(),
  openIdConnectAudience: yup.string(),
  apiKeys: yup.array(
    yup.object({
      name: yup.string().required("Name is required"),
      key: yup.string().required("Key is required"),
      metadata: yup.string(),
      active: yup.boolean().default(true),
    }),
  ),
});
export default function CreateUpdateScheme() {
  const schemeId = useParams()["schemeId"];
  const editMode = schemeId !== undefined;
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();

  const form = useForm<Inputs>({
    defaultValues: {
      name: "",
      type: "OpenIdConnect",
      openIdConnectIssuer: "",
      openIdConnectAudience: "",
    },
    resolver: yupResolver<Inputs>(schema),
  })!;

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    setError,
    watch,
  } = form;

  const watchType = watch("type");

  const onSubmit = async (data: Inputs) => {
    if (data.type === "OpenIdConnect") {
      try {
        const issuer = data.openIdConnectIssuer?.replace(/\/$/, "");
        const res = await fetch(`${issuer}/.well-known/openid-configuration`);
        if (!res.ok) {
          throw new Error("Invalid issuer");
        }
      } catch (error) {
        setError("openIdConnectIssuer", { message: "Invalid issuer" });
        return;
      }
    }

    const submitData = {
      ...data,
      openIdConnectAuthority: data.openIdConnectIssuer,
    };
    delete submitData.openIdConnectIssuer;
    const accessToken = await getAccessTokenSilently();
    const response = await fetch(
      !editMode
        ? `/api/apps/${app.id}/authentications/schemes`
        : `/api/apps/${app.id}/authentications/schemes/${parseInt(schemeId)}`,
      {
        method: !editMode ? "POST" : "PUT",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "content-type": "application/json",
        },
        body: JSON.stringify(submitData),
      },
    );
    if (response.ok) {
      navigate("../schemes");
      return;
    } else {
      toast.error("Failed to save scheme");
    }
  };

  useEffect(() => {
    const getScheme = async () => {
      const accessToken = await getAccessTokenSilently();
      const scheme = (await (
        await fetch(`/api/apps/${app.id}/authentications/schemes/${schemeId}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        })
      ).json()) as IScheme;
      setValue("name", scheme.name);
      setValue("type", scheme.type);
      setValue("openIdConnectIssuer", scheme.openIdConnectAuthority);
      setValue("openIdConnectAudience", scheme.openIdConnectAudience);
    };
    if (editMode) {
      getScheme();
    }
  }, []);
  return (
    <form className="p-2" onSubmit={handleSubmit(onSubmit)}>
      <FormProvider {...form}>
        <h1 className="font-bold">Create Authentication Scheme</h1>
        <div className="mt-3">
          <label htmlFor="name" className="">
            Name
          </label>
          <input
            {...register("name", { required: true })}
            type="text"
            id="name"
            autoComplete="none"
            className="w-full border border-gray-200 px-2 py-1"
          />
          {errors.name && (
            <p className="text-xs text-red-500">{errors.name.message}</p>
          )}
        </div>
        <div className="mt-2">
          <label htmlFor="type">Type</label>
          <select id="type" {...register("type", { required: true })}>
            <option value="OpenIdConnect">OpenID Connect</option>
            <option value="ApiKey">API Key</option>
          </select>
          {errors.type && (
            <p className="text-xs text-red-500">{errors.type.message}</p>
          )}
        </div>
        {watchType === "OpenIdConnect" && <OpenIdConnect />}
        {watchType === "ApiKey" && <ApiKey />}
        <div className="mt-2 flex">
          <Link
            to="../schemes"
            className="me-2 ms-auto inline-flex items-center bg-secondary px-3 text-white"
          >
            Cancel
          </Link>
          <button
            type="submit"
            className="border border-transparent bg-primary px-3 py-2 text-center font-medium text-white focus:z-10 focus:outline-none focus:ring-2 focus:ring-cyan-300 enabled:hover:bg-cyan-800"
          >
            Save
          </button>
        </div>
      </FormProvider>
    </form>
  );
}

function OpenIdConnect() {
  const {
    register,
    formState: { errors },
  } = useFormContext<Inputs>();
  return (
    <>
      <div className="mt-2">
        <label htmlFor="issuer">Issuer</label>
        <input
          {...register("openIdConnectIssuer")}
          type="text"
          id="issuer"
          autoComplete="none"
          className="w-full border border-gray-200 px-2 py-1"
        />
        {errors.openIdConnectIssuer && (
          <p className="text-xs text-red-500">
            {errors.openIdConnectIssuer.message}
          </p>
        )}
      </div>
      <div className="mt-2">
        <label htmlFor="audience">Audience</label>
        <input
          {...register("openIdConnectAudience")}
          type="text"
          id="audience"
          autoComplete="none"
          className="w-full border border-gray-200 px-2 py-1"
        />
        {errors.openIdConnectAudience && (
          <p className="text-xs text-red-500">
            {errors.openIdConnectAudience.message}
          </p>
        )}
      </div>
    </>
  );
}

function ApiKey() {
  const { control } = useFormContext<Inputs>();
  const { fields, append, remove } = useFieldArray({
    control,
    name: "apiKeys",
  });
  return (
    <>
      <table>
        <thead>
          <tr>
            <th>Name</th>
            <th>Key</th>
            <th>Metadata</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {fields.map((field, index) => {
            return (
              <tr key={field.id}>
                <td>
                  <input
                    {...control.register(`apiKeys.${index}.name` as const)}
                    type="text"
                    defaultValue={field.name}
                  />
                </td>
                <td>
                  <input
                    {...control.register(`apiKeys.${index}.key` as const)}
                    type="text"
                    defaultValue={field.key}
                  />
                </td>
                <td>
                  <button type="button" onClick={() => {}}>
                    Edit
                  </button>
                </td>
                <td>
                  <button
                    type="button"
                    onClick={() => {
                      remove(index);
                    }}
                  >
                    Remove
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
      <button
        type="button"
        onClick={() => {
          append({ name: "", key: "", metadata: "", active: true });
        }}
      >
        Add
      </button>
    </>
  );
}
