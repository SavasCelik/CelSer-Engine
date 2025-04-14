import { createTheme, MantineProvider } from "@mantine/core";
import { ThemeProvider } from "./components/theme-provider";
import IndexShadcn from "./shadcn/Index";
import { useState } from "react";
import Index from "./pages/Index";
// import "@mantine/core/styles.css";

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

function MainUiTester() {
  const [useMantine, setUseMantine] = useState(false);
  if (useMantine) {
  }

  return (
    <>
      {useMantine ? (
        <MantineProvider theme={theme} defaultColorScheme="dark">
          <Index />
        </MantineProvider>
      ) : (
        <ThemeProvider defaultTheme="dark" storageKey="vite-ui-theme">
          {/* <TestFlex /> */}
          <IndexShadcn />
        </ThemeProvider>
      )}
    </>
  );
}

export default MainUiTester;
