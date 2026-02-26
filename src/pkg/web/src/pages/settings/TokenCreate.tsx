import { useState } from "react";
import { Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { toast } from "react-toastify";
import { useApiClient } from "@/hooks";
import { ArrowLeft, Copy, Check, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

interface IToken {
  id: number;
  name: string;
  token?: string;
}

type FormData = {
  name: string;
};

type Step = "form" | "created";

export default function TokenCreate() {
  const { post } = useApiClient();
  const [step, setStep] = useState<Step>("form");
  const [createdToken, setCreatedToken] = useState<string>("");
  const [copied, setCopied] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    handleSubmit,
    register,
    formState: { errors },
  } = useForm<FormData>();

  const onSubmit = async (data: FormData) => {
    setIsSubmitting(true);
    try {
      const token = await post<IToken>("/api/usersettings/tokens", {
        name: data.name,
      });
      setCreatedToken(token.token || "");
      setStep("created");
    } catch (error) {
      toast.error("Something went wrong.");
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCopy = () => {
    navigator.clipboard.writeText(createdToken);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  if (step === "created") {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Token Created</CardTitle>
          <CardDescription>
            Your new API token has been generated
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              Make sure to copy your token now. You won't be able to see it again!
            </AlertDescription>
          </Alert>

          <div className="space-y-2">
            <Label>Your API Token</Label>
            <div className="flex gap-2">
              <Input
                readOnly
                value={createdToken}
                className="font-mono text-sm"
              />
              <Button variant="outline" size="icon" onClick={handleCopy}>
                {copied ? (
                  <Check className="h-4 w-4 text-green-600" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </div>
          </div>

          <Button asChild className="w-full">
            <Link to="/settings/tokens">Done</Link>
          </Button>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-4">
          <Button variant="ghost" size="icon" asChild>
            <Link to="/settings/tokens">
              <ArrowLeft className="h-4 w-4" />
            </Link>
          </Button>
          <div>
            <CardTitle>Generate New Token</CardTitle>
            <CardDescription>
              Create a new API token to authenticate your requests
            </CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
          <div className="space-y-2">
            <Label htmlFor="name">Token Name</Label>
            <Input
              id="name"
              {...register("name", {
                required: "Name is required",
                maxLength: {
                  value: 50,
                  message: "Name cannot exceed 50 characters",
                },
              })}
              placeholder="e.g., Production API Key"
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>

          <div className="flex gap-3">
            <Button type="button" variant="outline" asChild>
              <Link to="/settings/tokens">Cancel</Link>
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Generating..." : "Generate Token"}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}
