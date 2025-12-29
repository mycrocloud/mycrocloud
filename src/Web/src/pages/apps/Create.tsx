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
    <form className="mx-auto mt-5 max-w-4xl" onSubmit={handleSubmit(onSubmit)}>
      <h1 className="font-semibold">Create app</h1>
      <div className="mb-5 mt-3">
        <Label>Name</Label>
        <TextInput
          {...register("name")}
        />
        {errors.name && <HelperText>{errors.name.message}</HelperText>}
      </div>
      <div className="mb-5 mt-3">
        <Label>Description</Label>
        <Textarea
          {...register("description")}
        />
        {errors.description && <HelperText>{errors.description.message}</HelperText>}
      </div>
      <div className="flex">
        <Button
          onClick={() => navigate("/apps")}
          outline
          className="ms-auto"
        >
          Cancel
        </Button>
        <Button
          type="submit"
          className="ms-2"
        >
          Save
        </Button>
      </div>
    </form>
  );
}
export default AppCreate;
