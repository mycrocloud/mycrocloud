import { defineConfig, loadEnv } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd());
  return {
    plugins: [react()],
    server: {
      proxy: {
        "/api": {
          target: env.VITE_BASE_API_URL,
          changeOrigin: true,
          rewrite: (path) => path.replace(/^\/api/, ""),
        },
      },
    },
    define: {
      __COMMIT_HASH__: JSON.stringify(process.env.COMMIT_HASH),
    },
  };
});
