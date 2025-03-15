import { useContext, useEffect, useRef, useState } from "react";
import {
  FieldErrors,
  FormProvider,
  useFieldArray,
  useForm,
  useFormContext,
} from "react-hook-form";
import { yupResolver } from "@hookform/resolvers/yup";
import { AppContext } from "../apps";
import IRoute from "./Route";
import * as monaco from "monaco-editor/esm/vs/editor/editor.api";
import { bodyLanguages, methods } from "./constants";
import {
  RouteCreateUpdateInputs,
  routeCreateUpdateInputsSchema,
} from "./CreateUpdateFormInputs";
import { ChevronDownIcon, ChevronRightIcon } from "@heroicons/react/16/solid";
import { Modal } from "flowbite-react";
import {
  default as FileFolderItem,
  FolderPathItem,
} from "../storages/files/Item";
import {
  ArrowTopRightOnSquareIcon,
  DocumentIcon,
  FolderIcon,
} from "@heroicons/react/24/solid";
import { Link } from "react-router-dom";
import { useAuth0 } from "@auth0/auth0-react";
import React from "react";
const apiGatewayDomain = import.meta.env
  .VITE_WEBAPP_APIGATEWAY_DOMAIN as string;

export default function RouteCreateUpdate({
  route,
  onSubmit,
}: {
  route?: IRoute;
  onSubmit: (data: RouteCreateUpdateInputs) => void;
}) {
  //route = route || sampleRoute;
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const appDomain = apiGatewayDomain.replace("__app_id__", app.id.toString());

  const forms = useForm<RouteCreateUpdateInputs>({
    resolver: yupResolver(routeCreateUpdateInputsSchema),
    defaultValues: {
      name: route?.name || "untitled route",
      method: route?.method || "GET",
      path: route?.path || "/",
      requestQuerySchema: route?.requestQuerySchema,
      requestHeaderSchema: route?.requestHeaderSchema,
      requestBodySchema: route?.requestBodySchema,
      requireAuthorization: route?.requireAuthorization,
      responseType: route?.responseType || "Static",
      responseStatusCode: route?.responseStatusCode || 200,
      responseHeaders: route?.responseHeaders
        ? route.responseHeaders.map((value) => {
            return { name: value.name, value: value.value };
          })
        : [],
      responseBody: route?.responseBody,
      responseBodyLanguage: route?.responseBodyLanguage || "plaintext",
      functionHandler: route?.functionHandler,
      functionHandlerDependencies: route?.functionHandlerDependencies || [],
      useDynamicResponse: route?.useDynamicResponse,
      fileId: route?.fileId,
      enabled: route ? route.enabled : true,
    },
  });
  const {
    register,
    handleSubmit,
    formState: { errors },
    watch,
  } = forms;

  const responseType = watch("responseType");
  const url = appDomain + watch("path");
  const onInvalid = (e: FieldErrors<RouteCreateUpdateInputs>) => {
    console.error(e);
  };
  return (
    <FormProvider {...forms}>
      <form className="h-full p-2" onSubmit={handleSubmit(onSubmit, onInvalid)}>
        {route?.status === "Blocked" && (
          <div className="border border-red-200 bg-red-50 p-2 text-red-700">
            <p>
              This route is blocked because of some reason. Your route will be
              reviewed by our team.
            </p>
          </div>
        )}
        <div className="overflow-y-auto">
          <div>
            <label htmlFor="name">Name</label>
            <input
              id="name"
              type="text"
              {...register("name")}
              autoComplete="none"
              className="inline-block w-full border border-gray-200 px-2 py-1"
            />
            {errors.name && (
              <span className="text-red-500">{errors.name.message}</span>
            )}
          </div>
          <div className="mt-1">
            <label htmlFor="enable" className="flex items-center">
              <input type="checkbox" {...register("enabled")} id="enable" />
              Enable
            </label>
            {errors.enabled && (
              <span className="text-red-500">{errors.enabled.message}</span>
            )}
          </div>
          <section>
            <div className="flex">
              <h3 className="mt-3 border-l-2 border-primary px-1 font-semibold">
                Request
              </h3>
            </div>
            <div className="mt-2">
              <label>Method and Path</label>
              <div className="flex">
                <select
                  className="w-24 border border-gray-200"
                  {...register("method")}
                >
                  {methods.map((m) => (
                    <option key={m} value={m.toUpperCase()}>
                      {m.toUpperCase()}
                    </option>
                  ))}
                </select>
                <input
                  autoComplete="none"
                  type="text"
                  className="inline-block flex-1 border border-gray-200 px-2 py-1"
                  {...register("path")}
                />
              </div>
              {errors.method && (
                <span className="text-red-500">{errors.method.message}</span>
              )}
              {errors.path && (
                <span className="text-red-500">{errors.path.message}</span>
              )}
              <div className="mt-1">
                <small className="me-2">URL:</small>
                <a
                  className="inline-flex text-blue-500 hover:underline"
                  href={url}
                  target="_blank"
                >
                  <small>{url}</small>
                  <ArrowTopRightOnSquareIcon className="ms-0.5 h-4 w-4" />
                </a>
                <button
                  type="button"
                  onClick={() => navigator.clipboard.writeText(url)}
                  className="ms-3 text-blue-500 hover:underline"
                >
                  <small>Copy</small>
                </button>
              </div>
            </div>
            <div className="mt-1">
              <div className="p-1">
                <input
                  id="requireAuthorization"
                  type="checkbox"
                  {...register("requireAuthorization")}
                  className="me-1 inline-block border border-gray-200 px-2 py-1"
                />
                <label htmlFor="requireAuthorization" className="mt-2">
                  Require Authorization
                </label>
              </div>
              {errors.requireAuthorization && (
                <span className="text-red-500">
                  {errors.requireAuthorization.message}
                </span>
              )}
            </div>
            <div className="mt-1">
              <RequestValidation />
            </div>
          </section>
          <section>
            <h3 className="mt-3 border-l-2 border-primary pl-1 font-semibold">
              Response
            </h3>
            <div className="mt-1">
              <label className="me-1">Type</label>
              <select {...register("responseType")}>
                {["Static", "StaticFile", "Function"].map((t) => (
                  <option key={t}>{t}</option>
                ))}
              </select>
            </div>

            <div className="mt-1">
              {responseType === "Static" && <StaticResponse />}
              {responseType === "StaticFile" && (
                <StaticFile
                  file={
                    route?.fileId
                      ? {
                          id: route.fileId!,
                          name: route.fileName!,
                          folderId: route.fileFolderId!,
                        }
                      : undefined
                  }
                />
              )}
              {responseType === "Function" && <FunctionHandler />}
            </div>
          </section>
        </div>
        <div className="sticky bottom-0 mt-2">
          <button
            type="submit"
            className="border bg-primary px-3 py-1 text-center font-medium text-white enabled:hover:bg-cyan-700"
            disabled={route?.status === "Blocked"}
          >
            Save
          </button>
        </div>
      </form>
    </FormProvider>
  );
}

