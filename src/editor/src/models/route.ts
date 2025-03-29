export default interface IRoute {
  id: number;
  name: string;
  path: string;
  method: string;
  requestQuerySchema?: string;
  requestHeaderSchema?: string;
  requestBodySchema?: string;
  responseType: string;
  responseStatusCode?: number;
  responseHeaders?: IResponseHeader[];
  responseBodyLanguage?: string;
  responseBody?: string;
  functionHandler?: string;
  functionHandlerDependencies?: string[];
  requireAuthorization: boolean;
  status: string;
  useDynamicResponse: boolean;
  fileId?: number;
  fileName?: string;
  fileFolderId?: number;
  enabled: boolean;
}

export interface IResponseHeader {
  name: string;
  value: string;
}
