import { getConfig } from "@/config";

const { WEBAPP_APIGATEWAY_DOMAIN } = getConfig();

function slugify(text: string): string {
  return text.toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9-]/g, "");
}

export function getAppDomain(appName: string) {
  const appSlug = slugify(appName);
  return WEBAPP_APIGATEWAY_DOMAIN.replace("__app_id__", appSlug);
}
