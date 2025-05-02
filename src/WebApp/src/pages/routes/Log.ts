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
  additionalLogMessage?: string;
  functionExecutionEnvironment?: number;
  functionExecutionDuration?: number;
  requestContentLength?: number;
  requestContentType?: string;
  requestCookie?: string;
  requestFormContent?: string;
  requestHeaders?: string;
}
