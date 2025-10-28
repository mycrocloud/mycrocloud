export default interface Item {
  type: "File" | "Folder";
  name: string;
  id: number;
  size?: number;
  createdAt: string;
  parentId: number
}

export interface FolderPathItem {
  depth: number;
  id: number;
  name: string;
}