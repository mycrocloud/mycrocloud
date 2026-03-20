import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";
import mkcert from "vite-plugin-mkcert";
import { injectScriptTag } from "./plugins/inject-script-tag";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd());
  return {
    plugins: [
      react(),
      env.VITE_DEV_HOST ? mkcert() : null,
      injectScriptTag({ src: "/_config.js", versionEnvVars: ["COMMIT_HASH", "BUILD_ID"] }),
    ],
    server: {
      host: env.VITE_DEV_HOST || undefined,
      proxy: {
        "/api": {
          target: env.VITE_DEV_BASE_API_URL,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ""),
        },
      },
    },
    define: {
      __COMMIT_HASH__: JSON.stringify(process.env.COMMIT_HASH),
    },
    resolve: {
      alias: {
        "@": path.resolve(__dirname, "./src"),
      },
    },
  };
});
