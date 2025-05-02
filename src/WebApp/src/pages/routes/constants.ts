import { IRoute } from ".";

export const methods = ["get", "post", "put", "delete", "patch"];
export const responseBodyRenderTemplateEngines = ["handlebars", "mustache"];
export const bodyLanguages = [
  "json",
  "form",
  "plaintext",
  "html",
  "javascript",
  "xml",
  "yaml",
  "markdown",
  "css",
  "handlebars",
];

export const sampleRoute: IRoute = {
  id: 0,
  status: "Active",
  name: "untitled",
  method: "GET",
  path: "/",
  requireAuthorization: false,
  requestQuerySchema: "",
  requestHeaderSchema: "",
  requestBodySchema: "",
  responseType: "Static",
  responseStatusCode: 200,
  responseHeaders: [{ name: "content-type", value: "text/plain" }],
  useDynamicResponse: false,
  responseBodyLanguage: "plaintext",
  responseBody: "Hello, world!",
  functionHandler: "",
  enabled: true,
};

export const functionExecutionEnvironmentMap = new Map<number, string>(
  [[1, "In-process (Queued)"], [2, "Isolated (Queued)"]],
)