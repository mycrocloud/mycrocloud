import { DocumentIcon, FolderIcon } from "@heroicons/react/24/solid";
import { useContext, useEffect, useRef, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import { AppContext } from "../../apps";
import { Modal } from "flowbite-react";
import Object from "./Object";
import { downloadFile } from "../../../utils";

export default function List() {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();

  const [objects, setObjects] = useState<Object[]>([]);

  const [searchParams] = useSearchParams();
  const prefix = searchParams.get("prefix")
    ? parseInt(searchParams.get("folderId")!)
    : undefined;
  const fetchItems = async () => {
    let url = `/api/apps/${app.id}/objects`;
    if (prefix) {
      url += `?prefix=${prefix}`;
    }
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(url, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });
    const objects = (await res.json()) as Object[];
    setObjects(objects);
  };
  useEffect(() => {
    fetchItems();
  }, [prefix]);

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
        if (prefix) {
          url += `?prefix=${prefix}`;
        }
        const res = await fetch(url, {
          method: "POST",
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
          body: form,
        });

        if (res.ok) {
          // const newFile: Item = {
          //   ...(await res.json()),
          //   type: "File",
          // };
          // setPageData((prev) => ({
          //   ...prev,
          //   items: [...prev.items, newFile],
          // }));
        }
      }
    };
    input.click();
  };

  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const deleteItem = useRef<Object>();
  const onSubmitDelete = async () => {
    const item = deleteItem.current!;
    const accessToken = await getAccessTokenSilently();

    const res = await fetch(`/api/apps/${app.id}/objects/${item.key}`, {
      method: "DELETE",
      headers: {
        Authorization: `Bearer ${accessToken}`,
      },
    });

    if (res.ok) {
      deleteItem.current = undefined;
      setShowDeleteConfirm(false);
      setObjects((prev) => prev.filter((i) => i.key !== item.key));
    }
  };

  const handleDownloadClick = async (object: Object) => {
    const accessToken = await getAccessTokenSilently();
    await downloadFile(
      `/api/apps/${app.id}/objects/${object.key}`,
      {
        Authorization: `Bearer ${accessToken}`,
      },
      object.key,
    );
  };

  return (
    <div className="p-2">
      <h1>Objects</h1>
      <div className="mt-2 flex space-x-1">
        <button
          onClick={handleUploadFileClick}
          className="bg-primary px-2 py-1 text-white"
        >
          Upload File
        </button>
      </div>
      <div className="mt-2 flex space-x-1">
        {/* {folderPathItems.map((seg, index) => (
          <React.Fragment key={index}>
            <Link to={`?prefix=${seg}`} className="font-semibold">
              {seg}
            </Link>
            {index < folderPathItems.length - 1 && <span>{"/"}</span>}
          </React.Fragment>
        ))} */}
        {prefix}
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
          {objects.map((obj) => (
            <tr key={obj.key}>
              <td>
                {true ? (
                  <DocumentIcon className="h-4 w-4 text-blue-500" />
                ) : (
                  <FolderIcon className="h-4 w-4 text-yellow-500" />
                )}
              </td>
              <td>{true ? <>{obj.key}</> : <Link to={``}>{obj.key}</Link>}</td>
              <td>{obj.size || "-"}</td>
              <td>{new Date(obj.createdAt).toDateString()}</td>
              <td className="flex space-x-1">
                <button className="text-blue-500 hover:underline">
                  Generate Route
                </button>
                <button
                  onClick={() => handleDownloadClick(obj)}
                  className="text-blue-500 hover:underline"
                >
                  Download
                </button>

                <button
                  onClick={() => {
                    deleteItem.current = obj;
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
        show={showDeleteConfirm}
        onClose={() => {
          deleteItem.current = undefined;
          setShowDeleteConfirm(false);
        }}
      >
        <div className="p-2">
          <h4 className="font-semibold">Delete confirm</h4>
          <hr className="my-1" />
          <div className="min-h-20 p-2">
            Are you sure you want to delete object
            <span className="font-semibold">{deleteItem.current?.key}</span>?
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
                deleteItem.current = undefined;
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
