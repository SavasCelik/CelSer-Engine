import { useEffect, useState } from "react";
import { Progress } from "@/components/ui/progress";
import ScanResultItemsTable from "./components/ScanResultItemsTable";
import TrackedItemsTable from "./components/TrackedItemsTable";
import ScanConstraintsForm from "./components/ScanContraintsForm";
import { useDotNet } from "./utils/useDotNet";
import { jsInteropObj } from "./utils/JsInterop";

const componentId = "App";
const noProcessSelectedText = "- No Process Selected -";

function App() {
  const dotNetObj = useDotNet(componentId, "AppController");
  const [progressBarValue, setProgressBarValue] = useState(0);
  const [selectedProcessText, setSelectedProcessText] = useState<string>(
    noProcessSelectedText
  );

  useEffect(() => {
    const updateProgressBar = (value: number) => {
      setProgressBarValue(value);
    };

    const updateSelectedProcessText = (processName: string) => {
      if (!processName) {
        processName = noProcessSelectedText;
      }

      setSelectedProcessText(processName);
    };

    jsInteropObj.registerComponent(componentId, {
      updateProgressBar,
      updateSelectedProcessText,
    });

    return () => {
      jsInteropObj.unregisterComponent(componentId);
    };
  }, []);

  return (
    <>
      <div className="bg-card flex h-screen flex-col p-3">
        {/* Title bar */}
        <div className="text-center text-xs">{selectedProcessText}</div>

        {/* Progress bar */}
        <Progress value={progressBarValue} />

        <div className="flex flex-row gap-2">
          {/* Left panel - Memory addresses */}
          <ScanResultItemsTable dotNetObj={dotNetObj} />

          {/* Right panel - Search options */}
          <ScanConstraintsForm dotNetObj={dotNetObj} />
        </div>

        {/* Bottom table */}
        <div className="mt-2 flex-1 overflow-auto rounded-lg border-1 ">
          <TrackedItemsTable dotNetObj={dotNetObj} />
        </div>
      </div>
    </>
  );
}

export default App;
