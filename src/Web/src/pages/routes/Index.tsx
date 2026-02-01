import { Outlet, useNavigate, useParams, useMatch } from "react-router-dom";
import React, {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useReducer,
  useRef,
  useState,
} from "react";
import { AppContext } from "../apps";
import { useAuth0 } from "@auth0/auth0-react";
import {
  RoutesContext,
  routesReducer,
} from "./Context";
import { useForm } from "react-hook-form";
import { ensureSuccess } from "../../hooks/useApiClient";
import IRouteFolderRouteItem, { calculateLevel } from "./IRouteFolderRouteItem";
import IRoute from "./Route";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  ChevronRight,
  ChevronDown,
  MoreVertical,
  Plus,
  FolderPlus,
  Search,
  File,
  Folder,
  Copy,
  Pencil,
  Trash2,
  Loader2,
  Route,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { cn } from "@/lib/utils";

interface IExplorerItem extends IRouteFolderRouteItem {
  level: number;
  collapsed?: boolean;
  isEditing?: boolean;
}

export default function RouteIndex() {
  const [state, dispatch] = useReducer(routesReducer, {
    routes: [],
    activeRoute: undefined,
  });
  const params = useParams();
  const routeId = params["routeId"] ? parseInt(params["routeId"]) : undefined;

  const newRouteActive = useMatch("/apps/:appId/routes/new/:folderId?");
  const editRouteActive = useMatch("/apps/:appId/routes/:routeId");
  const logPageActive = useMatch("/apps/:appId/routes/:routeId/logs");

  return (
    <RoutesContext.Provider value={{ state, dispatch }}>
      <div className="flex h-full">
        <div className="w-72 border-r bg-muted/20">
          <RouteExplorer />
        </div>
        <div className="flex-1 overflow-auto">
          {newRouteActive || editRouteActive || logPageActive ? (
            <Outlet key={routeId} />
          ) : (
            <div className="flex h-full items-center justify-center text-muted-foreground">
              <div className="text-center">
                <File className="mx-auto h-12 w-12 text-muted-foreground/50" />
                <p className="mt-2">Select a route to edit or create a new one</p>
              </div>
            </div>
          )}
        </div>
      </div>
    </RoutesContext.Provider>
  );
}

