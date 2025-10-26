/// <reference types="vite/client" />
declare const __COMMIT_HASH__: string;

//https://vitejs.dev/guide/env-and-mode#intellisense-for-typescript
interface ImportMetaEnv {
  readonly VITE_AUTH0_DOMAIN: string;
  readonly VITE_AUTH0_CLIENTID: string;
  readonly VITE_AUTH0_AUDIENCE: string;
  readonly VITE_WEBAPP_APIGATEWAY_DOMAIN: string;
  readonly VITE_GITHUB_CLIENTID: string;
  readonly VITE_EDITOR_ORIGIN: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
