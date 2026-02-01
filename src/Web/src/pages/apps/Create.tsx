import { useState } from "react";
import { SubmitHandler, useForm } from "react-hook-form";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { useNavigate } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { generateAppName } from "@/lib/utils";

type Inputs = {
  name: string;
  description?: string;
};

function AppCreate() {
  const navigate = useNavigate();
  const { getAccessTokenSilently } = useAuth0();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const schema = yup.object({
    name: yup.string().required("Name is required"),
  });
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
    defaultValues: {
      name: generateAppName(),
    },
  });
  const onSubmit: SubmitHandler<Inputs> = async (data) => {
    setIsSubmitting(true);
    try {
      const accessToken = await getAccessTokenSilently();
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
      } else if (res.status === 409) {
        setError("name", { message: "This app name is already taken" });
      } else {
        toast.error("Something went wrong");
      }
    } catch {
      toast.error("Something went wrong");
    } finally {
      setIsSubmitting(false);
    }
  };
  return (
    <form className="mx-auto mt-8 max-w-2xl px-4 space-y-6" onSubmit={handleSubmit(onSubmit)}>
      <h1 className="text-2xl font-semibold">Create app</h1>
      <div className="space-y-2">
        <Label htmlFor="name">Name</Label>
        <Input
          type="text"
          id="name"
          {...register("name")}
        />
        {errors.name ? (
          <p className="text-sm text-destructive">{errors.name.message}</p>
        ) : (
          <p className="text-sm text-muted-foreground">You can change this later</p>
        )}
      </div>
      <div className="space-y-2">
        <Label htmlFor="description">Description</Label>
        <textarea
          id="description"
          rows={4}
          {...register("description")}
          className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-base shadow-sm transition-colors placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 md:text-sm"
        />
        {errors.description && (
          <p className="text-sm text-destructive">{errors.description.message}</p>
        )}
      </div>
      <div className="flex justify-end gap-2">
        <Button
          type="button"
          variant="outline"
          onClick={() => navigate("/apps")}
          disabled={isSubmitting}
        >
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Create App
        </Button>
      </div>
    </form>
  );
}
export default AppCreate;
