import { defineConfig } from "vite";
import plugin from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "path";

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [tailwindcss(), plugin()],
  resolve: {
    alias: {
      // /esm/icons/index.mjs only exports the icons statically, so no separate chunks are created
      "@tabler/icons-react": "@tabler/icons-react/dist/esm/icons/index.mjs",
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 49356,
  },
});
