import { useContext } from "react";
import { AppContext } from ".";
import { getAppDomain } from "./service";
import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import TextCopyButton from "@/components/ui/TextCopyButton";

export default function AppOverview() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const domain = getAppDomain(app.id);

  return (
    <div className="p-2">
      <h2 className="font-bold">Overview</h2>
      <table className="mt-1">
        <tbody>
          <tr>
            <td>Name</td>
            <td>{app.name}</td>
          </tr>
          <tr>
            <td>Description</td>
            <td>{app.description}</td>
          </tr>
          <tr>
            <td>Status</td>
            <td className="inline-flex">
              {app.status === "Active" ? (
                <PlayCircleIcon className="h-4 w-4 text-green-500" />
              ) : (
                <StopCircleIcon className="h-4 w-4 text-red-500" />
              )}
              {app.status}
            </td>
          </tr>
          <tr>
            <td>Created at</td>
            <td>{new Date(app.createdAt).toDateString()}</td>
          </tr>
          <tr>
            <td>Updated at</td>
            <td>
              {app.updatedAt ? new Date(app.updatedAt!).toDateString() : "-"}
            </td>
          </tr>
          <tr>
            <td>Domain</td>
            <td className="flex">
              <p className="text-blue-500 hover:underline">{domain}</p>
              <TextCopyButton text={domain} />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}
