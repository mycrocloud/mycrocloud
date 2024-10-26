import { useAuth0 } from "@auth0/auth0-react";
import { Link, useParams } from "react-router-dom";
import { AppContext } from "../../apps";
import { useContext, useEffect, useRef, useState } from "react";
import ITextStorage from "./ITextStorage";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import { Modal } from "flowbite-react";
import { toast } from "react-toastify";

export default function Logon() {
  const { getAccessTokenSilently } = useAuth0();
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const storageId = useParams()["storageId"]!;
  const [storage, setStorage] = useState<ITextStorage>();

  useEffect(() => {
    const getStorage = async () => {
      const accessToken = await getAccessTokenSilently();
      const res = await fetch(`/api/apps/${app.id}/textstorages/${storageId}`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });
      const storage = (await res.json()) as ITextStorage;
      setStorage(storage);
    };
    getStorage();
  }, []);

  const contentEditorRef = useRef(null);
  const contentEditor = useRef<monaco.editor.IStandaloneCodeEditor>();
  const languages = monaco.languages.getLanguages();
  useEffect(() => {
    contentEditor.current?.dispose();

    contentEditor.current = monaco.editor.create(contentEditorRef.current!, {
      language: "",
      value: "",
      minimap: {
        enabled: false,
      },
    });

    return () => {
      contentEditor.current?.dispose();
    };
  }, []);

  const handleContentEditorLanguageChange = (language: string) => {
    if (contentEditor.current) {
      monaco.editor.setModelLanguage(
        contentEditor.current.getModel()!,
        language,
      );
    }
  };

  const handleReadContentClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      `/api/apps/${app.id}/textstorages/${storageId}/content`,
      {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      },
    );

    const content = await res.text();
    if (contentEditor.current) {
      contentEditor.current.setValue(content);
    }
  };

  const handleWriteContentClick = async () => {
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(
      `/api/apps/${app.id}/textstorages/${storageId}/content`,
      {
        method: "PUT",
        headers: {
          "content-type": "text/plain",
          Authorization: `Bearer ${accessToken}`,
        },
        body: contentEditor.current?.getValue(),
      },
    );

    if (res.ok) {
      toast.success("Content has been written successfully");
    }
  };

  const [openWriteConfirmModal, setOpenWriteConfirmModal] = useState(false);

  return (
    <div className="px-3 py-2">
      <Link to="../" className="text-gray-700">
        Back
      </Link>
      <h1 className="mt-1 font-bold">
        Logon to storage{" "}
        <span className="italic text-primary">{storage?.name}</span>
      </h1>
      <div className="mt-2">
        <div className="flex space-x-1">
          <button
            type="button"
            onClick={handleReadContentClick}
            className="bg-primary px-5 py-1 text-white"
          >
            Read Content
          </button>
          <button
            type="button"
            onClick={() => setOpenWriteConfirmModal(true)}
            className="bg-primary px-5 py-1 text-white"
          >
            Write Content
          </button>
        </div>
        <div className="mt-2">
          <div className="flex">
            <div>Content</div>
            <div className="ms-auto">
              <label>Language</label>
              <select
                onChange={(e) => {
                  handleContentEditorLanguageChange(e.target.value);
                }}
              >
                {languages.map((l) => (
                  <option key={l.id} value={l.id}>
                    {l.aliases?.[0] || l.id}
                  </option>
                ))}
              </select>
            </div>
          </div>
          <div
            className="mt-2"
            style={{ height: "200px", width: "100%" }}
            ref={contentEditorRef}
          ></div>
        </div>
      </div>
      {openWriteConfirmModal && (
        <WriteConfirmModal
          storage={storage!}
          onOk={() => {
            handleWriteContentClick();
            setOpenWriteConfirmModal(false);
          }}
          onCancel={() => {
            setOpenWriteConfirmModal(false);
          }}
        />
      )}
    </div>
  );
}

function WriteConfirmModal({
  storage,
  onOk,
  onCancel,
}: {
  storage: ITextStorage;
  onOk: () => void;
  onCancel: () => void;
}) {
  const [confirmName, setConfirmName] = useState("");
  return (
    <Modal show={true} onClose={onCancel}>
      <div className="p-2">
        <div className="border-b font-semibold">Confirm onverwrite content</div>
        <div className="p-2">
          <p className="">
            Are you sure you want to overwrite the content of the storage?
          </p>
          <div className="mt-2">
            <label className="">
              Please type <span className="font-semibold">{storage.name}</span>{" "}
              to confirm.
            </label>
            <input
              type="text"
              onChange={(e) => setConfirmName(e.target.value)}
              className="mt-2 w-full border px-2 py-1 text-slate-900"
              placeholder={storage.name}
            />
          </div>
        </div>
        <div className="mt-2 flex space-x-1 border-t p-2">
          <button
            type="button"
            disabled={confirmName !== storage.name}
            onClick={onOk}
            className="bg-primary px-5 py-1 text-white disabled:opacity-50"
          >
            Ok
          </button>
          <button
            type="button"
            onClick={onCancel}
            className="bg-gray-500 px-3 py-1 text-white"
          >
            Cancel
          </button>
        </div>
      </div>
    </Modal>
  );
}
