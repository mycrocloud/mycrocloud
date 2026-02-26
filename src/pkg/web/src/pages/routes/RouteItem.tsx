import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { MoreVertical, Copy, Trash2 } from "lucide-react";
import { cn } from "@/lib/utils";
import type { IExplorerItem } from "./types";

const METHOD_COLORS: Record<string, string> = {
  GET: "text-sky-600 bg-sky-100 dark:text-sky-400 dark:bg-sky-950",
  POST: "text-orange-600 bg-orange-100 dark:text-orange-400 dark:bg-orange-950",
  PUT: "text-emerald-600 bg-emerald-100 dark:text-emerald-400 dark:bg-emerald-950",
  DELETE: "text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-950",
  PATCH: "text-amber-600 bg-amber-100 dark:text-amber-400 dark:bg-amber-950",
};

interface RouteItemProps {
  item: IExplorerItem;
  isActive: boolean;
  onClick: () => void;
  onDuplicate: () => void;
  onDelete: () => void;
}

export default function RouteItem({
  item,
  isActive,
  onClick,
  onDuplicate,
  onDelete,
}: RouteItemProps) {
  const { route } = item;
  if (!route) return null;

  const methodClass = METHOD_COLORS[route.method.toUpperCase()] || "text-gray-600 bg-gray-50";
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
      <span
        className={cn(
          "flex-1 truncate text-sm",
          route.status === "Inactive" && "text-muted-foreground"
        )}
      >
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
