import React, { useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { AppContext } from "../apps";
import { useApiClient } from "@/hooks";
import IRouteFolderRouteItem, { calculateLevel } from "./IRouteFolderRouteItem";
import IRoute from "./Route";
import RouteItem from "./RouteItem";
import FolderItem from "./FolderItem";
import type { IExplorerItem } from "./types";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Plus,
  FolderPlus,
  Search,
  Loader2,
  Route,
} from "lucide-react";
import { cn } from "@/lib/utils";

export default function RouteExplorer() {
  const navigate = useNavigate();
  const params = useParams();
  const routeId = params["routeId"] ? parseInt(params["routeId"]) : undefined;

  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  const { get, post, patch, del } = useApiClient();

  const [explorerItems, setExplorerItems] = useState<IExplorerItem[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [loading, setLoading] = useState(true);
  const [deleteDialog, setDeleteDialog] = useState<{ open: boolean; item: IExplorerItem | null }>({
    open: false,
    item: null,
  });

  const filteredItems = useMemo(
    () => getFilteredItems(explorerItems, searchTerm),
    [explorerItems, searchTerm]
  );

  const routeCount = useMemo(
    () => explorerItems.filter((item) => item.type === "Route" && item.id > 0).length,
    [explorerItems]
  );

  // Load routes
  useEffect(() => {
    const loadRoutes = async () => {
      setLoading(true);
      try {
        const items = await get<IRouteFolderRouteItem[]>(`/api/apps/${app.id}/api/routes`);
        setExplorerItems(
          items.map((item) => ({
            ...item,
            level: calculateLevel(item, items, null, 0),
          }))
        );
      } finally {
        setLoading(false);
      }
    };
    loadRoutes();
  }, [routeId, app.id, get]);

  // Handlers
  const handleNewRouteClick = (folderId: number | null = null) => {
    navigate(folderId !== null ? `new/${folderId}` : "new");
  };

  const handleNewFolderClick = (parentId: number | null = null, level: number = 0) => {
    setExplorerItems((items) => [
      ...items,
      {
        type: "Folder",
        id: -Date.now(),
        parentId,
        route: null,
        folder: { name: "new folder" },
        isEditing: true,
        level,
      },
    ]);
  };

  const handleFolderRenameClick = (folder: IExplorerItem) => {
    setExplorerItems((items) =>
      items.map((item) =>
        item.type === "Folder" && item.id === folder.id
          ? { ...item, isEditing: true }
          : item
      )
    );
  };

  const handleFolderNameSubmit = async (folder: IExplorerItem, name: string) => {
    const isNewFolder = folder.id < 0;

    if (isNewFolder) {
      const newId = await post<number>(`/api/apps/${app.id}/api/routes/folders`, {
        name,
        parentId: folder.parentId,
      });
      setExplorerItems((items) =>
        items.map((item) =>
          item.type === "Folder" && item.id === folder.id
            ? { ...item, id: newId, folder: { name }, isEditing: false }
            : item
        )
      );
    } else {
      await patch(`/api/apps/${app.id}/api/routes/folders/${folder.id}/rename`, { name });
      setExplorerItems((items) =>
        items.map((item) =>
          item.type === "Folder" && item.id === folder.id
            ? { ...item, folder: { name }, isEditing: false }
            : item
        )
      );
    }
  };

  const handleDuplicateClick = async (item: IExplorerItem) => {
    const { type, id, parentId, level } = item;
    const url =
      type === "Route"
        ? `/api/apps/${app.id}/api/routes/${id}/clone`
        : `/api/apps/${app.id}/api/routes/folders/${id}/duplicate`;

    if (type === "Folder") {
      const newItems = await post<IRouteFolderRouteItem[]>(url, {});
      setExplorerItems((items) =>
        items.concat(
          newItems.map((newItem) => ({
            ...newItem,
            level: calculateLevel(newItem, newItems, parentId, level),
          }))
        )
      );
    } else {
      const newRoute = await post<IRoute>(url, {});
      const originalItem = explorerItems.find((i) => i.type === "Route" && i.id === id)!;
      setExplorerItems((items) =>
        items.concat({ ...originalItem, id: newRoute.id, route: newRoute })
      );
    }
  };

  const handleDeleteClick = (item: IExplorerItem) => {
    setDeleteDialog({ open: true, item });
  };

  const confirmDelete = async () => {
    const item = deleteDialog.item;
    if (!item) return;

    const { type, id } = item;
    const url =
      type === "Route"
        ? `/api/apps/${app.id}/api/routes/${id}`
        : `/api/apps/${app.id}/api/routes/folders/${id}`;

    await del(url);

    if (type === "Folder") {
      setExplorerItems((nodes) => {
        const getFolderItems = (folder: IExplorerItem): IExplorerItem[] => {
          const children = nodes.filter((n) => n.parentId === folder.id);
          return children.concat(
            children
              .filter((c) => c.type === "Folder")
              .flatMap(getFolderItems)
          ).concat(folder);
        };
        const deleteItems = getFolderItems(item);
        return nodes.filter(
          (n) => !deleteItems.some((d) => d.id === n.id && d.type === n.type)
        );
      });
    } else {
      setExplorerItems((items) =>
        items.filter((i) => !(i.type === "Route" && i.id === id))
      );
    }

    setDeleteDialog({ open: false, item: null });
  };

  const handleFolderClick = (folder: IExplorerItem) => {
    setExplorerItems((items) =>
      items.map((item) =>
        item.type === "Folder" && item.id === folder.id
          ? { ...item, collapsed: !item.collapsed }
          : item
      )
    );
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
        if (currentItem) handleDeleteClick(currentItem);
      }
    },
    [flatRoutes, routeId, navigate]
  );

  // Render tree node
  const renderNode = (node: IExplorerItem | null, items: IExplorerItem[]) => {
    const isRoot = node === null;
    const children = isRoot
      ? items.filter((i) => i.parentId === null)
      : node.type === "Folder"
        ? items.filter((i) => i.parentId === node.id)
        : [];

    const isCurrentRoute = node?.type === "Route" && node.id === routeId;

    return (
      <>
        {!isRoot && (
          <div
            style={{ paddingLeft: node.level * 12 }}
            className={cn("group", isCurrentRoute && "bg-accent")}
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
            <React.Fragment key={`${child.type}-${child.id}`}>
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
          <Button size="sm" onClick={() => handleNewRouteClick()} className="flex-1">
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
                <Button size="sm" className="mt-4" onClick={() => handleNewRouteClick()}>
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
      <Dialog
        open={deleteDialog.open}
        onOpenChange={(open) => setDeleteDialog({ open, item: open ? deleteDialog.item : null })}
      >
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

// Helper function to filter items
function getFilteredItems(explorerItems: IExplorerItem[], searchTerm: string): IExplorerItem[] {
  if (!searchTerm) return explorerItems;

  const term = searchTerm.toLowerCase();
  const result: IExplorerItem[] = [];

  for (const item of explorerItems) {
    if (item.type !== "Route") continue;

    const matchesName = item.route?.name.toLowerCase().includes(term);
    const matchesPath = item.route?.path.toLowerCase().includes(term);
    const matchesMethod = item.route?.method.toLowerCase().includes(term);

    if (matchesName || matchesPath || matchesMethod) {
      result.push(item);
      // Add parent folders
      addParentFolders(explorerItems, item, result);
    }
  }

  return result;
}

function addParentFolders(
  allItems: IExplorerItem[],
  item: IExplorerItem,
  result: IExplorerItem[]
) {
  const parent = allItems.find((i) => i.type === "Folder" && i.id === item.parentId);
  if (!parent) return;
  if (!result.some((i) => i.type === "Folder" && i.id === parent.id)) {
    result.push(parent);
  }
  addParentFolders(allItems, parent, result);
}
