import type IRouteFolderRouteItem from "./IRouteFolderRouteItem";

export interface IExplorerItem extends IRouteFolderRouteItem {
  level: number;
  collapsed?: boolean;
  isEditing?: boolean;
}
