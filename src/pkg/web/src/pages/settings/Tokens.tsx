import { useEffect, useState } from "react";
import { Link, Outlet, useLocation } from "react-router-dom";
import { toast } from "react-toastify";
import { useApiClient } from "@/hooks";
import { Plus, Trash2, Key, Loader2, ChevronRight } from "lucide-react";
import { Button } from "@/components/ui/button";
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
}

export default function Tokens() {
  const location = useLocation();
  const isChildRoute = location.pathname !== "/settings/tokens";

  // Show child route content (new, edit)
  if (isChildRoute) {
    return <Outlet />;
  }

  return <TokenList />;
}

function TokenList() {
  const { get, del } = useApiClient();
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

  const handleDelete = async (e: React.MouseEvent, id: number) => {
    e.preventDefault();
    e.stopPropagation();
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
          <Button asChild size="sm" className="gap-2">
            <Link to="new">
              <Plus className="h-4 w-4" />
              Generate
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-6 w-6 animate-spin text-muted-foreground" />
          </div>
        ) : tokens.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <Key className="h-8 w-8 text-muted-foreground" />
            <p className="mt-2 text-sm text-muted-foreground">No tokens yet</p>
          </div>
        ) : (
          <div className="space-y-2">
            {tokens.map((t) => (
              <Link
                key={t.id}
                to={`${t.id}/edit`}
                className="flex items-center justify-between rounded-lg border p-3 transition-colors hover:bg-muted"
              >
                <div className="flex items-center gap-3">
                  <Key className="h-4 w-4 text-muted-foreground" />
                  <span className="font-medium">{t.name}</span>
                </div>
                <div className="flex items-center gap-1">
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-muted-foreground hover:text-destructive"
                    onClick={(e) => handleDelete(e, t.id)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                  <ChevronRight className="h-4 w-4 text-muted-foreground" />
                </div>
              </Link>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
