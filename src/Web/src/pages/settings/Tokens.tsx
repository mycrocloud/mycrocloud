import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { useApiClient } from "@/hooks";
import { Plus, Trash2, Key, Copy, Check, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";

interface IToken {
  id: number;
  name: string;
  token?: string;
}

type CreateType = {
  name: string;
};

type DialogStep = "form" | "created";

export default function Tokens() {
  const { get, post, del } = useApiClient();
  const [tokens, setTokens] = useState<IToken[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [dialogStep, setDialogStep] = useState<DialogStep>("form");
  const [createdToken, setCreatedToken] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const {
    handleSubmit,
    register,
    formState: { errors },
    reset,
  } = useForm<CreateType>();

  useEffect(() => {
    (async () => {
      try {
        const tokens = await get<IToken[]>("/api/usersettings/tokens");
        setTokens(tokens);
      } finally {
        setIsLoading(false);
      }
    })();
  }, []);

  const handleOpenDialog = () => {
    setDialogStep("form");
    setCreatedToken(null);
    setCopied(false);
    reset();
    setDialogOpen(true);
  };

  const handleCloseDialog = () => {
    setDialogOpen(false);
  };

  const onCreateClickHandler = async (data: CreateType) => {
    try {
      const token = await post<IToken>("/api/usersettings/tokens", {
        name: data.name,
      });
      setTokens([...tokens, { id: token.id, name: token.name }]);
      setCreatedToken(token.token || "");
      setDialogStep("created");
    } catch (error) {
      toast.error("Something went wrong.");
    }
  };

  const handleCopy = () => {
    if (createdToken) {
      navigator.clipboard.writeText(createdToken);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleDelete = async (id: number) => {
    if (confirm("Are you sure you want to delete this token?")) {
      await del(`/api/usersettings/tokens/${id}`);
      setTokens(tokens.filter((t) => t.id !== id));
      toast.success("Token deleted");
    }
  };

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>API Tokens</CardTitle>
            <CardDescription>
              Generate and manage API tokens for programmatic access
            </CardDescription>
          </div>
          <Button onClick={handleOpenDialog} size="sm" className="gap-2">
            <Plus className="h-4 w-4" />
            Generate
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex items-center justify-center py-8 text-muted-foreground">
            Loading...
          </div>
        ) : tokens.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <Key className="h-8 w-8 text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">
              No tokens yet
            </p>
          </div>
        ) : (
          <div className="space-y-2">
            {tokens.map((t) => (
              <div
                key={t.id}
                className="flex items-center justify-between rounded-lg border p-3"
              >
                <div className="flex items-center gap-3">
                  <Key className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">{t.name}</span>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-muted-foreground hover:text-destructive"
                  onClick={() => handleDelete(t.id)}
                >
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        )}
      </CardContent>

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          {dialogStep === "form" ? (
            <>
              <DialogHeader>
                <DialogTitle>Generate New Token</DialogTitle>
                <DialogDescription>
                  Create a new API token to authenticate your requests
                </DialogDescription>
              </DialogHeader>
              <form onSubmit={handleSubmit(onCreateClickHandler)}>
                <div className="space-y-4 py-4">
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
                </div>
                <DialogFooter>
                  <Button type="button" variant="outline" onClick={handleCloseDialog}>
                    Cancel
                  </Button>
                  <Button type="submit">Generate</Button>
                </DialogFooter>
              </form>
            </>
          ) : (
            <>
              <DialogHeader>
                <DialogTitle>Token Created</DialogTitle>
                <DialogDescription>
                  Your new API token has been generated
                </DialogDescription>
              </DialogHeader>
              <div className="space-y-4 py-4">
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
                      value={createdToken || ""}
                      className="font-mono text-sm"
                    />
                    <Button
                      type="button"
                      variant="outline"
                      size="icon"
                      onClick={handleCopy}
                    >
                      {copied ? (
                        <Check className="h-4 w-4 text-green-600" />
                      ) : (
                        <Copy className="h-4 w-4" />
                      )}
                    </Button>
                  </div>
                </div>
              </div>
              <DialogFooter>
                <Button onClick={handleCloseDialog}>Done</Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </Card>
  );
}
