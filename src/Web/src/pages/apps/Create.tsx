import { SubmitHandler, useForm } from "react-hook-form";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { useNavigate } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { Button, HelperText, Label, Textarea, TextInput } from "flowbite-react";

type Inputs = {
  name: string;
  description?: string;
};

const schema = yup.object({
  name: yup.string().required(),
});

function AppCreate() {
  const navigate = useNavigate();
  const { getAccessTokenSilently } = useAuth0();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
  });

  const onSubmit: SubmitHandler<Inputs> = async (data) => {
    const accessToken = await getAccessTokenSilently();
    //TODO: useApiClient
    const res = await fetch("/api/apps", {
      method: "POST",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(data),
    });
    if (res.ok) {
      const id = parseInt(res.headers.get("Location")!);
      navigate(`../${id}`);
    }
  };

  return (
    <form
      className="mx-auto mt-8 max-w-2xl p-6 bg-white rounded-lg shadow-sm dark:bg-gray-800"
      onSubmit={handleSubmit(onSubmit)}
    >
      <h1 className="text-2xl font-semibold text-gray-900 dark:text-white">
        Create App
      </h1>
      <div className="mt-6">
        <Label htmlFor="name">Name</Label>
        <TextInput
          id="name"
          {...register("name")}
          color={errors.name ? "failure" : "gray"}
        />
        {errors.name && <HelperText className="text-red-500">{errors.name.message}</HelperText>}
      </div>
      <div className="mt-6">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          rows={4}
          {...register("description")}
          color={errors.description ? "failure" : "gray"}
        />
        {errors.description && <HelperText className="text-red-500">{errors.description.message}</HelperText>}
      </div>
      <div className="mt-8 flex justify-end gap-3">
        <Button
          color={"light"}
          type="button"
          onClick={() => navigate("/apps")}
        >
          Cancel
        </Button>
        <Button type="submit">
          Save
        </Button>
      </div>
    </form>
  );
}
export default AppCreate;