const quickAddResponseHeaderButtons = [
  { text: "Add", key: "", value: "" },
  { text: "text/css", key: "content-type", value: "text/css" },
  { text: "text/csv", key: "content-type", value: "text/csv" },
  { text: "text/html", key: "content-type", value: "text/html" },
  { text: "image/jpeg", key: "content-type", value: "image/jpeg" },
  { text: "text/javascript", key: "content-type", value: "text/javascript" },
  { text: "application/json", key: "content-type", value: "application/json" },
  { text: "image/png", key: "content-type", value: "image/png" },
  { text: "application/pdf", key: "content-type", value: "application/pdf" },
  { text: "image/svg+xml", key: "content-type", value: "image/svg+xml" },
  { text: "text/plain", key: "content-type", value: "text/plain" },
];

function RequestValidation() {
  const {
    getValues,
    setValue,
    formState: { errors },
  } = useFormContext<RouteCreateUpdateInputs>();
  const [tab, setTab] = useState("requestQuerySchema");
  const editorEl = useRef<HTMLDivElement>(null);
  const editor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);
  const requestQuerySchemaModel = useRef<monaco.editor.ITextModel | null>(null);
  const requestHeaderSchemaModel = useRef<monaco.editor.ITextModel | null>(
    null,
  );
  const requestBodySchemaModel = useRef<monaco.editor.ITextModel | null>(null);

  useEffect(() => {
    requestQuerySchemaModel.current?.dispose();
    requestHeaderSchemaModel.current?.dispose();
    requestBodySchemaModel.current?.dispose();
    editor.current?.dispose();

    requestQuerySchemaModel.current = monaco.editor.createModel(
      getValues("requestQuerySchema") || "",
      "json",
    );
    requestQuerySchemaModel.current.onDidChangeContent(() => {
      const value = requestQuerySchemaModel.current!.getValue();
      setValue("requestQuerySchema", value);
    });

    requestHeaderSchemaModel.current = monaco.editor.createModel(
      getValues("requestHeaderSchema") || "",
      "json",
    );
    requestHeaderSchemaModel.current.onDidChangeContent(() => {
      const value = requestHeaderSchemaModel.current!.getValue();
      setValue("requestHeaderSchema", value);
    });

    requestBodySchemaModel.current = monaco.editor.createModel(
      getValues("requestBodySchema") || "",
      "json",
    );
    requestBodySchemaModel.current.onDidChangeContent(() => {
      const value = requestBodySchemaModel.current!.getValue();
      setValue("requestBodySchema", value);
    });

    editor.current = monaco.editor.create(editorEl.current!, {
      model: requestQuerySchemaModel.current,
      automaticLayout: true,
    });

    return () => {
      requestQuerySchemaModel.current?.dispose();
      requestHeaderSchemaModel.current?.dispose();
      requestBodySchemaModel.current?.dispose();
      editor.current?.dispose();
    };
  }, []);

  useEffect(() => {
    if (!editor.current) {
      return;
    }
    switch (tab) {
      case "requestQuerySchema":
        editor.current.setModel(requestQuerySchemaModel.current!);
        break;
      case "requestHeaderSchema":
        editor.current.setModel(requestHeaderSchemaModel.current!);
        break;
      case "requestBodySchema":
        editor.current.setModel(requestBodySchemaModel.current!);
        break;
      default:
        break;
    }
  }, [tab]);

  const [show, setShow] = useState(false);

  return (
    <div>
      <button
        type="button"
        onClick={() => setShow(!show)}
        className="inline-flex items-center"
      >
        {show ? (
          <ChevronDownIcon className="h-4 w-4 text-blue-500" />
        ) : (
          <ChevronRightIcon className="h-4 w-4 text-blue-500" />
        )}
        Validation
      </button>
      <div className={`p-1 ${show ? "" : "hidden"}`}>
        <div className="flex space-x-2">
          <button
            type="button"
            onClick={() => setTab("requestQuerySchema")}
            className={
              tab === "requestQuerySchema" ? "border-b-2 border-primary" : ""
            }
          >
            Query Params
          </button>
          <button
            type="button"
            onClick={() => setTab("requestHeaderSchema")}
            className={
              tab === "requestHeaderSchema" ? "border-b-2 border-primary" : ""
            }
          >
            Headers
          </button>
          <button
            type="button"
            onClick={() => setTab("requestBodySchema")}
            className={
              tab === "requestBodySchema" ? "border-b-2 border-primary" : ""
            }
          >
            Body
          </button>
        </div>
        <div ref={editorEl} className="mt-1 h-[200px] w-full"></div>
        {errors.requestQuerySchema && (
          <span className="block text-red-500">
            {errors.requestQuerySchema.message}
          </span>
        )}
        {errors.requestHeaderSchema && (
          <span className="block text-red-500">
            {errors.requestHeaderSchema.message}
          </span>
        )}
        {errors.requestBodySchema && (
          <span className="block text-red-500">
            {errors.requestBodySchema.message}
          </span>
        )}
      </div>
    </div>
  );
}

