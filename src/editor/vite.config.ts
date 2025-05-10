import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vitejs.dev/config/
export default defineConfig(({ }) => {
  return {
    plugins: [react()],
    define: {
      __COMMIT_HASH__: JSON.stringify(process.env.COMMIT_HASH),
    },
  };
});
