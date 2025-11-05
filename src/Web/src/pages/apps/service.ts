import { getConfig } from "@/config";

const { WEBAPP_APIGATEWAY_DOMAIN } = getConfig();
export function getAppDomain(appId: number) {
  const apiGatewayDomain = WEBAPP_APIGATEWAY_DOMAIN;
  return apiGatewayDomain.replace("__app_id__", appId.toString());
}
