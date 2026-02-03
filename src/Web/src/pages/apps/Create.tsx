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
import { cn } from "@/lib/utils";
import { AppWindow, Globe, Server } from "lucide-react";


type Inputs = {
  name: string;
  description?: string;
  type: "FullStack" | "SPA" | "API";
};


function AppCreate() {
  const navigate = useNavigate();
  const { getAccessTokenSilently } = useAuth0();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const schema = yup.object({
    name: yup.string().required("Name is required"),
    description: yup.string(),
    type: yup.string().oneOf(["FullStack", "SPA", "API"]).required() as yup.Schema<"FullStack" | "SPA" | "API">,
  });
  const {
    register,
    handleSubmit,
    setError,
    setValue,
    watch,
    formState: { errors },
  } = useForm<Inputs>({
    resolver: yupResolver(schema),
    defaultValues: {
      name: generateAppName(),
      type: "FullStack",
    },
  });

  const selectedType = watch("type");

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
        navigate(`../${id}?onboard=true&type=${data.type}`);
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

      <div className="space-y-4">
        <div>
          <Label>App Type</Label>
          <p className="text-sm text-muted-foreground">
            We use this to set up the initial routing for your app. You can change this configuration later in the app settings.
          </p>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          <AppTypeCard
            title="Full-stack App"
            description="Standard web application. Handles both static files and API routes."
            icon={AppWindow}
            selected={selectedType === "FullStack"}
            onClick={() => setValue("type", "FullStack")}
          />
          <AppTypeCard
            title="SPA"
            description="For apps like React, Vue, etc. Redirects all unknown requests to index.html."
            icon={Globe}
            selected={selectedType === "SPA"}
            onClick={() => setValue("type", "SPA")}
          />
          <AppTypeCard
            title="API Backend"
            description="Optimized for backend services that only provide API endpoints."
            icon={Server}
            selected={selectedType === "API"}
            onClick={() => setValue("type", "API")}
          />
        </div>
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

function AppTypeCard({
  title,
  description,
  icon: Icon,
  selected,
  onClick,
}: {
  title: string;
  description: string;
  icon: any;
  selected: boolean;
  onClick: () => void;
}) {
  return (
    <div
      onClick={onClick}
      className={cn(
        "cursor-pointer rounded-lg border p-4 transition-all hover:border-primary",
        selected
          ? "border-primary bg-primary/5 ring-1 ring-primary"
          : "bg-card text-card-foreground shadow-sm"
      )}
    >
      <div className="mb-2 flex items-center gap-2 font-semibold">
        <Icon className={cn("h-5 w-5", selected ? "text-primary" : "text-muted-foreground")} />
        {title}
      </div>
      <p className="text-xs text-muted-foreground">{description}</p>
    </div>
  );
}