function RouteExplorer() {
  const { getAccessTokenSilently } = useAuth0();
  const navigate = useNavigate();

  const params = useParams();
  const routeId = params["routeId"] ? parseInt(params["routeId"]) : undefined;

  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const [explorerItems, setExplorerItems] = useState<IExplorerItem[]>([]);
  const [searchTerm, setSearchTerm] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; item: IExplorerItem | null }>({
    open: false,
    item: null,
  });
  const filteredItems = useMemo(
    () => getFilteredItems(explorerItems, searchTerm),
    [explorerItems, searchTerm],
  );

  const routeCount = useMemo(
    () => explorerItems.filter((item) => item.type === "Route" && item.id > 0).length,
    [explorerItems],
  );

  function getFilteredItems(
    explorerItems: IExplorerItem[],
    searchTerm: string,
  ) {
    if (!searchTerm) {
      return explorerItems;
    }

    function filterItems(items: IExplorerItem[], searchTerm: string) {
      let result: IExplorerItem[] = [];

      for (const item of items) {
        if (item.type === "Route") {
          const term = searchTerm.toLowerCase();
          const matchesName = item.route?.name.toLowerCase().includes(term);
          const matchesPath = item.route?.path.toLowerCase().includes(term);
          const matchesMethod = item.route?.method.toLowerCase().includes(term);
          if (matchesName || matchesPath || matchesMethod) {
            result.push(item);
            const pathNodes: IExplorerItem[] = [];
            getPathNodes(explorerItems, item, pathNodes);

            result.push(
              ...pathNodes.filter(
                (folder) =>
                  !result.some(
                    (i) => i.type === "Folder" && i.id === folder.id,
                  ),
              ),
            );

            function getPathNodes(
              items: IExplorerItem[],
              item: IExplorerItem,
              result: IExplorerItem[],
            ) {
              const parent = items.find(
                (i) => i.type === "Folder" && i.id === item.parentId,
              );

              if (!parent) {
                return;
              }

              result.push(parent);
              getPathNodes(items, parent, result);
            }
          }
        }
      }
      return result;
    }

    return filterItems(explorerItems, searchTerm);
  }

  useEffect(() => {
    const getRoutes = async () => {
      setLoading(true);
      try {
        const accessToken = await getAccessTokenSilently();
        const res = await fetch(`/api/apps/${app.id}/routes`, {
          headers: { Authorization: `Bearer ${accessToken}` },
        });
        const items = (await res.json()) as IRouteFolderRouteItem[];
        setExplorerItems(
          items.map((route) => {
            return { ...route, level: calculateLevel(route, items, null, 0) };
          }),
        );
      } finally {
        setLoading(false);
      }
    };
    getRoutes();
  }, [routeId]);

  const handleNewRouteClick = (folderId: number | null = null) => {
    if (folderId !== null) {
      navigate(`new/${folderId}`);
    } else {
      navigate("new");
    }
  };

  const handleNewFolderClick = async (parentId: number | null = null, level: number = 0) => {
    setExplorerItems((items) => {
      const newFolder: IExplorerItem = {
        type: "Folder",
        // Use unique negative ID to avoid conflicts when creating multiple items before saving
        id: -Date.now(),
        parentId: parentId,
        route: null,
        folder: { name: "new folder" },
        isEditing: true,
        level: level,
      };
      return [...items, newFolder];
    });
  };

  const handleFolderRenameClick = (folder: IExplorerItem) => {
    setExplorerItems((items) => {
      return items.map((item) => {
        if (item.type === "Folder" && item.id === folder.id) {
          return { ...item, isEditing: true };
        }
        return item;
      });
    });
  };

  const handleFolderNameSubmit = async (
    folder: IExplorerItem,
    name: string,
  ) => {
    // Check for negative ID (new unsaved folders have unique negative IDs like -Date.now())
    const isNewFolder = folder.id < 0;
    const accessToken = await getAccessTokenSilently();
    if (isNewFolder) {
      const res = await fetch(`/api/apps/${app.id}/routes/folders`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${accessToken}`,
        },
        body: JSON.stringify({ name: name, parentId: folder.parentId }),
      });
      ensureSuccess(res);
      setExplorerItems((items) => {
        return items.map((item) => {
          // Check both type and id to avoid updating wrong item when Route and Folder share same id
          if (item.type === "Folder" && item.id === folder.id) {
            return {
              ...item,
              id: parseInt(res.headers.get("Location")!),
              folder: { name: name },
              isEditing: false,
            };
          }
          return item;
        });
      });
    } else {
      const res = await fetch(
        `/api/apps/${app.id}/routes/folders/${folder.id}/rename`,
        {
          method: "PATCH",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${accessToken}`,
          },
          body: JSON.stringify({ name: name }),
        },
      );
      ensureSuccess(res);
      setExplorerItems((items) => {
        return items.map((item) => {
          // Check both type and id to avoid updating wrong item when Route and Folder share same id
          if (item.type === "Folder" && item.id === folder.id) {
            return { ...item, folder: { name: name }, isEditing: false };
          }
          return item;
        });
      });
    }
  };

  const handleDuplicateClick = async (item: IExplorerItem) => {
    const { type, id, parentId, level } = item;
    const accessToken = await getAccessTokenSilently();
    const url =
      type === "Route"
        ? `/api/apps/${app.id}/routes/${id}/clone`
        : `/api/apps/${app.id}/routes/folders/${id}/duplicate`;

    const res = await fetch(url, {
      method: "POST",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    ensureSuccess(res);
    if (type === "Folder") {
      const newItems = (await res.json()) as IRouteFolderRouteItem[];
      setExplorerItems((items) => {
        return items.concat(
          newItems.map((newItem) => {
            return {
              ...newItem,
              level: calculateLevel(newItem, newItems, parentId, level),
            };
          }),
        );
      });
    } else {
      const newRoute = (await res.json()) as IRoute;
      const originalRoute = explorerItems.find(
        (i) => i.type === "Route" && i.id === id,
      )!;
      setExplorerItems((items) => {
        return items.concat({
          ...originalRoute,
          id: newRoute.id,
          route: newRoute,
        });
      });
    }
  };

  const handleDeleteClick = (item: IExplorerItem) => {
    setDeleteDialog({ open: true, item });
  };

  const confirmDelete = async () => {
    const item = deleteDialog.item;
    if (!item) return;

    const { type, id } = item;
    const accessToken = await getAccessTokenSilently();
    const url =
      type === "Route"
        ? `/api/apps/${app.id}/routes/${id}`
        : `/api/apps/${app.id}/routes/folders/${id}`;

    const res = await fetch(url, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    ensureSuccess(res);

    if (type === "Folder") {
      setExplorerItems((nodes) => {
        let deleteItems = getFolderItems(item);
        return nodes.filter((node) => {
          // Check both id and type to avoid deleting wrong item when Route and Folder share same id
          return !deleteItems.some((i) => i.id === node.id && i.type === node.type);
        });

        function getFolderItems(folder: IExplorerItem) {
          var items = nodes.filter((node) => node.parentId === folder.id);
          for (const i of items) {
            if (i.type === "Folder") {
              items = items.concat(getFolderItems(i));
            }
          }
          return items.concat(folder);
        }
      });
    } else {
      setExplorerItems((items) => {
        return items.filter((i) => !(i.type === "Route" && i.id === id));
      });
    }

    setDeleteDialog({ open: false, item: null });
  };

  const handleFolderClick = (folder: IExplorerItem) => {
    setExplorerItems((items) => {
      return items.map((item) => {
        if (item.type === "Folder" && item.id === folder.id) {
          return { ...item, collapsed: !item.collapsed };
        }
        return item;
      });
    });
  };

  const handleRouteClick = (route: IExplorerItem) => {
    navigate(route.id.toString());
  };

  // Keyboard navigation
  const treeRef = useRef<HTMLDivElement>(null);
  const flatRoutes = useMemo(() => {
    const result: IExplorerItem[] = [];
    const addItems = (parentId: number | null) => {
      const children = filteredItems.filter((i) => i.parentId === parentId);
      for (const child of children) {
        result.push(child);
        if (child.type === "Folder" && !child.collapsed) {
          addItems(child.id);
        }
      }
    };
    addItems(null);
    return result.filter((i) => i.type === "Route" && i.id > 0);
  }, [filteredItems]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (!flatRoutes.length) return;

      const currentIndex = flatRoutes.findIndex((r) => r.id === routeId);

      if (e.key === "ArrowDown") {
        e.preventDefault();
        const nextIndex = currentIndex < flatRoutes.length - 1 ? currentIndex + 1 : 0;
        navigate(flatRoutes[nextIndex].id.toString());
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        const prevIndex = currentIndex > 0 ? currentIndex - 1 : flatRoutes.length - 1;
        navigate(flatRoutes[prevIndex].id.toString());
      } else if (e.key === "Delete" && routeId) {
        e.preventDefault();
        const currentItem = flatRoutes.find((r) => r.id === routeId);
        if (currentItem) {
          handleDeleteClick(currentItem);
        }
      }
    },
    [flatRoutes, routeId, navigate, handleDeleteClick]
  );

  const renderNode = (node: IExplorerItem | null, items: IExplorerItem[]) => {
    const isRoot = node === null;
    // Only Folders can have children; Routes return empty array to prevent duplicate rendering
    // when a Route and Folder share the same id
    const children = isRoot
      ? items.filter((i) => i.parentId === null)
      : node.type === "Folder"
        ? items.filter((i) => i.parentId === node.id)
        : [];

    const isCurrentRoute = node?.type === "Route" && node.id === routeId;
    return (
      <>
        {isRoot ? null : (
          <div
            style={{ paddingLeft: node.level * 12 }}
            className={cn(
              "group",
              isCurrentRoute && "bg-accent"
            )}
          >
            {node.type === "Folder" ? (
              <FolderItem
                item={node}
                onClick={() => handleFolderClick(node)}
                onNewRoute={() => handleNewRouteClick(node.id)}
                onNewFolder={() => handleNewFolderClick(node.id, node.level + 1)}
                onRename={() => handleFolderRenameClick(node)}
                onDuplicate={() => handleDuplicateClick(node)}
                onDelete={() => handleDeleteClick(node)}
                onNameSubmit={(name) => handleFolderNameSubmit(node, name)}
              />
            ) : (
              <RouteItem
                item={node}
                isActive={isCurrentRoute}
                onClick={() => handleRouteClick(node)}
                onDuplicate={() => handleDuplicateClick(node)}
                onDelete={() => handleDeleteClick(node)}
              />
            )}
          </div>
        )}
        {(isRoot || !node.collapsed) &&
          children.map((child) => (
            <React.Fragment key={child.type + "-" + child.id}>
              {renderNode(child, items)}
            </React.Fragment>
          ))}
      </>
    );
  };

  return (
    <div className="flex h-full flex-col">
      {/* Header */}
      <div className="border-b p-3">
        <div className="mb-3 flex items-center justify-between">
          <span className="text-sm font-medium">Routes</span>
          {!loading && (
            <Badge variant="secondary" className="text-xs">
              {routeCount}
            </Badge>
          )}
        </div>
        <div className="flex gap-2">
          <Button
            size="sm"
            onClick={() => handleNewRouteClick()}
            className="flex-1"
          >
            <Plus className="mr-1 h-4 w-4" />
            Route
          </Button>
          <Button
            size="sm"
            variant="outline"
            onClick={() => handleNewFolderClick(null, 0)}
            className="flex-1"
          >
            <FolderPlus className="mr-1 h-4 w-4" />
            Folder
          </Button>
        </div>
        <div className="relative mt-3">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input
            type="text"
            placeholder="Search name, path, method..."
            className="pl-8"
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
      </div>

      {/* Tree */}
      <div
        ref={treeRef}
        className="flex-1 overflow-auto p-2 focus:outline-none"
        tabIndex={0}
        onKeyDown={handleKeyDown}
      >
        {loading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-5 w-5 animate-spin text-muted-foreground" />
          </div>
        ) : filteredItems.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-12 text-center">
            {searchTerm ? (
              <>
                <Search className="h-10 w-10 text-muted-foreground/30" />
                <p className="mt-3 text-sm font-medium">No routes found</p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Try searching by name, path, or method
                </p>
              </>
            ) : (
              <>
                <Route className="h-10 w-10 text-muted-foreground/30" />
                <p className="mt-3 text-sm font-medium">No routes yet</p>
                <p className="mt-1 text-xs text-muted-foreground">
                  Create your first route to get started
                </p>
                <Button
                  size="sm"
                  className="mt-4"
                  onClick={() => handleNewRouteClick()}
                >
                  <Plus className="mr-1 h-4 w-4" />
                  Create Route
                </Button>
              </>
            )}
          </div>
        ) : (
          renderNode(null, filteredItems)
        )}
      </div>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteDialog.open} onOpenChange={(open) => setDeleteDialog({ open, item: open ? deleteDialog.item : null })}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete {deleteDialog.item?.type}</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete this {deleteDialog.item?.type.toLowerCase()}?
              {deleteDialog.item?.type === "Folder" && " All routes inside will also be deleted."}
              {" "}This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteDialog({ open: false, item: null })}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={confirmDelete}>
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

