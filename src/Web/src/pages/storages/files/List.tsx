import { DocumentIcon, FolderIcon } from "@heroicons/react/24/solid";
import React, { useEffect, useRef, useState } from "react";
import Item, { FolderPathItem } from "./Item";
import { Link, useSearchParams } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { useApp } from "../../apps";
import { Modal, Spinner } from "flowbite-react";
import { useForm } from "react-hook-form";
import { toast } from "react-toastify";

interface PageData {
  items: Item[];
  folderPathItems: FolderPathItem[];
}

type CreateRenameFolderFormInputs = { name: string };

export default function List() {
  const { app } = useApp();
    if (!app) return <Spinner aria-label="Loading..." />
  const { getAccessTokenSilently } = useAuth0();

  const [{ items, folderPathItems }, setPageData] = useState<PageData>({
    items: [],
    folderPathItems: [],
  });

  const [searchParams] = useSearchParams();
  const folderId = searchParams.get("folderId")
    ? parseInt(searchParams.get("folderId")!)
    : undefined;
  const fetchItems = async () => {
    let url = `/api/apps/${app.id}/files`;
    if (folderId) {
      url += `?folderId=${folderId}`;
    }
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    const items = (await res.json()) as Item[];
    const folderPathItems = JSON.parse(
      res.headers.get("Path-Items")!,
    ) as FolderPathItem[];
    setPageData({ items: items, folderPathItems: folderPathItems });
  };
  useEffect(() => {
    fetchItems();
  }, [folderId]);

  const handleUploadFileClick = () => {
    const input = document.createElement("input");
    input.type = "file";
    input.onchange = async (_) => {
      const file = input.files?.item(0);
      if (file) {
        const form = new FormData();
        form.append("file", file);
        const accessToken = await getAccessTokenSilently();
        let url = `/api/apps/${app.id}/files`;
        if (folderId) {
          url += `?folderId=${folderId}`;
        }
        const res = await fetch(url, {
          method: "POST",
          headers: { Authorization: `Bearer ${accessToken}` },
          body: form,
        });

        if (res.ok) {
          const newFile: Item = { ...(await res.json()), type: "File" };
          setPageData((prev) => ({ ...prev, items: [...prev.items, newFile] }));
        }
      }
    };
    input.click();
  };
  const renameFolder = useRef<Item | null>(null);
  const createRenameFolderForm = useForm<CreateRenameFolderFormInputs>();
  const onSumitCreateRenameFolderForm = async (
    inputs: CreateRenameFolderFormInputs,
  ) => {
    const accessToken = await getAccessTokenSilently();
    const isEditMode = renameFolder.current !== undefined;
    let url = `/api/apps/${app.id}/files/folders`;
    let query;
    if (!isEditMode) {
      query = folderId ? `?folderId=${folderId}` : "";
    } else {
      query = `?folderId=${renameFolder.current!.id}`;
    }
    url += query;
    const res = await fetch(url, {
      method: !isEditMode ? "POST" : "PATCH",
      headers: {
        "content-type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify(inputs),
    });

    if (res.ok) {
      setShowCreateRenameModal(false);

      if (!isEditMode) {
        const newFolder: Item = { ...(await res.json()), type: "Folder" };
        setPageData((prev) => ({ ...prev, items: [...prev.items, newFolder] }));
      } else {
        setPageData((prev) => ({
          ...prev,
          items: prev.items.map((item) => {
            if (item.id === renameFolder.current!.id) {
              return { ...item, name: inputs.name };
            }
            return item;
          }),
        }));
      }
    }
  };
  const [showCreateRenameModal, setShowCreateRenameModal] = useState(false);

  const handleRenameClick = (item: Item) => {
    renameFolder.current = item;
    createRenameFolderForm.reset();
    createRenameFolderForm.resetField("name");
    createRenameFolderForm.setValue("name", item.name);
    setShowCreateRenameModal(true);
  };
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const deleteItem = useRef<Item | null>(null);
  const onSubmitDelete = async () => {
    const item = deleteItem.current!;
    const accessToken = await getAccessTokenSilently();
    let url = `/api/apps/${app.id}/files`;
    if (item.type === "File") {
      url += `?fileId=${item.id}`;
    } else {
      url += `?folderId=${item.id}`;
    }
    const res = await fetch(url, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      deleteItem.current = null;
      setShowDeleteConfirm(false);
      setPageData((prev) => ({
        ...prev,
        items: prev.items.filter((i) => i.id !== item.id),
      }));
    }
  };

  const handleDownloadClick = async (item: Item) => {
    const accessToken = await getAccessTokenSilently();
    let url = `/api/apps/${app.id}/files/download`;
    if (item.type === "File") {
      url += `?fileId=${item.id}`;
    } else {
      url += `?folderId=${item.id}`;
    }
    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      const blob = await res.blob();
      const url = URL.createObjectURL(blob);
      const a = document.createElement("a");
      a.href = url;
      a.download = item.name;
      a.click();
    }
  };

  const handleGenerateRouteClick = async (item: Item) => {
    const accessToken = await getAccessTokenSilently();
    let url = `/api/apps/${app.id}/files/generate-route`;
    if (item.type === "File") {
      url += `?fileId=${item.id}`;
    } else {
      url += `?folderId=${item.id}`;
    }
    const res = await fetch(url, {
      method: "POST",
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    if (res.ok) {
      toast.success("Route generated successfully");
    }
  };
  return (
    <div className="p-2">
      <h1>Files</h1>
      <div className="mt-2 flex space-x-1">
        <button
          onClick={handleUploadFileClick}
          className="bg-primary px-2 py-1 text-white"
        >
          Upload File
        </button>
        <button
          onClick={() => {
            if (renameFolder.current) {
              renameFolder.current = null;
            }
            createRenameFolderForm.reset();
            createRenameFolderForm.resetField("name");
            setShowCreateRenameModal(true);
          }}
          className="bg-primary px-2 py-1 text-white"
        >
          New Folder
        </button>
      </div>
      <div className="mt-2 flex space-x-1">
        {folderPathItems.map((item, index) => (
          <React.Fragment key={item.id}>
            <Link
              to={`?folderId=${item.id}`}
              className={item.id === folderId ? "font-semibold" : ""}
            >
              {item.name}
            </Link>
            {index < folderPathItems.length - 1 && <span>{">"}</span>}
          </React.Fragment>
        ))}
      </div>
      <table className="w-full">
        <thead>
          <tr>
            <td></td>
            <td>Name</td>
            <td>Size (B)</td>
            <td>Created At</td>
            <td>Actions</td>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.type + item.id}>
              <td>
                {item.type === "File" ? (
                  <DocumentIcon className="h-4 w-4 text-blue-500" />
                ) : (
                  <FolderIcon className="h-4 w-4 text-yellow-500" />
                )}
              </td>
              <td>
                {item.type === "File" ? (
                  <>{item.name}</>
                ) : (
                  <Link to={`?folderId=${item.id}`}>{item.name}</Link>
                )}
              </td>
              <td>{item.size || "-"}</td>
              <td>{new Date(item.createdAt).toDateString()}</td>
              <td className="flex space-x-1">
                <button
                  onClick={() => handleGenerateRouteClick(item)}
                  className="text-blue-500 hover:underline"
                >
                  Generate Route
                </button>
                <button
                  onClick={() => handleDownloadClick(item)}
                  className="text-blue-500 hover:underline"
                >
                  Download
                </button>
                <button
                  onClick={() => handleRenameClick(item)}
                  className="text-blue-500 hover:underline"
                >
                  Rename
                </button>
                <button
                  onClick={() => {
                    deleteItem.current = item;
                    setShowDeleteConfirm(true);
                  }}
                  className="text-red-500 hover:underline"
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <Modal
        dismissible
        show={showCreateRenameModal}
        onClose={() => {
          setShowCreateRenameModal(false);
        }}
      >
        <form
          onSubmit={createRenameFolderForm.handleSubmit(
            onSumitCreateRenameFolderForm,
          )}
        >
          <Modal.Header>
            {renameFolder.current ? "Rename folder" : "Create new folder"}
          </Modal.Header>
          <Modal.Body>
            <div>
              <input
                type="text"
                {...createRenameFolderForm.register("name", {
                  required: "Folder name is required",
                })}
                className="w-full border px-2 py-1"
                aria-autocomplete="none"
              />
              {createRenameFolderForm.formState.errors.name && (
                <span className="text-red-500">
                  {createRenameFolderForm.formState.errors.name.message}
                </span>
              )}
            </div>
            <div className="mt-2 flex justify-end space-x-1">
              <button type="submit" className="bg-primary px-2 py-1 text-white">
                {renameFolder.current ? "Rename" : "Create"}
              </button>
              <button
                onClick={() => {
                  if (renameFolder.current) {
                    renameFolder.current = null;
                  }
                  setShowCreateRenameModal(false);
                }}
                className="bg-gray-500 px-2 py-1 text-white"
              >
                Cancel
              </button>
            </div>
          </Modal.Body>
        </form>
      </Modal>
      <Modal
        show={showDeleteConfirm}
        onClose={() => {
          deleteItem.current = null;
          setShowDeleteConfirm(false);
        }}
      >
        <div className="p-2">
          <h4 className="font-semibold">Delete confirm</h4>
          <hr className="my-1" />
          <div className="min-h-20 p-2">
            Are you sure you want to delete{" "}
            <span className="font-semibold">
              {deleteItem.current?.type === "File" ? "file" : "folder"}{" "}
              {deleteItem.current?.name}
            </span>
            ?
          </div>
          <div className="mt-2 flex justify-end space-x-1">
            <button
              onClick={onSubmitDelete}
              className="bg-red-500 px-2 py-1 text-white"
            >
              Delete
            </button>
            <button
              onClick={() => {
                deleteItem.current = null;
                setShowDeleteConfirm(false);
              }}
              className="bg-gray-500 px-2 py-1 text-white"
            >
              Cancel
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
