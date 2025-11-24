import { useEffect } from "react";
import IVariable from "./Variable";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useForm } from "react-hook-form";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { useAuth0 } from "@auth0/auth0-react";
import { useApp } from "../../apps";
import { Spinner } from "flowbite-react";

type Inputs = {
  name: string;
  stringValue?: string;
  valueType: string;
  isSecret: boolean;
};

const schema = yup.object({
  name: yup.string().required(),
  stringValue: yup.string().max(400),
  valueType: yup.string().required(),
  isSecret: yup.boolean().required(),
});

export default function AddUpdateVariables() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const { app } = useApp();
  if (!app) return <Spinner aria-label="Loading..." />
  const variableId = useParams()["variableId"];
  const isEditMode = variableId !== undefined;

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
    watch,
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
  });

  const onSubmit = async (inputs: Inputs) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      !isEditMode
        ? `/api/apps/${app.id}/variables`
        : `/api/apps/${app.id}/variables/${variableId}`,
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
    if (variableId) {
      const getVariable = async () => {
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/variables/${variableId}`, {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        });
        const variable = (await res.json()) as IVariable;

        setValue("isSecret", variable.isSecret);
        setValue("name", variable.name);
        setValue("stringValue", variable.stringValue);
        setValue("valueType", variable.valueType);
      };

      getVariable();
    }
  }, []);

  const valueType = watch("valueType");
  return (
    <div className="p-3">
      <h1 className="font-bold">
        {isEditMode ? "Edit Variable" : "Create Variable"}
      </h1>
      <form onSubmit={handleSubmit(onSubmit)}>
        <div className="mt-2">
          <div className="inline-flex">
            <input type="checkbox" {...register("isSecret")} />
            <label className="ms-1">Is Secret</label>
          </div>
          {errors.isSecret && (
            <span className="text-red-500">{errors.isSecret.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label className="block">Name</label>
          <input
            type="text"
            {...register("name")}
            className="block w-full border px-2 py-1"
          />
          {errors.name && (
            <span className="text-red-500">{errors.name.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label className="block">Value Type</label>
          <select {...register("valueType")}>
            <option value="String">String</option>
            <option value="Number">Number</option>
            <option value="Boolean">Boolean</option>
            <option value="Null">Null</option>
          </select>
          {errors.valueType && (
            <span className="text-red-500">{errors.valueType.message}</span>
          )}
        </div>
        <div className="mt-2">
          <label className="block">Value</label>
          <input
            type="text"
            {...register("stringValue")}
            className="block w-full border px-2 py-1"
            disabled={valueType === "Null"}
          />
          {errors.stringValue && (
            <span className="text-red-500">{errors.stringValue.message}</span>
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
