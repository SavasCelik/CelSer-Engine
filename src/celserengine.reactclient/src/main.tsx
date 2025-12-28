import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import "./index.css";
import { ThemeProvider } from "@/components/theme-provider";
import { Toaster } from "./components/ui/sonner.tsx";
import { HashRouter, Route, Routes } from "react-router";
import React from "react";
import NotFoundPage from "./pages/NotFoundPage.tsx";
const App = React.lazy(() => import("./App.tsx"));
const SelectProcess = React.lazy(() => import("./pages/SelectProcess.tsx"));
const PointerScanner = React.lazy(() => import("./pages/PointerScanner.tsx"));

// Create a client
const queryClient = new QueryClient();

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
      <QueryClientProvider client={queryClient}>
        <HashRouter>
          <Routes>
            <Route path="/" element={<App />} />
            <Route path="/select-process" element={<SelectProcess />} />
            <Route path="/pointer-scanner" element={<PointerScanner />} />
            <Route path="*" element={<NotFoundPage />} />
          </Routes>
        </HashRouter>
        <Toaster richColors />
      </QueryClientProvider>
    </ThemeProvider>
  </StrictMode>
);
