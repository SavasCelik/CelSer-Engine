import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
//import './index.css'
import Index from "./shadcn/Index.tsx";
import "./shadcnStyles.css";
import { ThemeProvider } from "@/components/theme-provider";
import TestFlex from "./shadcn/TestFlex.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      {/* <TestFlex /> */}
      <Index />
    </ThemeProvider>
  </StrictMode>
);