function StaticResponse() {
  const {
    control,
    register,
    formState: { errors },
    setValue,
    getValues,
    watch,
  } = useFormContext<RouteCreateUpdateInputs>();
  const {
    fields: responseHeaders,
    append: addResponseHeaders,
    remove: removeResponseHeader,
  } = useFieldArray({ control, name: "responseHeaders" });

  const bodyEditorRef = useRef<HTMLDivElement>(null);
  const bodyEditor = useRef<monaco.editor.IStandaloneCodeEditor | null>(null);

  useEffect(() => {
    bodyEditor.current?.dispose();

    bodyEditor.current = monaco.editor.create(bodyEditorRef.current!, {
      language: getValues("responseBodyLanguage"),
      value: getValues("responseBody") || undefined,
      minimap: { enabled: false },
    });
    bodyEditor.current.onDidChangeModelContent(() => {
      setValue("responseBody", bodyEditor.current!.getValue());
    });

    return () => {
      bodyEditor.current?.dispose();
    };
  }, []);

  const responseBodyLanguage = watch("responseBodyLanguage");
  useEffect(() => {
    if (bodyEditor.current && responseBodyLanguage) {
      monaco.editor.setModelLanguage(
        bodyEditor.current!.getModel()!,
        responseBodyLanguage,
      );
    }
  }, [responseBodyLanguage]);
  return (
    <>
      <div className="mt-2">
        <label htmlFor="responseStatusCode" className="block">
          Status Code
        </label>
        <input
          id="responseStatusCode"
          type="number"
          {...register("responseStatusCode")}
          autoComplete="none"
          className="block w-1/6 border border-gray-200 px-2 py-1"
        />
        {errors.responseStatusCode && (
          <span className="text-red-500">
            {errors.responseStatusCode.message}
          </span>
        )}
      </div>
      <div className="mt-2">
        <label htmlFor="header">Headers</label>
        <div className="flex flex-col space-y-0.5">
          {responseHeaders.map((header, index) => (
            <div key={header.id} className="flex space-x-1">
              <input
                id={`responseHeaders[${index}].name`}
                type="text"
                {...register(`responseHeaders.${index}.name` as const)}
                autoComplete="none"
                className="border border-gray-200 px-2 py-1"
              />
              <input
                id={`responseHeaders[${index}].value`}
                type="text"
                {...register(`responseHeaders.${index}.value` as const)}
                autoComplete="none"
                className="border border-gray-200 px-2 py-1"
              />
              <button
                type="button"
                onClick={() => removeResponseHeader(index)}
                className="text-red-600"
              >
                Remove
              </button>
            </div>
          ))}
        </div>
        <div className="flex flex-wrap space-x-2">
          {quickAddResponseHeaderButtons.map((button, index) => (
            <button
              key={index}
              type="button"
              onClick={() =>
                addResponseHeaders({ name: button.key, value: button.value })
              }
              className="mt-1 text-blue-600 hover:underline"
            >
              {button.text}
            </button>
          ))}
        </div>
      </div>
      <div className="mt-2">
        <label className="block">Body</label>
        <div className="mt-1 flex">
          <div>
            <label
              htmlFor="useDynamicResponse"
              className="mt-2 flex items-center"
            >
              <input
                id="useDynamicResponse"
                type="checkbox"
                {...register("useDynamicResponse")}
                className="inline-block border border-gray-200 px-2 py-1"
              />
              Use dynamic response
            </label>
          </div>
          <div className="ms-auto">
            <label htmlFor="responseBodyLanguage">Editor format</label>
            <select {...register("responseBodyLanguage")}>
              {bodyLanguages.map((l) => (
                <option key={l}>{l}</option>
              ))}
            </select>
          </div>
        </div>
        <div className="mt-1">
          <div
            ref={bodyEditorRef}
            style={{ width: "100%", height: "300px" }}
          ></div>
          {errors.responseBody && (
            <p className="text-red-500">{errors.responseBody.message}</p>
          )}
        </div>
      </div>
    </>
  );
}

