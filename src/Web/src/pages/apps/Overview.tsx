import { useContext } from "react";
import { AppContext } from ".";
import { getAppDomain } from "./service";
import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import TextCopyButton from "../../components/ui/TextCopyButton";

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const domain = getAppDomain(app.id);

  return (
    <div className="space-y-6 p-4">
      <div>
        <h2 className="text-xl font-semibold">Overview</h2>
        <div className="mt-4 grid grid-cols-[auto_1fr] gap-x-8 gap-y-2 text-sm">
          <span className="text-muted-foreground">Name</span>
          <span>{app.name}</span>
          <span className="text-muted-foreground">Description</span>
          <span>{app.description || "-"}</span>
          <span className="text-muted-foreground">Status</span>
          <span className="flex items-center gap-1">
            {app.status === "Active" ? (
              <PlayCircleIcon className="h-4 w-4 text-green-500" />
            ) : (
              <StopCircleIcon className="h-4 w-4 text-red-500" />
            )}
            {app.status}
          </span>
          <span className="text-muted-foreground">Created at</span>
          <span>{new Date(app.createdAt).toDateString()}</span>
          <span className="text-muted-foreground">Updated at</span>
          <span>{app.updatedAt ? new Date(app.updatedAt!).toDateString() : "-"}</span>
          <span className="text-muted-foreground">Domain</span>
          <span className="flex items-center gap-1">
            <span className="text-primary">{domain}</span>
            <TextCopyButton text={domain} />
          </span>
        </div>
      </div>
    </div>
  );
}
