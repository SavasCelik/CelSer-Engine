import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
//import './index.css'
import Index from "./pages/Index.tsx";
import "@mantine/core/styles.css";
import { MantineProvider, createTheme, rem } from "@mantine/core";

const theme = createTheme({
  fontSizes: {
    sm: "12px",
  },
  primaryColor: "CelSerEngineColor",
  colors: {
    // teal: [
    //   "#e0f2f1",
    //   "#b2dfdb",
    //   "#80cbc4",
    //   "#4db6ac",
    //   "#26a69a",
    //   "#009688",
    //   "#00897b",
    //   "#00796b",
    //   "#00695c",
    //   "#004d40",
    // ],
    CelSerEngineColor: [
      "#e0f2f2",
      "#b2dfdd",
      "#80cbc7",
      "#4db6b0",
      "#25a69e",
      "#00968d",
      "#00897f",
      "#00796f",
      "#016960",
      "#024d43",
    ],
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <MantineProvider theme={theme} defaultColorScheme="dark">
      <Index />
    </MantineProvider>
  </StrictMode>
);
