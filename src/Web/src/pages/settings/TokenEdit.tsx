import { useEffect, useState } from "react";
import { useNavigate, useParams, Link } from "react-router-dom";
import { useForm } from "react-hook-form";
import { toast } from "react-toastify";
import { useApiClient } from "@/hooks";
import {
  ArrowLeft,
  Copy,
  Check,
  AlertTriangle,
  RefreshCw,
  Loader2,
} from "lucide-react";
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

export default function TokenEdit() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { get, patch, post } = useApiClient();

  const [token, setToken] = useState<IToken | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isRegenerating, setIsRegenerating] = useState(false);
  const [regeneratedToken, setRegeneratedToken] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  const {
    handleSubmit,
    register,
    formState: { errors },
    reset,
  } = useForm<FormData>();

  useEffect(() => {
    const fetchToken = async () => {
      try {
        const data = await get<IToken>(`/api/usersettings/tokens/${id}`);
        setToken(data);
        reset({ name: data.name });
      } catch (error) {
        toast.error("Token not found");
        navigate("/settings/tokens");
      } finally {
        setIsLoading(false);
      }
    };
    fetchToken();
  }, [id]);

  const onSubmit = async (data: FormData) => {
    setIsSaving(true);
    try {
      await patch(`/api/usersettings/tokens/${id}`, { name: data.name });
      toast.success("Token updated");
      navigate("/settings/tokens");
    } catch (error) {
      toast.error("Something went wrong.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleRegenerate = async () => {
    if (!confirm("Are you sure? The current token will stop working immediately.")) {
      return;
    }
    setIsRegenerating(true);
    try {
      const result = await post<IToken>(`/api/usersettings/tokens/${id}/regenerate`);
      setRegeneratedToken(result.token || "");
      toast.success("Token regenerated");
    } catch (error) {
      toast.error("Something went wrong.");
    } finally {
      setIsRegenerating(false);
    }
  };

  const handleCopy = () => {
    if (regeneratedToken) {
      navigator.clipboard.writeText(regeneratedToken);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="flex items-center justify-center py-12">
          <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
        </CardContent>
      </Card>
    );
  }

  // Show regenerated token
  if (regeneratedToken) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Token Regenerated</CardTitle>
          <CardDescription>Your token has been regenerated</CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <Alert variant="destructive">
            <AlertTriangle className="h-4 w-4" />
            <AlertDescription>
              Make sure to copy your new token now. You won't be able to see it again!
            </AlertDescription>
          </Alert>

          <div className="space-y-2">
            <Label>Your New API Token</Label>
            <div className="flex gap-2">
              <Input
                readOnly
                value={regeneratedToken}
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
            <CardTitle>Edit Token</CardTitle>
            <CardDescription>Update token name or regenerate</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent className="space-y-6">
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
            <Button type="submit" disabled={isSaving}>
              {isSaving ? "Saving..." : "Save Changes"}
            </Button>
          </div>
        </form>

        <div className="border-t pt-6">
          <h3 className="font-medium">Regenerate Token</h3>
          <p className="mt-1 text-sm text-muted-foreground">
            Generate a new token value. The current token will stop working immediately.
          </p>
          <Button
            variant="outline"
            className="mt-4 gap-2"
            onClick={handleRegenerate}
            disabled={isRegenerating}
          >
            <RefreshCw className={`h-4 w-4 ${isRegenerating ? "animate-spin" : ""}`} />
            {isRegenerating ? "Regenerating..." : "Regenerate Token"}
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