interface PageData {
  items: FileFolderItem[];
  folderPathItems: FolderPathItem[];
}

interface IFile {
  id: number;
  name: string;
  folderId: number;
}

function StaticFile({ file }: { file?: IFile }) {
  const { app } = useContext(AppContext)!;
  if (!app) throw new Error();
  const { getAccessTokenSilently } = useAuth0();
  const {
    control,
    register,
    formState: { errors },
    setValue,
  } = useFormContext<RouteCreateUpdateInputs>();

  const {
    fields: responseHeaders,
    append: addResponseHeaders,
    remove: removeResponseHeader,
  } = useFieldArray({ control, name: "responseHeaders" });
  const selectedFile = useRef<IFile | undefined>(file);
  const [modalSelectedFile, setModalSelectedFile] = useState<IFile | undefined>(
    file,
  );
  const [folderId, setFolderId] = useState<number | undefined>(file?.folderId);
  const [showModal, setShowModal] = useState(false);
  const handleChooseFileClick = () => {
    setModalSelectedFile(selectedFile.current);
    setShowModal(true);
  };
  const [{ items, folderPathItems }, setPageData] = useState<PageData>({
    items: [],
    folderPathItems: [],
  });

  const handleFolderClick = (
    e: React.MouseEvent<HTMLAnchorElement>,
    folderId: number,
  ) => {
    e.preventDefault();
    setFolderId(folderId);
  };

  const fetchItems = async () => {
    let url = `/api/apps/${app.id}/files`;
    if (folderId) {
      url += `?folderId=${folderId}`;
    }
    const accessToken = await getAccessTokenSilently();
    const res = await fetch(url, {
      headers: { Authorization: `Bearer ${accessToken}` },
    });
    const items = (await res.json()) as FileFolderItem[];
    const folderPathItems = JSON.parse(
      res.headers.get("Path-Items")!,
    ) as FolderPathItem[];
    setPageData({ items: items, folderPathItems: folderPathItems });
  };

  useEffect(() => {
    if (showModal) {
      fetchItems();
    }
  }, [showModal, folderId]);

  const handleFileSelect = (file: FileFolderItem) => {
    setModalSelectedFile({
      id: file.id,
      name: file.name,
      folderId: file.parentId,
    });
  };
  const handleChooseClick = () => {
    selectedFile.current = modalSelectedFile;
    setValue("fileId", selectedFile.current?.id);
    setShowModal(false);
  };
  return (
    <div>
      <div className="mt-2">
        <label htmlFor="header">Headers</label>
        <div className="flex flex-col space-y-0.5">
          {responseHeaders.map((header, index) => (
            <div key={header.id} className="flex space-x-1">
              <input
                id={`responseHeaders[${index}].name`}
                type="text"
                {...register(`responseHeaders.${index}.name` as const)}
                autoComplete="none"
                className="border border-gray-200 px-2 py-1"
              />
              <input
                id={`responseHeaders[${index}].value`}
                type="text"
                {...register(`responseHeaders.${index}.value` as const)}
                autoComplete="none"
                className="border border-gray-200 px-2 py-1"
              />
              <button
                type="button"
                onClick={() => removeResponseHeader(index)}
                className="text-red-600"
              >
                Remove
              </button>
            </div>
          ))}
        </div>
        <div className="flex flex-wrap space-x-2">
          {quickAddResponseHeaderButtons.map((button, index) => (
            <button
              key={index}
              type="button"
              onClick={() =>
                addResponseHeaders({ name: button.key, value: button.value })
              }
              className="mt-1 text-blue-600 hover:underline"
            >
              {button.text}
            </button>
          ))}
        </div>
      </div>
      <div className="mt-2">
        <div>File</div>
        {selectedFile.current ? (
          <Link
            to={`/apps/${app.id}/storages/files?folderId=${selectedFile.current!.folderId}`}
            className="block text-blue-500 hover:underline"
          >
            {selectedFile.current!.name}
          </Link>
        ) : (
          <p>Choose a file</p>
        )}
        <input type="hidden" {...register("fileId")} />
        {errors.fileId && (
          <span className="text-red-500">{errors.fileId.message}</span>
        )}
        <button
          type="button"
          onClick={handleChooseFileClick}
          className="mt-1 bg-blue-500 px-2 py-1 text-white"
        >
          Choose
        </button>
        <Modal show={showModal} onClose={() => setShowModal(false)}>
          <div className="p-2">
            <h3 className="font-semibold">Choose a file</h3>
            <hr className="my-1" />
            <div className="min-h-56">
              <div className="mt-2 flex space-x-1">
                {folderPathItems.map((item, index) => (
                  <React.Fragment key={item.id}>
                    <Link
                      to={``}
                      className={item.id === folderId ? "font-semibold" : ""}
                      onClick={(e) => handleFolderClick(e, item.id)}
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
                    <td></td>
                    <td>Name</td>
                    <td>Size (B)</td>
                    <td>Created At</td>
                  </tr>
                </thead>
                <tbody>
                  {items.map((item) => (
                    <tr key={item.type + item.id}>
                      <td>
                        <input
                          type="radio"
                          name="file"
                          value={item.id}
                          disabled={item.type === "Folder"}
                          onChange={() => {
                            handleFileSelect(item);
                          }}
                          checked={item.id === modalSelectedFile?.id}
                        />
                      </td>
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
                          <Link
                            to={""}
                            onClick={(e) => handleFolderClick(e, item.id)}
                          >
                            {item.name}
                          </Link>
                        )}
                      </td>
                      <td>{item.size || "-"}</td>
                      <td>{new Date(item.createdAt).toDateString()}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <hr className="my-1" />
            <div className="mt-1 flex justify-end space-x-1">
              <button
                onClick={handleChooseClick}
                className="bg-primary px-2 py-1 text-white disabled:opacity-50"
                disabled={selectedFile === undefined}
              >
                Choose
              </button>
              <button
                onClick={() => setShowModal(false)}
                className="bg-gray-500 px-2 py-1 text-white"
              >
                Cancel
              </button>
            </div>
          </div>
        </Modal>
      </div>
    </div>
  );
}
function FunctionHandler() {
  const {
    formState: { errors },
    setValue,
    getValues,
  } = useFormContext<RouteCreateUpdateInputs>();
  const handlerEditorRef = useRef<HTMLDivElement>(null);
  const handlerEditor = useRef<monaco.editor.IStandaloneCodeEditor | null>(
    null,
  );

  const sampleFunctionHandler = `function handler(req) {
  return {
    statusCode: 200,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ message: "Hello, world!" }),
  }
}`;

  useEffect(() => {
    handlerEditor.current?.dispose();

    const functionHandler = getValues("functionHandler");
    if (!functionHandler) {
      console.log("setting sampleFunctionHandler");
      setValue("functionHandler", sampleFunctionHandler);
    }
    handlerEditor.current = monaco.editor.create(handlerEditorRef.current!, {
      language: "javascript",
      value: functionHandler || sampleFunctionHandler,
      minimap: { enabled: false },
    });
    handlerEditor.current.onDidChangeModelContent(() => {
      setValue("functionHandler", handlerEditor.current!.getValue());
    });

    return () => {
      handlerEditor.current?.dispose();
    };
  }, []);

  return (
    <>
      <div className="mt-1">
        <label>Handler</label>
        <div
          ref={handlerEditorRef}
          style={{ width: "100%", height: "200px" }}
        ></div>
        {errors.functionHandler && (
          <p className="text-red-500">{errors.functionHandler.message}</p>
        )}
      </div>
    </>
  );
}
