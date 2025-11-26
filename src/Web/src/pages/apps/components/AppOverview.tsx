import { useState } from "react";
import { Modal, Button, Dropdown, ModalBody, DropdownItem, ModalHeader, Spinner } from "flowbite-react";
import { useForm } from "react-hook-form";
import { PlayCircleIcon, StopCircleIcon } from "@heroicons/react/24/solid";
import { TextCopyButton } from "@/components/ui";
import { useApp } from "../context";
import { useApiClient } from "@/hooks";
import { getAppDomain } from "../service";

type RenameFormInputs = {
  name: string
}

export default function AppOverview() {
  const { app, setApp } = useApp();
  if (!app) {
    return <Spinner />
  }

  const { send } = useApiClient()

  // Modals state
  const [renameOpen, setRenameOpen] = useState(false);
  const [statusOpen, setStatusOpen] = useState(false);

  const domain = getAppDomain(app.id)

  // Form rename
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RenameFormInputs>({
    defaultValues: { name: app.name },
  });

  const onSubmitRename = async (inputs: RenameFormInputs) => {
    try {
      await send(`/api/apps/${app.id}/rename`, {
        method: "PATCH",
        body: JSON.stringify({ name: inputs.name }),
        headers: {
          "If-Match": app.version
        }
      })

      setApp({...app, name: inputs.name});
      setRenameOpen(false);
    } catch (err) {
      console.error(err);
    }
  };

  // Change status
  const changeStatus = async (newStatus: string) => {
    if (
      app.status === "Active" &&
      !confirm("Are you sure want to deactivate the app?")
    ) {
      return;
    }
    const status = app.status === "Active" ? "Inactive" : "Active";
    await send(`/api/apps/${app.id}/status?status=${status}`, {
      method: "PATCH"
    });
  };

  return (
    <>
      <div className="bg-white shadow rounded-lg p-6 dark:bg-gray-800">
        <h2 className="text-xl font-bold mb-4 text-gray-900 dark:text-gray-100">
          Overview
        </h2>

        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm text-gray-700 dark:text-gray-300">
            <tbody className="divide-y divide-gray-200 dark:divide-gray-700">

              {/* Name + button */}
              <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                <td className="py-2 font-medium">Name</td>
                <td className="py-2 flex items-center justify-between">
                  <span>{app.name}</span>
                  <button
                    onClick={() => setRenameOpen(true)}
                    className="text-xs px-2 py-1 rounded-md bg-gray-100 border border-gray-300 hover:bg-gray-200 
                               dark:bg-gray-700 dark:border-gray-600 dark:hover:bg-gray-600"
                  >
                    Rename
                  </button>
                </td>
              </tr>

              {/* Description */}
              <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                <td className="py-2 font-medium">Description</td>
                <td className="py-2">{app.description}</td>
              </tr>

              {/* Status + button */}
              <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                <td className="py-2 font-medium">Status</td>
                <td className="py-2 flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    {app.status === "Active" ? (
                      <PlayCircleIcon className="h-5 w-5 text-green-500" />
                    ) : (
                      <StopCircleIcon className="h-5 w-5 text-red-500" />
                    )}
                    <span>{app.status}</span>
                  </div>

                  <button
                    onClick={() => setStatusOpen(true)}
                    className="text-xs px-2 py-1 rounded-md bg-gray-100 border border-gray-300 hover:bg-gray-200 
                               dark:bg-gray-700 dark:border-gray-600 dark:hover:bg-gray-600"
                  >
                    Change
                  </button>
                </td>
              </tr>

              {/* Created */}
              <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                <td className="py-2 font-medium">Created At</td>
                <td className="py-2">{new Date(app.createdAt).toDateString()}</td>
              </tr>

              {/* Updated */}
              <tr className="hover:bg-gray-50 dark:hover:bg-gray-700">
                <td className="py-2 font-medium">Updated At</td>
                <td className="py-2">
                  {app.updatedAt ? new Date(app.updatedAt).toDateString() : "-"}
                </td>
              </tr>

              {/* Domain */}
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
      {/* 🔵 MODAL RENAME */}
      <Modal show={renameOpen} size="md" onClose={() => setRenameOpen(false)} popup>
        <ModalHeader />
        <ModalBody>
          <h3 className="text-lg font-semibold mb-4 dark:text-white">Rename App</h3>
          <form onSubmit={handleSubmit(onSubmitRename)}>
            <input
              type="text"
              {...register("name", { required: "Name is required" })}
              className="w-full border rounded px-3 py-2 dark:bg-gray-700 dark:border-gray-600"
              autoComplete="off"
            />
            {errors.name && (
              <p className="text-red-500 text-sm">{errors.name.message}</p>
            )}

            <div className="flex justify-end mt-4 gap-2">
              <Button color="gray" onClick={() => setRenameOpen(false)}>
                Cancel
              </Button>
              <Button type="submit">Save</Button>
            </div>
          </form>
        </ModalBody>
      </Modal>


      {/* 🟣 MODAL CHANGE STATUS */}
      <Modal show={statusOpen} size="md" onClose={() => setStatusOpen(false)} popup>
        <ModalHeader />
        <ModalBody>
          <h3 className="text-lg font-semibold mb-4 dark:text-white">Change Status</h3>

          <Dropdown label={app.status} dismissOnClick={false}>
            <DropdownItem onClick={() => changeStatus("Active")}>
              Active
            </DropdownItem>
            <DropdownItem onClick={() => changeStatus("Inactive")}>
              Inactive
            </DropdownItem>
          </Dropdown>

          <div className="flex justify-end mt-4">
            <Button color="gray" onClick={() => setStatusOpen(false)}>
              Close
            </Button>
          </div>
        </ModalBody>
      </Modal>
    </>
  );
}