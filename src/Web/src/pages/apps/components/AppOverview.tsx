import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import { TextCopyButton } from "@/components/ui";
import IApp from "../App";

export default function AppOverview({ app, domain }: { app: IApp; domain: string }) {
  return (
    <div className="bg-white shadow rounded-lg p-6 dark:bg-gray-800">
      <h2 className="text-xl font-bold mb-4 text-gray-900 dark:text-gray-100">Overview</h2>
      <div className="overflow-x-auto">
        <table className="w-full text-left text-sm text-gray-700 dark:text-gray-300">
          <tbody className="divide-y divide-gray-200 dark:divide-gray-700">
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Name</td>
              <td className="py-2">{app.name}</td>
            </tr>
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Description</td>
              <td className="py-2">{app.description}</td>
            </tr>
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Status</td>
              <td className="py-2 flex items-center gap-2">
                {app.status === "Active" ? (
                  <PlayCircleIcon className="h-5 w-5 text-green-500" />
                ) : (
                  <StopCircleIcon className="h-5 w-5 text-red-500" />
                )}
                <span>{app.status}</span>
              </td>
            </tr>
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Created At</td>
              <td className="py-2">{new Date(app.createdAt).toDateString()}</td>
            </tr>
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Updated At</td>
              <td className="py-2">
                {app.updatedAt ? new Date(app.updatedAt).toDateString() : "-"}
              </td>
            </tr>
            <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
              <td className="py-2 font-medium">Domain</td>
              <td className="py-2 flex items-center gap-2">
                <a
                  href={`https://${domain}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-blue-500 hover:underline"
                >
                  {domain}
                </a>
                <TextCopyButton text={domain} />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  );
}