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
import {
  ArrowTopRightOnSquareIcon,
} from "@heroicons/react/24/solid";
import { getConfig } from "@/config";
const { WEBAPP_APIGATEWAY_DOMAIN } = getConfig();
const apiGatewayDomain = WEBAPP_APIGATEWAY_DOMAIN;
const { EDITOR_ORIGIN } = getConfig();

export default function RouteCreateUpdate({
  route,
  onSubmit,
}: {
  route?: IRoute;
  onSubmit: (data: RouteCreateUpdateInputs) => void;
}) {
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
      response: route?.response,
      responseBodyLanguage: route?.responseBodyLanguage || "plaintext",
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
              {responseType === "Function" && <FunctionHandler />}
              {errors.response && (
                <p className="text-red-500">{errors.response.message}</p>
              )}
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
      value: getValues("response") || undefined,
      minimap: { enabled: false },
    });
    bodyEditor.current.onDidChangeModelContent(() => {
      setValue("response", bodyEditor.current!.getValue());
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
          {errors.response && (
            <p className="text-red-500">{errors.response.message}</p>
          )}
        </div>
      </div>
    </>
  );
}

function validEditorMessage(e: MessageEvent, editor: string) {
  if (e.origin !== EDITOR_ORIGIN) return false;
  if (e.data.editorId !== editor) return false;
  return true;
}

function FunctionHandler() {
  const {
    formState: { errors },
    setValue,
    getValues,
  } = useFormContext<RouteCreateUpdateInputs>();

  const editorId = "functionHandler";
  const editorRef = useRef<HTMLIFrameElement>(null);
  const [editorLoaded, setEditorLoaded] = useState(false);

  const sampleFunctionHandler = `function handler(req) {
  return {
    statusCode: 200,
    headers: { "content-type": "application/json" },
    body: JSON.stringify({ message: "Hello, world!" }),
  }
}`;

  useEffect(() => {
    const functionHandler = getValues("response");
    if (!functionHandler) {
      setValue("response", sampleFunctionHandler);
    }

    const onMessage = (e: MessageEvent) => {
      if (!validEditorMessage(e, editorId)) return;

      const { type, payload } = e.data;
      switch (type) {
        case "loaded":
          setEditorLoaded(true);
          break;
        case "changed":
          setValue("response", payload);
          break;
        default:
          break;
      }
    };

    window.addEventListener("message", onMessage);

    return () => {
      window.removeEventListener("message", onMessage);
    };
  }, []);

  useEffect(() => {
    if (!editorLoaded) return;

    editorRef.current?.contentWindow?.postMessage(
      {
        editorId,
        type: "load",
        payload: {
          value: getValues("response"),
          language: "javascript",
        },
      },
      EDITOR_ORIGIN,
    );
  }, [editorLoaded]);

  return (
    <div className="mt-1">
      <label>Handler</label>
      <iframe
        ref={editorRef}
        src={EDITOR_ORIGIN + "?id=" + editorId}
        style={{ width: "100%", height: "200px" }}
      />
      {errors.response && (
        <p className="text-red-500">{errors.response.message}</p>
      )}
    </div>
  );
}
