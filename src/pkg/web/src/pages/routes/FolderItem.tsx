import { useForm } from "react-hook-form";
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
  Folder,
  Copy,
  Pencil,
  Trash2,
} from "lucide-react";
import type { IExplorerItem } from "./types";

interface FolderItemProps {
  item: IExplorerItem;
  onClick: () => void;
  onNewRoute: () => void;
  onNewFolder: () => void;
  onRename: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
  onNameSubmit: (name: string) => void;
}

export default function FolderItem({
  item: { folder, collapsed, isEditing },
  onClick,
  onNewRoute,
  onNewFolder,
  onRename,
  onDuplicate,
  onDelete,
  onNameSubmit,
}: FolderItemProps) {
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
