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

function AppCreate() {
  const navigate = useNavigate();
  const { getAccessTokenSilently } = useAuth0();
  const schema = yup.object({
    name: yup.string().required(),
  });
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
      className="mx-auto mt-6 max-w-4xl space-y-6"
      onSubmit={handleSubmit(onSubmit)}
    >
      <h1 className="text-lg font-semibold">Create app</h1>

      <div className="space-y-1">
        <Label htmlFor="name">Name</Label>
        <TextInput
          id="name"
          {...register("name")}
        />
        {errors.name && (
          <HelperText color="failure">
            {errors.name.message}
          </HelperText>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="description">Description</Label>
        <Textarea
          id="description"
          rows={4}
          {...register("description")}
        />
        {errors.description && (
          <HelperText color="failure">
            {errors.description.message}
          </HelperText>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-4">
        <Button
          type="button"
          outline
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
