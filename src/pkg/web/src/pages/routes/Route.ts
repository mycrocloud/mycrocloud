export default interface IRoute {
  id: number;
  name: string;
  path: string;
  method: string;
  requestQuerySchema?: string;
  requestHeaderSchema?: string;
  requestBodySchema?: string;
  response: IRouteResponse;
  requireAuthorization: boolean;
  status: string;
  enabled: boolean;
}

export interface IRouteResponse {
  type: "Static" | "Function";
  staticResponse?: {
    statusCode?: number;
    headers?: IResponseHeader[];
    content?: string;
  } | null;
  functionResponse?: {
    sourceCode?: string;
  } | null;
}

export interface IResponseHeader {
  name: string;
  value: string;
}
