import { useEffect, useState } from "react";
import { Progress } from "@/components/ui/progress";
import ScanResultItemsTable from "./components/ScanResultItemsTable";
import TrackedItemsTable from "./components/TrackedItemsTable";
import ScanConstraintsForm from "./components/ScanContraintsForm";
import { useDotNet } from "./utils/useDotNet";
import { jsInteropObj } from "./utils/JsInterop";

const componentId = "App";

function App() {
  const dotNetObj = useDotNet(componentId, "AppController");
  const [progressBarValue, setProgressBarValue] = useState(0);

  useEffect(() => {
    const updateProgressBar = (value: number) => {
      setProgressBarValue(value);
    };

    jsInteropObj.registerComponent(componentId, {
      updateProgressBar,
    });

    return () => {
      jsInteropObj.unregisterComponent(componentId);
    };
  }, []);

  return (
    <>
      <div className="bg-card flex h-screen flex-col p-3">
        {/* Title bar */}
        <div className="text-center text-xs">0x0000GAC4 - OneDrive.exe</div>

        {/* Progress bar */}
        <Progress value={progressBarValue} />

        {/* Main content */}
        <div className="flex flex-1 flex-col overflow-hidden">
          {/* Upper panel */}
          <div className="flex flex-row gap-2">
            {/* Left panel - Memory addresses */}
            <ScanResultItemsTable />

            {/* Right panel - Search options */}
            <ScanConstraintsForm dotNetObj={dotNetObj} />
          </div>

          {/* Bottom table */}
          <TrackedItemsTable />
        </div>
      </div>
    </>
  );
}

export default App;
