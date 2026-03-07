export default interface IRouteFolderRouteItem {
  type: "Route" | "Folder";
  id: number;
  parentId: number | null;
  route: IRouteItem | null;
  folder: IFolderItem | null;
}

interface IRouteItem {
  name: string;
  method: string;
  path: string;
  status: string;
}

interface IFolderItem {
  name: string;
}

export function calculateLevel(
  item: IRouteFolderRouteItem,
  items: IRouteFolderRouteItem[],
  topParentId: number | null,
  topLevel: number,
): number {
  if (item.parentId === topParentId) {
    return topLevel;
  }

  const parent = items.find(
    (r) => r.type === "Folder" && r.id === item.parentId,
  );
  if (!parent) {
    throw new Error("Parent not found");
  }

  const parentLevel = calculateLevel(parent, items, topParentId, topLevel);

  return parentLevel + 1;
}
