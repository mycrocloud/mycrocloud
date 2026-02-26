import { useContext, useEffect, useState } from "react";
import { AppContext } from "..";
import { useFieldArray, useForm } from "react-hook-form";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import { DndContext, DragEndEvent } from "@dnd-kit/core";
import { SortableContext, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
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
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  Globe,
  Pencil,
  Trash2,
  Plus,
  ArrowLeft,
  ArrowRight,
  Info,
  Loader2,
  Shield,
  Key,
  GripVertical,
  CheckCircle2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import {
  CorsSettings,
  CorsFormInputs,
  IScheme,
  SchemeFormInputs,
  AuthOrderInputs,
} from "./types";

const HTTP_METHODS = [
  "GET",
  "POST",
  "PUT",
  "PATCH",
  "DELETE",
  "OPTIONS",
  "HEAD",
];

const schemeFormSchema = yup.object({
  name: yup.string().required("Name is required"),
  type: yup.string().required("Type is required"),
  openIdConnectIssuer: yup.string(),
  openIdConnectAudience: yup.string(),
});

export default function ApiTab() {
  return (
    <>
      <CorsSettingsSection />
      <AuthenticationSection />
    </>
  );
}

function CorsSettingsSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const { register, handleSubmit, setValue, watch } = useForm<CorsFormInputs>({
    defaultValues: {
      allowedOrigins: "",
      allowedMethods: "",
      allowedHeaders: "",
      exposeHeaders: "",
      maxAgeSeconds: "",
    },
  });

  const watchMethods = watch("allowedMethods");
  const selectedMethods = watchMethods
    ? watchMethods.split(",").filter(Boolean)
    : [];

  useEffect(() => {
    const fetchCorsSettings = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/cors`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        const data: CorsSettings = await res.json();
        setValue("allowedOrigins", data.allowedOrigins?.join(", ") || "");
        setValue("allowedMethods", data.allowedMethods?.join(",") || "");
        setValue("allowedHeaders", data.allowedHeaders?.join(", ") || "");
        setValue("exposeHeaders", data.exposeHeaders?.join(", ") || "");
        setValue("maxAgeSeconds", data.maxAgeSeconds?.toString() || "");
      }
      setLoading(false);
    };

    fetchCorsSettings();
  }, [app.id, getAccessTokenSilently, setValue]);

  const toggleMethod = (method: string) => {
    const current = selectedMethods;
    if (current.includes(method)) {
      setValue(
        "allowedMethods",
        current.filter((m) => m !== method).join(",")
      );
    } else {
      setValue("allowedMethods", [...current, method].join(","));
    }
  };

  const onSubmit = async (data: CorsFormInputs) => {
    setSaving(true);
    const parseArray = (str: string) =>
      str
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean);

    const payload: CorsSettings = {
      allowedOrigins: parseArray(data.allowedOrigins),
      allowedMethods: parseArray(data.allowedMethods),
      allowedHeaders: parseArray(data.allowedHeaders),
      exposeHeaders: parseArray(data.exposeHeaders),
      maxAgeSeconds: data.maxAgeSeconds
        ? parseInt(data.maxAgeSeconds)
        : undefined,
    };

    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/cors`, {
      method: "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(payload),
    });
    setSaving(false);
    if (res.ok) {
      toast("CORS settings saved");
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Globe className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">CORS Settings</CardTitle>
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
        <div className="flex items-center gap-2">
          <Globe className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">CORS Settings</CardTitle>
        </div>
        <CardDescription>
          Configure Cross-Origin Resource Sharing for your API
        </CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
          <div className="space-y-2">
            <Label htmlFor="allowedOrigins">Allowed Origins</Label>
            <Input
              id="allowedOrigins"
              {...register("allowedOrigins")}
              placeholder="https://example.com, https://app.example.com"
            />
            <p className="text-xs text-muted-foreground">
              Comma-separated list of allowed origins. Use * to allow all
              origins.
            </p>
          </div>

          <div className="space-y-2">
            <Label>Allowed Methods</Label>
            <div className="flex flex-wrap gap-2">
              {HTTP_METHODS.map((method) => (
                <Button
                  key={method}
                  type="button"
                  variant={
                    selectedMethods.includes(method) ? "default" : "outline"
                  }
                  size="sm"
                  onClick={() => toggleMethod(method)}
                  className="font-mono text-xs"
                >
                  {method}
                </Button>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="allowedHeaders">Allowed Headers</Label>
            <Input
              id="allowedHeaders"
              {...register("allowedHeaders")}
              placeholder="Content-Type, Authorization, X-Custom-Header"
            />
            <p className="text-xs text-muted-foreground">
              Comma-separated list of allowed request headers.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="exposeHeaders">Expose Headers</Label>
            <Input
              id="exposeHeaders"
              {...register("exposeHeaders")}
              placeholder="X-Request-Id, X-Response-Time"
            />
            <p className="text-xs text-muted-foreground">
              Comma-separated list of headers exposed to the browser.
            </p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="maxAgeSeconds">Max Age (seconds)</Label>
            <Input
              id="maxAgeSeconds"
              type="number"
              {...register("maxAgeSeconds")}
              placeholder="86400"
              className="w-32"
            />
            <p className="text-xs text-muted-foreground">
              How long the preflight response can be cached.
            </p>
          </div>

          <Button type="submit" disabled={saving}>
            {saving && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Save Changes
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function AuthenticationSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const [schemes, setSchemes] = useState<IScheme[]>([]);
  const [loading, setLoading] = useState(true);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingScheme, setEditingScheme] = useState<IScheme | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    setValue,
    setError,
    watch,
  } = useForm<SchemeFormInputs>({
    resolver: yupResolver(schemeFormSchema),
    defaultValues: {
      name: "",
      type: "OpenIdConnect",
      openIdConnectIssuer: "",
      openIdConnectAudience: "",
    },
  });

  const watchType = watch("type");

  const fetchSchemes = async () => {
    try {
      const accessToken = await getAccessTokenSilently();
      const data = (await (
        await fetch(`/api/apps/${app.id}/authentications/schemes`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        })
      ).json()) as IScheme[];
      setSchemes(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSchemes();
  }, []);

  const handleCreateClick = () => {
    setEditingScheme(null);
    reset({
      name: "",
      type: "OpenIdConnect",
      openIdConnectIssuer: "",
      openIdConnectAudience: "",
    });
    setDialogOpen(true);
  };

  const handleEditClick = (scheme: IScheme) => {
    setEditingScheme(scheme);
    reset({
      name: scheme.name,
      type: scheme.type,
      openIdConnectIssuer: scheme.openIdConnectAuthority || "",
      openIdConnectAudience: scheme.openIdConnectAudience || "",
    });
    setDialogOpen(true);
  };

  const handleDeleteClick = async (id: number) => {
    if (!confirm("Are you sure you want to delete this scheme?")) return;
    try {
      const accessToken = await getAccessTokenSilently();
      await fetch(`/api/apps/${app.id}/authentications/schemes/${id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      toast("Scheme deleted");
      fetchSchemes();
    } catch {
      toast.error("Failed to delete scheme");
    }
  };

  const onSubmitScheme = async (data: SchemeFormInputs) => {
    if (data.type === "OpenIdConnect" && data.openIdConnectIssuer) {
      try {
        const issuer = data.openIdConnectIssuer.replace(/\/$/, "");
        const res = await fetch(`${issuer}/.well-known/openid-configuration`);
        if (!res.ok) throw new Error("Invalid issuer");
      } catch {
        setError("openIdConnectIssuer", { message: "Invalid issuer" });
        return;
      }
    }

    const submitData = {
      name: data.name,
      type: data.type,
      openIdConnectAuthority: data.openIdConnectIssuer,
      openIdConnectAudience: data.openIdConnectAudience,
    };

    try {
      const accessToken = await getAccessTokenSilently();
      const url = editingScheme
        ? `/api/apps/${app.id}/authentications/schemes/${editingScheme.id}`
        : `/api/apps/${app.id}/authentications/schemes`;
      const res = await fetch(url, {
        method: editingScheme ? "PUT" : "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(submitData),
      });
      if (res.ok) {
        toast(editingScheme ? "Scheme updated" : "Scheme created");
        setDialogOpen(false);
        fetchSchemes();
      } else {
        toast.error("Failed to save scheme");
      }
    } catch {
      toast.error("Failed to save scheme");
    }
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Shield className="h-4 w-4 text-muted-foreground" />
            <CardTitle className="text-base">Authentication Schemes</CardTitle>
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
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <div className="flex items-center gap-2">
              <Shield className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-base">
                Authentication Schemes
              </CardTitle>
            </div>
            <CardDescription>
              Manage authentication schemes for your API routes
            </CardDescription>
          </div>
          <Button size="sm" onClick={handleCreateClick}>
            <Plus className="mr-1 h-4 w-4" />
            New Scheme
          </Button>
        </CardHeader>
        <CardContent>
          {schemes.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 text-center">
              <Key className="h-10 w-10 text-muted-foreground/50" />
              <p className="mt-2 text-sm text-muted-foreground">
                No authentication schemes configured
              </p>
              <p className="text-xs text-muted-foreground">
                Click "New Scheme" to add one
              </p>
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {schemes.map((scheme) => (
                  <TableRow key={scheme.id}>
                    <TableCell className="font-medium">{scheme.name}</TableCell>
                    <TableCell>
                      <Badge variant="secondary" className="font-mono text-xs">
                        {scheme.type}
                      </Badge>
                    </TableCell>
                    <TableCell>
                      <Badge
                        variant="secondary"
                        className={cn(
                          scheme.enabled
                            ? "bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400"
                            : "bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400"
                        )}
                      >
                        {scheme.enabled ? "Enabled" : "Disabled"}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-muted-foreground">
                      {new Date(scheme.createdAt).toLocaleDateString()}
                    </TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleEditClick(scheme)}
                        >
                          <Pencil className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDeleteClick(scheme.id)}
                          disabled={scheme.enabled}
                        >
                          <Trash2
                            className={cn(
                              "h-4 w-4",
                              scheme.enabled
                                ? "text-muted"
                                : "text-destructive"
                            )}
                          />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <AuthOrderSection schemes={schemes} onUpdate={fetchSchemes} />

      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingScheme ? "Edit Scheme" : "Create Scheme"}
            </DialogTitle>
            <DialogDescription>
              {editingScheme
                ? "Update the authentication scheme settings."
                : "Add a new authentication scheme."}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmitScheme)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input id="name" {...register("name")} />
              {errors.name && (
                <p className="text-sm text-destructive">
                  {errors.name.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="type">Type</Label>
              <Select
                value={watch("type")}
                onValueChange={(value) => setValue("type", value)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="OpenIdConnect">OpenID Connect</SelectItem>
                </SelectContent>
              </Select>
            </div>
            {watchType === "OpenIdConnect" && (
              <>
                <div className="space-y-2">
                  <Label htmlFor="issuer">Issuer</Label>
                  <Input
                    id="issuer"
                    {...register("openIdConnectIssuer")}
                    placeholder="https://example.auth0.com/"
                  />
                  {errors.openIdConnectIssuer && (
                    <p className="text-sm text-destructive">
                      {errors.openIdConnectIssuer.message}
                    </p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="audience">Audience</Label>
                  <Input
                    id="audience"
                    {...register("openIdConnectAudience")}
                    placeholder="https://api.example.com"
                  />
                  {errors.openIdConnectAudience && (
                    <p className="text-sm text-destructive">
                      {errors.openIdConnectAudience.message}
                    </p>
                  )}
                </div>
              </>
            )}
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setDialogOpen(false)}
              >
                Cancel
              </Button>
              <Button type="submit">Save</Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </>
  );
}

function AuthOrderSection({
  schemes,
  onUpdate,
}: {
  schemes: IScheme[];
  onUpdate: () => void;
}) {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();

  const { handleSubmit, control, setValue } = useForm<AuthOrderInputs>();

  const { fields, append, swap, remove } = useFieldArray({
    control,
    name: "schemes",
    keyName: "fieldId",
  });

  const availableSchemes = schemes.filter(
    (s) => fields.findIndex((sf) => sf.id === s.id) === -1
  );

  useEffect(() => {
    setValue(
      "schemes",
      schemes.filter((s) => s.enabled)
    );
  }, [schemes, setValue]);

  const onSubmit = async (data: AuthOrderInputs) => {
    const schemeIds = data.schemes.map((s) => s.id);
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      `/api/apps/${app.id}/authentications/schemes/settings`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify(schemeIds),
      }
    );
    if (res.ok) {
      toast("Authentication order saved");
      onUpdate();
    }
  };

  const handleDragEnd = (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;
    if (active.id !== over.id) {
      const activeIndex = fields.findIndex((f) => f.id === active.id);
      const overIndex = fields.findIndex((f) => f.id === over.id);
      swap(activeIndex, overIndex);
    }
  };

  const handleDisableClick = (id: number) => {
    const index = fields.findIndex((f) => f.id === id);
    remove(index);
  };

  const handleEnableClick = (id: number) => {
    const scheme = schemes.find((s) => s.id === id)!;
    append(scheme);
  };

  if (schemes.length === 0) return null;

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center gap-2">
          <Key className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">Authentication Order</CardTitle>
        </div>
        <CardDescription>
          Enable/disable and reorder authentication schemes
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Alert className="mb-4">
          <Info className="h-4 w-4" />
          <AlertDescription>
            Use the arrows to enable/disable schemes. Drag to reorder enabled
            schemes.
          </AlertDescription>
        </Alert>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="grid gap-4 md:grid-cols-2">
            <div className="rounded-lg border p-4">
              <h3 className="mb-3 flex items-center gap-2 text-sm font-medium">
                <CheckCircle2 className="h-4 w-4 text-green-500" />
                Enabled Schemes
              </h3>
              <DndContext onDragEnd={handleDragEnd}>
                <SortableContext items={fields}>
                  <div className="min-h-[100px] space-y-2">
                    {fields.length ? (
                      fields.map((scheme) => (
                        <SortableSchemeItem
                          key={scheme.id}
                          scheme={scheme}
                          onDisableClick={handleDisableClick}
                        />
                      ))
                    ) : (
                      <p className="py-4 text-center text-sm text-muted-foreground">
                        No schemes enabled
                      </p>
                    )}
                  </div>
                </SortableContext>
              </DndContext>
            </div>
            <div className="rounded-lg border p-4">
              <h3 className="mb-3 text-sm font-medium text-muted-foreground">
                Available Schemes
              </h3>
              <div className="min-h-[100px] space-y-2">
                {availableSchemes.length ? (
                  availableSchemes.map((s) => (
                    <AvailableSchemeItem
                      key={s.id}
                      scheme={s}
                      onEnableClick={handleEnableClick}
                    />
                  ))
                ) : (
                  <p className="py-4 text-center text-sm text-muted-foreground">
                    All schemes enabled
                  </p>
                )}
              </div>
            </div>
          </div>
          <Button type="submit" className="mt-4">
            Save Order
          </Button>
        </form>
      </CardContent>
    </Card>
  );
}

function SortableSchemeItem({
  scheme,
  onDisableClick,
}: {
  scheme: IScheme;
  onDisableClick: (id: number) => void;
}) {
  const { attributes, listeners, setNodeRef, transform, transition } =
    useSortable({
      id: scheme.id,
    });
  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };
  return (
    <div style={style} className="flex items-center gap-2">
      <div
        ref={setNodeRef}
        {...attributes}
        {...listeners}
        className="flex flex-1 cursor-grab items-center gap-2 rounded-md border bg-muted/50 px-3 py-2 text-sm"
      >
        <GripVertical className="h-4 w-4 text-muted-foreground" />
        {scheme.name}
      </div>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        onClick={() => onDisableClick(scheme.id)}
      >
        <ArrowRight className="h-4 w-4" />
      </Button>
    </div>
  );
}

function AvailableSchemeItem({
  scheme,
  onEnableClick,
}: {
  scheme: IScheme;
  onEnableClick: (id: number) => void;
}) {
  return (
    <div className="flex items-center gap-2">
      <Button
        type="button"
        variant="ghost"
        size="icon"
        onClick={() => onEnableClick(scheme.id)}
      >
        <ArrowLeft className="h-4 w-4" />
      </Button>
      <div className="flex-1 rounded-md border px-3 py-2 text-sm">
        {scheme.name}
      </div>
    </div>
  );
}