const methodColors: Record<string, string> = {
  GET: "text-sky-600 bg-sky-100 dark:text-sky-400 dark:bg-sky-950",
  POST: "text-orange-600 bg-orange-100 dark:text-orange-400 dark:bg-orange-950",
  PUT: "text-emerald-600 bg-emerald-100 dark:text-emerald-400 dark:bg-emerald-950",
  DELETE: "text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-950",
  PATCH: "text-amber-600 bg-amber-100 dark:text-amber-400 dark:bg-amber-950",
};

function RouteItem({
  item,
  isActive,
  onClick,
  onDuplicate,
  onDelete,
}: {
  item: IExplorerItem;
  isActive: boolean;
  onClick: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
}) {
  const { route } = item;
  if (!route) return null;

  const methodClass = methodColors[route.method.toUpperCase()] || "text-gray-600 bg-gray-50";
  const isNewRoute = item.id < 0;

  return (
    <div
      className={cn(
        "flex items-center gap-2 rounded-md px-2 py-1.5 cursor-pointer transition-colors",
        isActive ? "bg-accent" : "hover:bg-muted"
      )}
      onClick={onClick}
    >
      <span
        className={cn(
          "shrink-0 rounded px-1.5 py-0.5 text-[10px] font-semibold",
          methodClass
        )}
      >
        {route.method}
      </span>
      <span className={cn(
        "flex-1 truncate text-sm",
        route.status === "Inactive" && "text-muted-foreground"
      )}>
        {route.name}
      </span>
      {route.status === "Blocked" && (
        <span className="shrink-0 text-xs text-destructive">Blocked</span>
      )}
      {route.status === "Inactive" && !isNewRoute && (
        <span className="shrink-0 text-xs text-muted-foreground">Inactive</span>
      )}
      <DropdownMenu>
        <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 shrink-0 opacity-0 group-hover:opacity-100"
          >
            <MoreVertical className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDuplicate(); }}>
            <Copy className="mr-2 h-4 w-4" />
            Duplicate
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem
            onClick={(e) => { e.stopPropagation(); onDelete(); }}
            className="text-destructive focus:text-destructive"
          >
            <Trash2 className="mr-2 h-4 w-4" />
            Delete
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

