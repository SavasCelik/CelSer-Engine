import { StrictMode, use } from "react";
import { createRoot } from "react-dom/client";
//import './index.css'
import Index from "./shadcn/Index.tsx";
import "./shadcnStyles.css";
import { ThemeProvider } from "@/components/theme-provider";
import TestFlex from "./shadcn/TestFlex.tsx";
import MainUiTester from "./MainUiTester.tsx";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <MainUiTester />
  </StrictMode>,
);
