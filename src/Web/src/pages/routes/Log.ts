export default interface ILog {
  id: string;
  timestamp: string;
  remoteAddress: string;
  appId: number;
  routeId?: number;
  routeName?: string;
  method: string;
  path: string;
  statusCode: number;
  functionLogs: null | IFunctionLog[];
  functionExecutionEnvironment?: number;
  functionExecutionDuration?: number;
  requestContentLength?: number;
  requestContentType?: string;
  requestCookie?: string;
  requestFormContent?: string;
  requestHeaders?: string;
}

interface IFunctionLog {
  message: string;
  timestamp: string;
  type: string
}
