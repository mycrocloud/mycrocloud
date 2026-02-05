import { useContext, useState, useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { toast } from "react-toastify";
import { AppContext } from "..";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Loader2,
  Key,
  Plus,
  Trash2,
  Eye,
  EyeOff,
  Filter,
} from "lucide-react";
import { useApiClient } from "@/hooks";
import {
  IEnvVariable,
  IEnvVariableCreate,
  VARIABLE_TARGETS,
  VariableTarget,
} from "./types";

type FilterTarget = "all" | VariableTarget;

export default function EnvironmentTab() {
  const { get, post, del } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [variables, setVariables] = useState<IEnvVariable[]>([]);
  const [filteredVariables, setFilteredVariables] = useState<IEnvVariable[]>([]);
  const [filterTarget, setFilterTarget] = useState<FilterTarget>("all");
  const [showAddDialog, setShowAddDialog] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState<IEnvVariable | null>(null);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [visibleSecrets, setVisibleSecrets] = useState<Set<number>>(new Set());

  const {
    register,
    handleSubmit,
    reset,
    control,
    formState: { errors },
  } = useForm<IEnvVariableCreate>({
    defaultValues: {
      name: "",
      value: "",
      target: "Build",
      isSecret: false,
    },
  });

  useEffect(() => {
    fetchVariables();
  }, [app.id]);

  useEffect(() => {
    if (filterTarget === "all") {
      setFilteredVariables(variables);
    } else {
      setFilteredVariables(
        variables.filter((v) => v.target === filterTarget || v.target === "All")
      );
    }
  }, [variables, filterTarget]);

  const fetchVariables = async () => {
    try {
      const data = await get<IEnvVariable[]>(`/api/apps/${app.id}/variables`);
      setVariables(data);
    } finally {
      setLoading(false);
    }
  };

  const onSubmit = async (data: IEnvVariableCreate) => {
    setSaving(true);
    try {
      await post(`/api/apps/${app.id}/variables`, data);
      toast.success("Environment variable added");
      setShowAddDialog(false);
      reset();
      fetchVariables();
    } catch (err) {
      console.error("Failed to add environment variable:", err);
      toast.error("Failed to add environment variable");
    } finally {
      setSaving(false);
    }
  };

  const onDelete = async () => {
    if (!showDeleteDialog) return;
    setDeleting(true);
    try {
      await del(`/api/apps/${app.id}/variables/${showDeleteDialog.id}`);
      toast.success("Environment variable deleted");
      setShowDeleteDialog(null);
      fetchVariables();
    } catch (err) {
      console.error("Failed to delete environment variable:", err);
      toast.error("Failed to delete environment variable");
    } finally {
      setDeleting(false);
    }
  };

  const toggleSecretVisibility = (id: number) => {
    setVisibleSecrets((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const getTargetBadgeClass = (target: VariableTarget) => {
    switch (target) {
      case "Build":
        return "bg-blue-500/10 text-blue-600";
      case "Runtime":
        return "bg-green-500/10 text-green-600";
      case "All":
        return "bg-purple-500/10 text-purple-600";
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Key className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">Environment Variables</CardTitle>
          </div>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            Loading...
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between">
          <div>
            <div className="flex items-center gap-2">
              <Key className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-base">Environment Variables</CardTitle>
            </div>
            <CardDescription>
              Manage environment variables for builds and functions
            </CardDescription>
          </div>
          <Button variant="outline" size="sm" onClick={() => setShowAddDialog(true)}>
            <Plus className="mr-2 h-4 w-4" />
            Add
          </Button>
        </div>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Filter */}
        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-muted-foreground" />
          <Select
            value={filterTarget}
            onValueChange={(v) => setFilterTarget(v as FilterTarget)}
          >
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Targets</SelectItem>
              <SelectItem value="Build">Build</SelectItem>
              <SelectItem value="Runtime">Runtime</SelectItem>
              <SelectItem value="All">Build + Runtime</SelectItem>
            </SelectContent>
          </Select>
          <span className="text-sm text-muted-foreground">
            {filteredVariables.length} variable{filteredVariables.length !== 1 ? "s" : ""}
          </span>
        </div>

        {/* Variables List */}
        {filteredVariables.length === 0 ? (
          <p className="text-sm text-muted-foreground py-4">
            {variables.length === 0
              ? "No environment variables configured. Add variables for builds or functions."
              : "No variables match the current filter."}
          </p>
        ) : (
          <div className="space-y-2">
            {filteredVariables.map((variable) => (
              <div
                key={variable.id}
                className="flex items-center justify-between rounded-lg border p-3"
              >
                <div className="flex items-center gap-3">
                  <code className="text-sm font-medium">{variable.name}</code>
                  <span
                    className={`rounded px-1.5 py-0.5 text-xs ${getTargetBadgeClass(variable.target)}`}
                  >
                    {variable.target === "All" ? "Build + Runtime" : variable.target}
                  </span>
                  {variable.isSecret && (
                    <span className="rounded bg-yellow-500/10 px-1.5 py-0.5 text-xs text-yellow-600">
                      Secret
                    </span>
                  )}
                </div>
                <div className="flex items-center gap-2">
                  {variable.isSecret ? (
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => toggleSecretVisibility(variable.id)}
                    >
                      {visibleSecrets.has(variable.id) ? (
                        <EyeOff className="h-4 w-4" />
                      ) : (
                        <Eye className="h-4 w-4" />
                      )}
                    </Button>
                  ) : null}
                  <code className="max-w-[200px] truncate text-sm text-muted-foreground">
                    {variable.isSecret && !visibleSecrets.has(variable.id)
                      ? "••••••••"
                      : variable.value}
                  </code>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setShowDeleteDialog(variable)}
                  >
                    <Trash2 className="h-4 w-4 text-destructive" />
                  </Button>
                </div>
              </div>
            ))}
          </div>
        )}

        {/* Usage Info */}
        <div className="rounded-lg bg-muted/50 p-3 text-sm text-muted-foreground">
          <p className="font-medium mb-1">Variable Targets:</p>
          <ul className="list-disc list-inside space-y-1">
            <li><span className="text-blue-600">Build</span> - Available during static site builds (e.g., VITE_API_URL)</li>
            <li><span className="text-green-600">Runtime</span> - Available in serverless functions (e.g., DATABASE_URL)</li>
            <li><span className="text-purple-600">Build + Runtime</span> - Available in both contexts</li>
          </ul>
        </div>
      </CardContent>

      {/* Add Variable Dialog */}
      <Dialog open={showAddDialog} onOpenChange={setShowAddDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Add Environment Variable</DialogTitle>
            <DialogDescription>
              Add a new environment variable for your app.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)}>
            <div className="space-y-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="name">Name</Label>
                <Input
                  id="name"
                  {...register("name", { required: "Name is required" })}
                  placeholder="VARIABLE_NAME"
                  className="font-mono"
                />
                {errors.name && (
                  <p className="text-sm text-destructive">{errors.name.message}</p>
                )}
              </div>
              <div className="space-y-2">
                <Label htmlFor="value">Value</Label>
                <Input
                  id="value"
                  {...register("value")}
                  placeholder="value"
                  className="font-mono"
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="target">Target</Label>
                <Controller
                  name="target"
                  control={control}
                  render={({ field }) => (
                    <Select value={field.value} onValueChange={field.onChange}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        {VARIABLE_TARGETS.map((t) => (
                          <SelectItem key={t.value} value={t.value}>
                            <div>
                              <span>{t.label}</span>
                              <span className="ml-2 text-xs text-muted-foreground">
                                ({t.description})
                              </span>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  )}
                />
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="isSecret"
                  {...register("isSecret")}
                  className="h-4 w-4 rounded border-gray-300"
                />
                <Label htmlFor="isSecret" className="text-sm font-normal">
                  Mark as secret (value will be hidden)
                </Label>
              </div>
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowAddDialog(false)}
                disabled={saving}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={saving}>
                {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Add Variable
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!showDeleteDialog} onOpenChange={() => setShowDeleteDialog(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete Environment Variable</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this variable?
            </DialogDescription>
          </DialogHeader>
          {showDeleteDialog && (
            <div className="py-4">
              <div className="rounded-lg border p-3">
                <code className="text-sm font-medium">{showDeleteDialog.name}</code>
              </div>
              <p className="mt-3 text-sm text-muted-foreground">
                This action cannot be undone.
              </p>
            </div>
          )}
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeleteDialog(null)}
              disabled={deleting}
            >
              Cancel
            </Button>
            <Button variant="destructive" onClick={onDelete} disabled={deleting}>
              {deleting && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  );
}
