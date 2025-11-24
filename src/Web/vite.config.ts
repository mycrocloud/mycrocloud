import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";
import mkcert from "vite-plugin-mkcert";
import tailwindcss from '@tailwindcss/vite'
import flowbiteReact from "flowbite-react/plugin/vite";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd());
  return {
    plugins: [
      react(),
      tailwindcss(),
      flowbiteReact(),
      env.VITE_DEV_HOST ? mkcert() : null,
      {
        name: "inject-config-tag",
        apply: "build",
        transformIndexHtml(html) {
          const version =
            process.env.COMMIT_HASH?.slice(0, 7) ||
            process.env.BUILD_ID ||
            Date.now();
          return html.replace(
            "</title>",
            `</title>\n\t\t<script src="/_config.js?v=${version}"></script>`,
          );
        },
      },
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