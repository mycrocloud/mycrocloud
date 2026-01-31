import { useEffect, useState } from "react";
import { toast } from "react-toastify";
import { useForm } from "react-hook-form";
import { useApiClient } from "@/hooks";
import { Plus, Trash2, Ban, Key } from "lucide-react";
import TextCopyButton from "../../components/ui/TextCopyButton";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreVertical } from "lucide-react";

interface IToken {
  id: number;
  name: string;
  token: string;
  status: "None" | "Revoked";
}

type CreateType = {
  name: string;
};

export default function Tokens() {
  const { get, post, del } = useApiClient();

  const [tokens, setTokens] = useState<IToken[]>([]);
  const [isLoading, setIsLoading] = useState(true);

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

  const [showCreateModal, setShowCreateModal] = useState(false);
  const {
    handleSubmit,
    register,
    formState: { errors },
    reset,
  } = useForm<CreateType>();

  const onCreateClickHandler = async (data: CreateType) => {
    try {
      const token = await post<IToken>("/api/usersettings/tokens", {
        name: data.name,
      });
      setTokens([...tokens, token]);
      reset();
      setShowCreateModal(false);
      toast.success("Token created successfully");
    } catch (error) {
      toast.error("Something went wrong.");
    }
  };

  const _delete = async (id: number) => {
    if (confirm("Are you sure you want to delete this token?")) {
      await del(`/api/usersettings/tokens/${id}`);
      setTokens(tokens.filter((t) => t.id !== id));
      toast.success("Token deleted");
    }
  };

  const revoke = async (id: number) => {
    if (confirm("Are you sure you want to revoke this token?")) {
      await post(`/api/usersettings/tokens/${id}/revoke`);
      setTokens((tokens) =>
        tokens.map((t) => (t.id === id ? { ...t, status: "Revoked" } : t))
      );
      toast.success("Token revoked");
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
          <Button onClick={() => setShowCreateModal(true)} className="gap-2">
            <Plus className="h-4 w-4" />
            Generate Token
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex items-center justify-center py-8 text-muted-foreground">
            Loading...
          </div>
        ) : tokens.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted">
              <Key className="h-6 w-6 text-muted-foreground" />
            </div>
            <h3 className="mt-4 font-medium">No tokens yet</h3>
            <p className="mt-1 text-sm text-muted-foreground">
              Generate your first API token to get started
            </p>
            <Button
              onClick={() => setShowCreateModal(true)}
              className="mt-4 gap-2"
              variant="outline"
            >
              <Plus className="h-4 w-4" />
              Generate Token
            </Button>
          </div>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Token</TableHead>
                <TableHead>Status</TableHead>
                <TableHead className="w-[50px]"></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {tokens.map((t) => (
                <TableRow key={t.id}>
                  <TableCell className="font-medium">{t.name}</TableCell>
                  <TableCell>
                    <div className="flex items-center gap-2">
                      <code className="rounded bg-muted px-2 py-1 text-sm">
                        {t.token.substring(0, 20)}...
                      </code>
                      <TextCopyButton text={t.token} />
                    </div>
                  </TableCell>
                  <TableCell>
                    {t.status === "Revoked" ? (
                      <Badge variant="destructive">Revoked</Badge>
                    ) : (
                      <Badge variant="secondary">Active</Badge>
                    )}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button variant="ghost" size="icon" className="h-8 w-8">
                          <MoreVertical className="h-4 w-4" />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem
                          onClick={() => revoke(t.id)}
                          disabled={t.status === "Revoked"}
                        >
                          <Ban className="mr-2 h-4 w-4" />
                          Revoke
                        </DropdownMenuItem>
                        <DropdownMenuSeparator />
                        <DropdownMenuItem
                          onClick={() => _delete(t.id)}
                          className="text-destructive focus:text-destructive"
                        >
                          <Trash2 className="mr-2 h-4 w-4" />
                          Delete
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </CardContent>

      <Dialog open={showCreateModal} onOpenChange={setShowCreateModal}>
        <DialogContent>
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
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowCreateModal(false)}
              >
                Cancel
              </Button>
              <Button type="submit">Generate</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