function FolderItem({
  item: { folder, collapsed, isEditing },
  onClick,
  onNewRoute,
  onNewFolder,
  onRename,
  onDuplicate,
  onDelete,
  onNameSubmit,
}: {
  item: IExplorerItem;
  onClick: () => void;
  onNewRoute: () => void;
  onNewFolder: () => void;
  onRename: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
  onNameSubmit: (name: string) => void;
}) {
  if (!folder) return null;

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<{ name: string }>();

  const onSubmit = (data: { name: string }) => {
    onNameSubmit(data.name);
  };

  if (isEditing) {
    return (
      <div className="flex items-center gap-2 px-2 py-1">
        <Folder className="h-4 w-4 shrink-0 text-muted-foreground" />
        <form onSubmit={handleSubmit(onSubmit)} className="flex-1">
          <Input
            {...register("name", { required: "Name is required" })}
            defaultValue={folder.name}
            autoFocus
            className="h-7 text-sm"
          />
          {errors.name && (
            <span className="text-xs text-destructive">{errors.name.message}</span>
          )}
        </form>
      </div>
    );
  }

  return (
    <div
      className="flex items-center gap-1 rounded-md px-2 py-1.5 cursor-pointer hover:bg-muted transition-colors"
      onClick={onClick}
    >
      {collapsed ? (
        <ChevronRight className="h-4 w-4 shrink-0 text-muted-foreground" />
      ) : (
        <ChevronDown className="h-4 w-4 shrink-0 text-muted-foreground" />
      )}
      <Folder className="h-4 w-4 shrink-0 text-muted-foreground" />
      <span className="flex-1 truncate text-sm">{folder.name}</span>
      <DropdownMenu>
        <DropdownMenuTrigger asChild onClick={(e) => e.stopPropagation()}>
          <Button
            variant="ghost"
            size="icon"
            className="h-6 w-6 shrink-0 opacity-0 group-hover:opacity-100"
          >
            <MoreVertical className="h-4 w-4" />
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onNewRoute(); }}>
            <Plus className="mr-2 h-4 w-4" />
            New Route
          </DropdownMenuItem>
          <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onNewFolder(); }}>
            <FolderPlus className="mr-2 h-4 w-4" />
            New Folder
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onDuplicate(); }}>
            <Copy className="mr-2 h-4 w-4" />
            Duplicate
          </DropdownMenuItem>
          <DropdownMenuItem onClick={(e) => { e.stopPropagation(); onRename(); }}>
            <Pencil className="mr-2 h-4 w-4" />
            Rename
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem
            onClick={(e) => { e.stopPropagation(); onDelete(); }}
            className="text-destructive focus:text-destructive"
          >
            <Trash2 className="mr-2 h-4 w-4" />
            Delete
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
