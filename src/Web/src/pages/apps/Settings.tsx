import { useContext, useEffect, useRef, useState } from "react";
import { AppContext } from ".";
import { useFieldArray, useForm } from "react-hook-form";
import { useAuth0 } from "@auth0/auth0-react";
import { toast } from "react-toastify";
import { useNavigate } from "react-router-dom";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import * as yup from "yup";
import { yupResolver } from "@hookform/resolvers/yup";
import Ajv, { JSONSchemaType } from "ajv";
import { DndContext, DragEndEvent } from "@dnd-kit/core";
import { SortableContext, useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
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
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
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
import { ArrowLeftIcon, ArrowRightIcon, PencilIcon, TrashIcon, PlusIcon } from "@heroicons/react/24/solid";
import { AlertTriangle, Info, Loader2 } from "lucide-react";
import { useApiClient } from "@/hooks";
import { getConfig } from "@/config";
import { NotFoundError } from "@/errors";
import InfoIcon from "@/components/ui/InfoIcon";
import { IAppIntegration } from "./App";

const { GITHUB_APP_NAME } = getConfig();

export default function AppSettings() {
  return (
    <div className="p-4">
      <h2 className="mb-6 text-xl font-semibold">Settings</h2>
      <Tabs defaultValue="general" className="w-full">
        <TabsList className="grid w-full max-w-md grid-cols-3">
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="api">API</TabsTrigger>
          <TabsTrigger value="pages">Pages</TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="mt-6 space-y-6">
          <GeneralTab />
        </TabsContent>

        <TabsContent value="api" className="mt-6 space-y-6">
          <ApiTab />
        </TabsContent>

        <TabsContent value="pages" className="mt-6 space-y-6">
          <PagesTab />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function GeneralTab() {
  return (
    <>
      <RenameSection />
      <ChangeStateSection />
      <DeleteSection />
    </>
  );
}

function ApiTab() {
  return (
    <>
      <CorsSettingsSection />
      <AuthenticationSection />
    </>
  );
}

function PagesTab() {
  return (
    <>
      <GitHubLinkSection />
      <BuildSettingsSection />
    </>
  );
}

// ============ API Tab Components ============

interface CorsSettings {
  allowedHeaders?: string[];
  allowedMethods?: string[];
  allowedOrigins?: string[];
  exposeHeaders?: string[];
  maxAgeSeconds?: number;
}

const corsSettingsSchema: JSONSchemaType<CorsSettings> = {
  type: "object",
  properties: {
    allowedHeaders: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    allowedMethods: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    allowedOrigins: {
      type: "array",
      nullable: true,
      items: { type: "string" },
    },
    exposeHeaders: { type: "array", nullable: true, items: { type: "string" } },
    maxAgeSeconds: { type: "number", nullable: true },
  },
  additionalProperties: false,
};

function CorsSettingsSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();

  const editorElRef = useRef(null);
  const editor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
  const [error, setError] = useState<string>();

  useEffect(() => {
    editor.current?.dispose();

    editor.current = monaco.editor.create(editorElRef.current!, {
      language: "json",
      value: "",
      minimap: { enabled: false },
    });

    return () => {
      editor.current?.dispose();
    };
  }, []);

  useEffect(() => {
    if (!editor.current) {
      return;
    }
    const fetchCorsSettings = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/cors`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        const json = await res.json();
        editor.current!.setValue(JSON.stringify(json, null, 2));
      }
    };

    fetchCorsSettings();
  }, [editor.current]);

  const handleSaveClick = async () => {
    if (!editor.current) return;
    if (error) {
      setError(undefined);
    }
    const json = editor.current.getValue();
    let data;
    try {
      data = JSON.parse(json);
    } catch (e) {
      setError("Invalid JSON");
      return;
    }
    const ajv = new Ajv();
    const validate = ajv.compile(corsSettingsSchema);
    const valid = validate(data);
    if (!valid) {
      setError(validate.errors?.[0].message!);
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/cors`, {
      method: "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: json,
    });
    if (res.ok) {
      toast("CORS settings saved");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>CORS Settings</CardTitle>
        <CardDescription>
          Configure Cross-Origin Resource Sharing for your API
        </CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="h-[200px] w-full rounded-md border" ref={editorElRef}></div>
        {error && <p className="text-sm text-destructive">{error}</p>}
        <Button onClick={handleSaveClick}>Save Changes</Button>
      </CardContent>
    </Card>
  );
}

// Authentication Section
interface IScheme {
  id: number;
  appId: number;
  type: string;
  name: string;
  description?: string;
  openIdConnectAuthority?: string;
  openIdConnectAudience?: string;
  enabled: boolean;
  createdAt: string;
  updatedAt?: string;
}

type SchemeFormInputs = {
  name: string;
  type: string;
  openIdConnectIssuer?: string;
  openIdConnectAudience?: string;
};

const schemeFormSchema = yup.object({
  name: yup.string().required("Name is required"),
  type: yup.string().required("Type is required"),
  openIdConnectIssuer: yup.string(),
  openIdConnectAudience: yup.string(),
});

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
          <CardTitle>Authentication Schemes</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="text-muted-foreground">Loading...</div>
        </CardContent>
      </Card>
    );
  }

  return (
    <>
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <div>
            <CardTitle>Authentication Schemes</CardTitle>
            <CardDescription>
              Manage authentication schemes for your API routes
            </CardDescription>
          </div>
          <Button size="sm" onClick={handleCreateClick}>
            <PlusIcon className="mr-1 h-4 w-4" />
            New Scheme
          </Button>
        </CardHeader>
        <CardContent>
          {schemes.length === 0 ? (
            <p className="text-sm text-muted-foreground">No authentication schemes configured.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Type</TableHead>
                  <TableHead>Enabled</TableHead>
                  <TableHead>Created</TableHead>
                  <TableHead className="w-[100px]">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {schemes.map((scheme) => (
                  <TableRow key={scheme.id}>
                    <TableCell className="font-medium">{scheme.name}</TableCell>
                    <TableCell>{scheme.type}</TableCell>
                    <TableCell>
                      <span className={scheme.enabled ? "text-green-600" : "text-muted-foreground"}>
                        {scheme.enabled ? "Yes" : "No"}
                      </span>
                    </TableCell>
                    <TableCell>{new Date(scheme.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <div className="flex gap-1">
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleEditClick(scheme)}
                        >
                          <PencilIcon className="h-4 w-4" />
                        </Button>
                        <Button
                          variant="ghost"
                          size="icon"
                          onClick={() => handleDeleteClick(scheme.id)}
                          disabled={scheme.enabled}
                        >
                          <TrashIcon className={`h-4 w-4 ${scheme.enabled ? "text-muted" : "text-destructive"}`} />
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
            <DialogTitle>{editingScheme ? "Edit Scheme" : "Create Scheme"}</DialogTitle>
            <DialogDescription>
              {editingScheme ? "Update the authentication scheme settings." : "Add a new authentication scheme."}
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmitScheme)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="name">Name</Label>
              <Input id="name" {...register("name")} />
              {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
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
                  <Input id="issuer" {...register("openIdConnectIssuer")} placeholder="https://example.auth0.com/" />
                  {errors.openIdConnectIssuer && (
                    <p className="text-sm text-destructive">{errors.openIdConnectIssuer.message}</p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label htmlFor="audience">Audience</Label>
                  <Input id="audience" {...register("openIdConnectAudience")} placeholder="https://api.example.com" />
                  {errors.openIdConnectAudience && (
                    <p className="text-sm text-destructive">{errors.openIdConnectAudience.message}</p>
                  )}
                </div>
              </>
            )}
            <DialogFooter>
              <Button type="button" variant="outline" onClick={() => setDialogOpen(false)}>
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

type AuthOrderInputs = {
  schemes: IScheme[];
};

function AuthOrderSection({ schemes, onUpdate }: { schemes: IScheme[]; onUpdate: () => void }) {
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
    setValue("schemes", schemes.filter((s) => s.enabled));
  }, [schemes]);

  const onSubmit = async (data: AuthOrderInputs) => {
    const schemeIds = data.schemes.map((s) => s.id);
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/authentications/schemes/settings`, {
      method: "POST",
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(schemeIds),
    });
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
        <CardTitle>Authentication Order</CardTitle>
        <CardDescription>
          Enable/disable and reorder authentication schemes
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Alert className="mb-4">
          <Info className="h-4 w-4" />
          <AlertDescription>
            Drag schemes between panels to enable/disable. Drag within the left panel to reorder.
          </AlertDescription>
        </Alert>
        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="flex gap-4">
            <div className="flex-1 rounded-lg border p-3">
              <h3 className="mb-2 text-sm font-medium">Enabled Schemes</h3>
              <DndContext onDragEnd={handleDragEnd}>
                <SortableContext items={fields}>
                  <div className="min-h-[100px] space-y-1">
                    {fields.length ? (
                      fields.map((scheme) => (
                        <SortableSchemeItem
                          key={scheme.id}
                          scheme={scheme}
                          onDisableClick={handleDisableClick}
                        />
                      ))
                    ) : (
                      <p className="text-sm text-muted-foreground">No schemes enabled</p>
                    )}
                  </div>
                </SortableContext>
              </DndContext>
            </div>
            <div className="flex-1 rounded-lg border p-3">
              <h3 className="mb-2 text-sm font-medium">Available Schemes</h3>
              <div className="min-h-[100px] space-y-1">
                {availableSchemes.length ? (
                  availableSchemes.map((s) => (
                    <AvailableSchemeItem
                      key={s.id}
                      scheme={s}
                      onEnableClick={handleEnableClick}
                    />
                  ))
                ) : (
                  <p className="text-sm text-muted-foreground">No schemes available</p>
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
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({
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
        className="flex-1 cursor-grab rounded border bg-muted/50 px-3 py-2 text-sm"
      >
        {scheme.name}
      </div>
      <button
        type="button"
        onClick={() => onDisableClick(scheme.id)}
        className="rounded p-1 hover:bg-muted"
      >
        <ArrowRightIcon className="h-4 w-4 text-primary" />
      </button>
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
      <button
        type="button"
        onClick={() => onEnableClick(scheme.id)}
        className="rounded p-1 hover:bg-muted"
      >
        <ArrowLeftIcon className="h-4 w-4 text-primary" />
      </button>
      <div className="flex-1 rounded border px-3 py-2 text-sm">{scheme.name}</div>
    </div>
  );
}

// ============ Pages Tab Components ============

interface IGitHubInstallation {
  installationId: number;
  accountLogin: string;
  accountType: string;
}

interface GitHubRepo {
  id: number;
  name: string;
  fullName: string;
}

function GitHubLinkSection() {
  const { get, post, del } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const [loading, setLoading] = useState(true);
  const [link, setLink] = useState<IAppIntegration | null>();
  const [installations, setInstallations] = useState<IGitHubInstallation[]>([]);
  const [repos, setRepos] = useState<GitHubRepo[]>([]);
  const [installationId, setInstallationId] = useState<number | null>(null);
  const [repoId, setRepoId] = useState<number | null>(null);

  useEffect(() => {
    (async () => {
      const [link, installations] = await Promise.all([
        get<IAppIntegration>(`/api/apps/${app.id}/link`).catch((err) =>
          err instanceof NotFoundError ? null : Promise.reject(err)
        ),
        get<IGitHubInstallation[]>(`/api/integrations/github/installations`),
      ]);

      setLink(link);
      setInstallations(installations);

      if (installations.length === 1) {
        setInstallationId(installations[0].installationId);
      }

      setLoading(false);
    })();
  }, []);

  useEffect(() => {
    if (!installationId) return;

    (async () => {
      const repos = await get<GitHubRepo[]>(
        `/api/integrations/github/installations/${installationId}/repos`
      );
      setRepos(repos);

      if (repos.length === 1) {
        setRepoId(repos[0].id);
      }
    })();
  }, [installationId]);

  const connectGitHub = async () => {
    const githubAppUrl = `https://github.com/apps/${GITHUB_APP_NAME}/installations/new?state=foo`;
    window.location.href = githubAppUrl;
  };

  const onConnect = async () => {
    if (!installationId || !repoId) return;

    await post(`/api/apps/${app.id}/link/github`, { installationId, repoId });

    setLink({
      type: "GitHub",
      org: installations.find((i) => i.installationId === installationId)!.accountLogin,
      repoId: repoId,
      repo: repos.find((r) => r.id === repoId)!.name,
    });
  };

  const onDisconnect = async () => {
    if (!confirm("Are you sure you want to disconnect this repository?")) return;

    await del(`/api/apps/${app.id}/link`);
    setLink(null);
  };

  if (loading) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>GitHub Integration</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="h-4 w-4 animate-spin" />
            <span>Loading GitHub integrations...</span>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>GitHub Integration</CardTitle>
        <CardDescription>
          Connect your app to a GitHub repository for automatic deployments
        </CardDescription>
      </CardHeader>
      <CardContent>
        {link ? (
          <div className="flex items-center justify-between rounded-lg border p-3 text-sm">
            <div>
              Connected to <strong>{link.org}/{link.repo}</strong>
            </div>
            <Button
              variant="link"
              onClick={onDisconnect}
              className="h-auto p-0 text-destructive"
            >
              Disconnect
            </Button>
          </div>
        ) : (
          <div className="space-y-3">
            <p className="text-sm text-muted-foreground">
              Connect your app to GitHub to access your repositories.
            </p>
            <div className="flex items-center gap-3">
              <Select
                value={installationId?.toString() ?? ""}
                onValueChange={(value) => {
                  if (value === "") setInstallationId(null);
                  else setInstallationId(Number(value));
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select organization" />
                </SelectTrigger>
                <SelectContent>
                  {installations.map((inst) => (
                    <SelectItem
                      key={inst.installationId}
                      value={inst.installationId.toString()}
                    >
                      {inst.accountLogin}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <Select
                value={repoId?.toString() ?? ""}
                onValueChange={(value) => {
                  if (value === "") setRepoId(null);
                  else setRepoId(Number(value));
                }}
              >
                <SelectTrigger className="w-[180px]">
                  <SelectValue placeholder="Select repository" />
                </SelectTrigger>
                <SelectContent>
                  {repos.map((repo) => (
                    <SelectItem key={repo.id} value={repo.id.toString()}>
                      {repo.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>

              {installationId && repoId && <Button onClick={onConnect}>Link</Button>}
            </div>
            <p className="text-sm text-muted-foreground">
              Missing some repositories?{" "}
              <a
                href="#"
                className="font-medium text-primary underline hover:no-underline"
                onClick={connectGitHub}
              >
                Manage GitHub access
              </a>
            </p>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

interface IBuildConfig {
  branch: string;
  directory: string;
  buildCommand: string;
  outDir: string;
}

function BuildSettingsSection() {
  const { get, post } = useApiClient();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const {
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<IBuildConfig>({
    defaultValues: {
      branch: "default",
      directory: ".",
      buildCommand: "npm run build",
      outDir: "dist",
    },
  });

  useEffect(() => {
    (async () => {
      const config = await get<IBuildConfig>(`/api/apps/${app.id}/builds/config`);

      setValue("branch", config.branch);
      setValue("directory", config.directory);
      setValue("buildCommand", config.buildCommand);
      setValue("outDir", config.outDir);
    })();
  }, []);

  const onSubmitConfig = async (data: IBuildConfig) => {
    await post(`/api/apps/${app.id}/builds/config`, data);
    toast.success("Build configuration saved");
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Build Settings</CardTitle>
        <CardDescription>
          Configure how your application is built and deployed
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Alert variant="destructive" className="mb-4">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            Settings can't be edited right now. Please try again shortly.
          </AlertDescription>
        </Alert>
        <TooltipProvider>
          <form onSubmit={handleSubmit(onSubmitConfig)} className="space-y-4">
            <div className="grid grid-cols-[140px_1fr] items-start gap-4">
              <div className="flex items-center gap-2 pt-2">
                <Label>Branch</Label>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span>
                      <InfoIcon />
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>The Git branch used for deployment.</p>
                  </TooltipContent>
                </Tooltip>
              </div>
              <div>
                <Input {...register("branch", { required: "Branch is required" })} readOnly />
                {errors.branch && (
                  <p className="mt-1 text-sm text-destructive">{errors.branch.message}</p>
                )}
              </div>
            </div>

            <div className="grid grid-cols-[140px_1fr] items-start gap-4">
              <div className="flex items-center gap-2 pt-2">
                <Label>Build Directory</Label>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span>
                      <InfoIcon />
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>Path relative to the root where the build runs.</p>
                  </TooltipContent>
                </Tooltip>
              </div>
              <div>
                <Input
                  {...register("directory", { required: "Directory is required" })}
                  readOnly
                />
                {errors.directory && (
                  <p className="mt-1 text-sm text-destructive">{errors.directory.message}</p>
                )}
              </div>
            </div>

            <div className="grid grid-cols-[140px_1fr] items-start gap-4">
              <div className="flex items-center gap-2 pt-2">
                <Label>Output Directory</Label>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span>
                      <InfoIcon />
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>Path where build output is located.</p>
                  </TooltipContent>
                </Tooltip>
              </div>
              <div>
                <Input {...register("outDir", { required: "Output dir is required" })} readOnly />
                {errors.outDir && (
                  <p className="mt-1 text-sm text-destructive">{errors.outDir.message}</p>
                )}
              </div>
            </div>

            <div className="grid grid-cols-[140px_1fr] items-start gap-4">
              <div className="flex items-center gap-2 pt-2">
                <Label>Build Command</Label>
                <Tooltip>
                  <TooltipTrigger asChild>
                    <span>
                      <InfoIcon />
                    </span>
                  </TooltipTrigger>
                  <TooltipContent>
                    <p>The command that runs your build (e.g. npm run build).</p>
                  </TooltipContent>
                </Tooltip>
              </div>
              <div>
                <Input
                  {...register("buildCommand", { required: "Build command is required" })}
                  readOnly
                />
                {errors.buildCommand && (
                  <p className="mt-1 text-sm text-destructive">{errors.buildCommand.message}</p>
                )}
              </div>
            </div>

            <Button type="submit" disabled>
              Save Changes
            </Button>
          </form>
        </TooltipProvider>
      </CardContent>
    </Card>
  );
}

// ============ General Tab Components ============

type RenameFormInput = { name: string };

function RenameSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const schema = yup.object({ name: yup.string().required() });
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RenameFormInput>({
    resolver: yupResolver(schema),
    defaultValues: { name: app.name },
  });
  const onSubmit = async (input: RenameFormInput) => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(`/api/apps/${app.id}/rename`, {
      method: "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
        "If-Match": app.version,
      },
      body: JSON.stringify(input),
    });
    if (res.ok) {
      toast("Renamed app");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>App Name</CardTitle>
        <CardDescription>Change the display name of your app</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit(onSubmit)} className="flex items-start gap-3">
          <div className="space-y-1">
            <Input
              type="text"
              {...register("name")}
              autoComplete="off"
              className="w-64"
            />
            {errors.name && (
              <p className="text-sm text-destructive">{errors.name.message}</p>
            )}
          </div>
          <Button type="submit">Rename</Button>
        </form>
      </CardContent>
    </Card>
  );
}

function DeleteSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const handleDeleteClick = async () => {
    if (confirm("Are you sure want to delete this app?")) {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}`, {
        method: "DELETE",
        headers: { Authorization: `Bearer ${accessToken}` },
      });
      if (res.ok) {
        toast("Deleted app");
        navigate("/apps");
      }
    }
  };

  return (
    <Card className="border-destructive/50">
      <CardHeader>
        <CardTitle className="text-destructive">Danger Zone</CardTitle>
        <CardDescription>
          Permanently delete this app and all its data
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button variant="destructive" onClick={handleDeleteClick}>
          Delete App
        </Button>
      </CardContent>
    </Card>
  );
}

function ChangeStateSection() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();
  const handleChangeStatusClick = async () => {
    if (
      app.status === "Active" &&
      !confirm("Are you sure want to deactivate the app?")
    ) {
      return;
    }
    const accessToken = await getAccessTokenSilently();
    const status = app.status === "Active" ? "Inactive" : "Active";
    const res = await fetch(`/api/apps/${app.id}/status?status=${status}`, {
      method: "PATCH",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      app.status = status;
      toast("Status changed");
      navigate(".");
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>App Status</CardTitle>
        <CardDescription>
          {app.status === "Active"
            ? "Your app is currently active and receiving traffic"
            : "Your app is currently inactive"}
        </CardDescription>
      </CardHeader>
      <CardContent>
        <Button
          variant={app.status === "Active" ? "outline" : "default"}
          disabled={app.status === "Blocked"}
          onClick={handleChangeStatusClick}
        >
          {app.status === "Active" ? "Deactivate App" : "Activate App"}
        </Button>
      </CardContent>
    </Card>
  );
}
