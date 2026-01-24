import { useEffect, useState } from "react";
import { Progress } from "@/components/ui/progress";
import ScanResultItemsTable from "./components/main/ScanResultItemsTable";
import TrackedItemsTable from "./components/main/TrackedItemsTable";
import ScanConstraintsForm from "./components/main/ScanContraintsForm";
import { useDotNet } from "./utils/useDotNet";
import { Button } from "./components/ui/button";
import SelectProcessIcon from "./assets/SelectProcess.png";
import { cn } from "./lib/utils";

const componentId = "App";
const noProcessSelectedText = "- No Process Selected -";

function App() {
  const dotNetObj = useDotNet(componentId, "AppController");
  const dotNetObjScanResultItems = useDotNet(
    "Scan",
    "ScanResultItemsController"
  );
  const [progressBarValue, setProgressBarValue] = useState(0);
  const [selectedProcessText, setSelectedProcessText] = useState<string>(
    noProcessSelectedText
  );

  useEffect(() => {
    if (!dotNetObj) {
      return;
    }

    const updateProgressBar = (value: number) => {
      setProgressBarValue(value);
    };

    const updateSelectedProcessText = (processName: string) => {
      if (!processName) {
        processName = noProcessSelectedText;
      }

      setSelectedProcessText(processName);
    };

    dotNetObj.registerComponent({
      updateProgressBar,
      updateSelectedProcessText,
    });
  }, [dotNetObj]);

  useEffect(() => {
    if (!dotNetObj || !dotNetObjScanResultItems) {
      return;
    }

    dotNetObj.bindComponentReferences([dotNetObjScanResultItems]);
  }, [dotNetObj, dotNetObjScanResultItems]);

  return (
    <>
      <div className="bg-card flex h-screen flex-col p-2">
        <div className="flex gap-2">
          <Button
            className={cn("p-1 hover:animate-none dark:bg-neutral-700", {
              "animate-blink": selectedProcessText === noProcessSelectedText,
            })}
            variant="outline"
            onClick={() => {
              dotNetObj?.invokeMethod("OpenProcessSelector");
            }}
          >
            <img className="h-6.25" src={SelectProcessIcon}></img>
          </Button>

          <div className="flex-1">
            {/* Title bar */}
            <div className="text-center text-xs">{selectedProcessText}</div>

            {/* Progress bar */}
            <Progress className="h-3.5" value={progressBarValue} />
          </div>
        </div>

        <div className="flex flex-row gap-2">
          <ScanResultItemsTable dotNetObj={dotNetObjScanResultItems} />

          {/* Right panel - Search options */}
          <ScanConstraintsForm dotNetObj={dotNetObj} />
        </div>

        {/* Bottom table */}
        <div className="mt-2 flex-1 overflow-auto rounded-lg border">
          <TrackedItemsTable />
        </div>
      </div>
    </>
  );
}

export default App;
