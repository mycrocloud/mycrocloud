import { Outlet, useNavigate, useParams, useMatch } from "react-router-dom";
import React, {
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
  //useRoutesContext,
} from "./Context";
import {
  ChevronDownIcon,
  ChevronRightIcon,
  EllipsisVerticalIcon,
} from "@heroicons/react/24/solid";
import { useForm } from "react-hook-form";
import { ensureSuccess } from "../../utils/fetchUtils";
import IRouteFolderRouteItem, { calculateLevel } from "./IRouteFolderRouteItem";
import IRoute from "./Route";
//import { toast } from "react-toastify";

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
        <div className="w-64 border-r p-1">
          <RouteExplorer />
        </div>
        <div className="flex-1">
          {newRouteActive || editRouteActive || logPageActive ? (
            <div className="">
              <Outlet key={routeId} />
            </div>
          ) : (
            <div className="flex h-screen items-center justify-center">
              Click New button to create new route or click route to edit.
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
  const filteredItems = useMemo(
    () => getFilteredItems(explorerItems, searchTerm),
    [explorerItems, searchTerm],
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
          if (
            item.route?.name.toLowerCase().includes(searchTerm.toLowerCase())
          ) {
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

              if (parent) {
                result.push(parent);
                getPathNodes(items, parent, result);
              }
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
      const accessToken = await getAccessTokenSilently();
      fetch(`/api/apps/${app.id}/routes`, {
        headers: { Authorization: `Bearer ${accessToken}` },
      })
        .then((res) => res.json() as Promise<IRouteFolderRouteItem[]>)
        .then((items) => {
          setExplorerItems(
            items.map((route) => {
              return { ...route, level: calculateLevel(route, items, null, 0) };
            }),
          );
        });
    };
    getRoutes();
  }, []);

  //#region Action Menu
  const [showActionMenu, setShowActionMenu] = useState(false);
  const actionMenuItemRef = useRef<IExplorerItem | null>(null);
  const actionMenuRef = useRef<HTMLDivElement>(null);
  const actionMenuClientXRef = useRef<number | null>(null);
  const actionMenuClientYRef = useRef<number | null>(null);

  function calculateActionMenuTop() {
    if (!actionMenuRef.current || !actionMenuClientYRef.current) {
      return undefined;
    }
    return actionMenuClientYRef.current - actionMenuRef.current.offsetTop;
  }
  function calculateActionMenuRight() {
    if (!actionMenuClientXRef.current) {
      return undefined;
    }
    return 20;
  }

  const handleActionMenuClick = (
    item: IExplorerItem,
    clientX: number,
    clientY: number,
  ) => {
    actionMenuClientXRef.current = clientX;
    actionMenuClientYRef.current = clientY;
    actionMenuItemRef.current = item;
    setShowActionMenu(true);
  };

  //#endregion

  const handleNewRouteClick = async () => {
    const { id, level } = actionMenuItemRef.current!;
    navigate(`new/${id}`);
    setExplorerItems((items) => {
      const newRoute: IExplorerItem = {
        type: "Route",
        id: -1,
        parentId: id,
        route: {
          name: "New Route",
          method: "GET",
          path: "/new-route",
          status: "Active",
        },
        folder: null,
        collapsed: false,
        level: level + 1,
      };
      return [...items, newRoute];
    });
  };

  const handleNewFolderClick = async (atRoot: boolean = false) => {
    const { id, level } = atRoot
      ? { id: null, level: 0 }
      : actionMenuItemRef.current!;
    setExplorerItems((items) => {
      const newFolder: IExplorerItem = {
        type: "Folder",
        id: -1,
        parentId: id,
        route: null,
        folder: { name: "new folder" },
        isEditing: true,
        level: atRoot ? 0 : level + 1,
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
    const isNewFolder = folder.id === -1;
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
          if (item.id === folder.id) {
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
          if (item.id === folder.id) {
            return { ...item, folder: { name: name }, isEditing: false };
          }
          return item;
        });
      });
    }
  };

  const handleDuplicateClick = async () => {
    const { type, id, parentId, level } = actionMenuItemRef.current!;
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
          newItems.map((item) => {
            return {
              ...item,
              level: calculateLevel(item, newItems, parentId, level),
            };
          }),
        );
      });
    } else {
      const newRoute = (await res.json()) as IRoute;
      var originalRoute = explorerItems.find(
        (item) => type === "Route" && item.id === id,
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

  const handleDeleteClick = async () => {
    const { type, id } = actionMenuItemRef.current!;
    if (confirm(`Are you sure want to delete this ${type.toLowerCase()}?`)) {
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
          let deleteItems = getFolderItems(actionMenuItemRef.current!);
          return nodes.filter((node) => {
            return !deleteItems.some((item) => item.id === node.id);
          });

          function getFolderItems(folder: IExplorerItem) {
            var items = nodes.filter((node) => node.parentId === folder.id);
            for (const item of items) {
              if (item.type === "Folder") {
                items = items.concat(getFolderItems(item));
              }
            }
            return items.concat(folder);
          }
        });
      } else {
        setExplorerItems((items) => {
          let remainingItems: IExplorerItem[] = [];

          for (const item of items) {
            const isDeletedRoute = item.type === "Route" && item.id === id;
            if (!isDeletedRoute) {
              remainingItems.push(item);
            }
          }

          return remainingItems;
        });
      }
    }
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

  const renderNode = (node: IExplorerItem | null, items: IExplorerItem[]) => {
    const isRoot = node === null;
    const children = isRoot
      ? items.filter((i) => i.parentId === null)
      : items.filter((i) => i.parentId === node.id);

    const isCurrentRoute = node?.type === "Route" && node.id === routeId;
    return (
      <>
        {isRoot ? null : (
          <div
            style={{ paddingLeft: node.level * 8 }}
            className={`hover:bg-slate-100 ${isCurrentRoute ? "bg-slate-100" : ""}`}
          >
            {node.type === "Folder" ? (
              <FolderItem
                item={node}
                onClick={() => handleFolderClick(node)}
                onActionMenuClick={(clientX, clientY) =>
                  handleActionMenuClick(node, clientX, clientY)
                }
                onNameSubmit={(name) => handleFolderNameSubmit(node, name)}
              />
            ) : (
              <RouteItem
                item={node}
                onClick={() => handleRouteClick(node)}
                onActionMenuClick={(clientX, clientY) =>
                  handleActionMenuClick(node, clientX, clientY)
                }
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
    <div>
      <div ref={actionMenuRef} className="relative">
        {showActionMenu && (
          <ActionMenu
            top={calculateActionMenuTop()!}
            right={calculateActionMenuRight()!}
            node={actionMenuItemRef.current!}
            onNewRouteClick={handleNewRouteClick}
            onNewFolderClick={() => handleNewFolderClick(false)}
            onFolderRenameClick={() =>
              handleFolderRenameClick(actionMenuItemRef.current!)
            }
            onDuplicateClick={handleDuplicateClick}
            onDeleteClick={handleDeleteClick}
            onOutSideClick={() => {
              setShowActionMenu(false);
            }}
          />
        )}
      </div>
      <div className="flex justify-center space-x-2">
        <button
          type="button"
          onClick={() => {
            navigate("new");
          }}
          className="w-1/2 bg-primary px-2 py-1 text-white enabled:hover:bg-cyan-700 disabled:opacity-50"
        >
          New Route
        </button>
        <button
          type="button"
          onClick={() => handleNewFolderClick(true)}
          className="w-1/2 bg-primary px-2 py-1 text-white enabled:hover:bg-cyan-700 disabled:opacity-50"
        >
          New Folder
        </button>
      </div>
      <div className="mt-1">
        <input
          type="text"
          placeholder="Filter"
          className="w-full border px-1 py-0.5"
          onChange={(e) => {
            setSearchTerm(e.target.value);
          }}
        />
      </div>
      <hr className="my-1" />
      {renderNode(null, filteredItems)}
    </div>
  );
}

function RouteItem({
  item: { route },
  onClick,
  onActionMenuClick,
}: {
  item: IExplorerItem;
  onActionMenuClick: (buttonLeft: number, buttonTop: number) => void;
  onClick: () => void;
}) {
  if (!route) {
    return null;
  }

  const methodTextColors = new Map<string, string>([
    ["GET", "text-sky-500"],
    ["POST", "text-orange-500"],
    ["PUT", "text-green-500"],
    ["DELETE", "text-red-500"],
    ["PATCH", "text-yellow-500"],
  ]);

  const handleActionMenuClick = (
    e: React.MouseEvent<HTMLButtonElement, MouseEvent>,
  ) => {
    e.stopPropagation();
    onActionMenuClick(e.currentTarget.offsetLeft, e.currentTarget.offsetTop);
  };

  return (
    <div
      className="group flex items-center p-0.5"
      style={{ cursor: "pointer" }}
      onClick={onClick}
    >
      <span
        className={`me-1 font-semibold ${methodTextColors.get(
          route.method.toUpperCase(),
        )}`}
        style={{ fontSize: "0.65rem" }}
      >
        {route.method}
      </span>
      <span className="overflow-hidden text-ellipsis whitespace-nowrap text-sm">
        {route.name}
      </span>
      <button
        type="button"
        onClick={(e) => handleActionMenuClick(e)}
        className="ms-auto hidden group-hover:block"
      >
        <EllipsisVerticalIcon className="h-4 w-4 text-gray-600" />
      </button>
      {route.status === "Blocked" && (
        <small className="ms-auto text-sm text-red-600">Blocked</small>
      )}
    </div>
  );
}

function FolderItem({
  item: { folder, collapsed, isEditing },
  onActionMenuClick,
  onClick,
  onNameSubmit,
}: {
  item: IExplorerItem;
  onActionMenuClick: (buttonLeft: number, buttonTop: number) => void;
  onClick: () => void;
  onNameSubmit: (name: string) => void;
}) {
  if (!folder) {
    return null;
  }

  const handleClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
    e.stopPropagation();
    onActionMenuClick(e.currentTarget.offsetLeft, e.currentTarget.offsetTop);
  };

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<{ name: string }>();

  const onSubmit = (data: { name: string }) => {
    onNameSubmit(data.name);
  };
  return !isEditing ? (
    <div
      className="group flex items-center p-0.5"
      style={{ cursor: "pointer" }}
      onClick={onClick}
    >
      {collapsed ? (
        <ChevronRightIcon className="me-1 h-4 w-4" />
      ) : (
        <ChevronDownIcon className="me-1 h-4 w-4" />
      )}
      <span className="overflow-hidden text-ellipsis whitespace-nowrap text-sm">
        {folder.name}
      </span>
      <button
        type="button"
        onClick={(e) => handleClick(e)}
        className="ms-auto hidden group-hover:block"
      >
        <EllipsisVerticalIcon className="h-4 w-4 text-gray-600" />
      </button>
    </div>
  ) : (
    <div className="flex items-center p-0.5">
      <form onSubmit={handleSubmit(onSubmit)} className="w-full">
        <input
          type="text"
          {...register("name", { required: "Name is required" })}
          className="w-full border"
          defaultValue={folder.name}
        />
        {errors.name && (
          <span className="text-red-500">{errors.name.message}</span>
        )}
      </form>
    </div>
  );
}

function ActionMenu({
  node,
  top,
  right,
  onNewRouteClick,
  onNewFolderClick,
  onDuplicateClick,
  onOutSideClick,
  onDeleteClick,
  onFolderRenameClick,
}: {
  node: IRouteFolderRouteItem;
  top: number;
  right: number;
  onNewRouteClick(): void;
  onNewFolderClick(): void;
  onDuplicateClick(): void;
  onDeleteClick(): void;
  onOutSideClick(): void;
  onFolderRenameClick(): void;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const windowClickHandler = () => {
    onOutSideClick();
  };
  useEffect(() => {
    window.addEventListener("click", windowClickHandler);

    return () => {
      window.removeEventListener("click", windowClickHandler);
    };
  }, []);
  return (
    <div
      style={{ top: `${top}px`, right: `${right}px` }}
      ref={ref}
      className="absolute w-36 border bg-white p-2 shadow"
    >
      <ul className="">
        {node.type === "Folder" && (
          <>
            <li className="p-1 hover:bg-gray-100">
              <button
                type="button"
                onClick={onNewRouteClick}
                className="w-full text-left"
              >
                New Route
              </button>
            </li>
            <li className="p-1 hover:bg-gray-100">
              <button
                type="button"
                onClick={onNewFolderClick}
                className="w-full text-left"
              >
                New Folder
              </button>
            </li>
          </>
        )}
        <li className="p-1 hover:bg-gray-100">
          <button
            type="button"
            onClick={onDuplicateClick}
            className="w-full text-left"
          >
            Duplicate
          </button>
        </li>
        {node.type === "Folder" && (
          <li className="p-1 hover:bg-gray-100">
            <button
              type="button"
              onClick={onFolderRenameClick}
              className="w-full text-left"
            >
              Rename
            </button>
          </li>
        )}
        <hr className="my-1" />
        <li className="p-1 hover:bg-gray-100">
          <button
            type="button"
            className="w-full text-left text-red-500"
            onClick={onDeleteClick}
          >
            Delete
          </button>
        </li>
      </ul>
    </div>
  );
}
