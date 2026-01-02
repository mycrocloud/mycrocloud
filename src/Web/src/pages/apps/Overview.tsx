import { useContext } from "react";
import { AppContext } from ".";
import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import TextCopyButton from "@/components/ui/TextCopyButton";

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();

  return (
    <div className="flex h-full min-h-0 gap-4">
      {/* MAIN (Chart area) */}
      <div className="min-w-0 flex-1">
        {/* Page title */}
        <div className="mb-3">
          <h2 className="text-lg font-semibold text-slate-900">Overview</h2>
          <p className="text-sm text-slate-500">
            Metrics & usage insights for this app.
          </p>
        </div>

        {/* Chart placeholder */}
        <div className="rounded-xl border border-slate-200 bg-white">

        </div>
      </div>

      {/* RIGHT (Info panel) */}
      <aside className="hidden w-80 shrink-0 lg:block">
        <div className="sticky top-0 rounded-xl border border-slate-200 bg-white">
          <div className="border-b border-slate-200 px-4 py-3">
            <p className="text-sm font-medium text-slate-800">App details</p>
          </div>

          <div className="space-y-3 p-4 text-sm">
            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Name</p>
              <p className="col-span-2 font-medium text-slate-900">{app?.name || "-"}</p>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Description</p>
              <p className="col-span-2 text-slate-900">{app?.description || "-"}</p>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Status</p>
              <div className="col-span-2 flex items-center gap-2">
                <span className="inline-flex h-2 w-2 rounded-full bg-emerald-500" />
                <p className="font-medium text-slate-900">{app?.status || "Active"}</p>
              </div>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Created</p>
              <p className="col-span-2 text-slate-900">{app?.createdAt || "-"}</p>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Updated</p>
              <p className="col-span-2 text-slate-900">{app?.updatedAt || "-"}</p>
            </div>

            <div className="grid grid-cols-3 gap-3">
              <p className="col-span-1 text-slate-500">Domain</p>
              <div className="col-span-2">
                <div className="flex items-center gap-2">
                  <a
                    href={app.domain}
                    target="_blank"
                    rel="noreferrer"
                    className="truncate text-blue-600 hover:underline"
                    title={app.domain}
                  >
                    {app.domain}
                  </a>
                  <button
                    type="button"
                    className="rounded-md border border-slate-200 bg-white px-2 py-1 text-xs text-slate-700 hover:bg-slate-50"
                    onClick={() => navigator.clipboard?.writeText(app.domain)}
                  >
                    Copy
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </aside>
    </div>
  );
}
