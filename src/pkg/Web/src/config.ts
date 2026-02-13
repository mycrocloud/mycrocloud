interface Config {
  AUTH0_DOMAIN: string;
  AUTH0_CLIENTID: string;
  AUTH0_AUDIENCE: string;
  WEBAPP_APIGATEWAY_DOMAIN: string;
  GITHUB_APP_NAME: string;
  EDITOR_ORIGIN: string;
  SLACK_CLIENT_ID: string;
}

export const getConfig = (): Config => {
  if (typeof window !== "undefined" && (window as any).CONFIG) {
    return (window as any).CONFIG;
  }

  return {
    AUTH0_DOMAIN: import.meta.env.VITE_AUTH0_DOMAIN,
    AUTH0_CLIENTID: import.meta.env.VITE_AUTH0_CLIENTID,
    AUTH0_AUDIENCE: import.meta.env.VITE_AUTH0_AUDIENCE,
    WEBAPP_APIGATEWAY_DOMAIN: import.meta.env.VITE_WEBAPP_APIGATEWAY_DOMAIN,
    GITHUB_APP_NAME: import.meta.env.VITE_GITHUB_APP_NAME,
    EDITOR_ORIGIN: import.meta.env.VITE_EDITOR_ORIGIN,
    SLACK_CLIENT_ID: import.meta.env.VITE_SLACK_CLIENT_ID,
  };
};
