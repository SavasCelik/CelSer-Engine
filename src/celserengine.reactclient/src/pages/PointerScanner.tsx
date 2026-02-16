import { useDotNet } from "@/utils/useDotNet";
import React from "react";
import PointerScanOptionsDialog from "@/components/pointer-scanner/PointerScanOptionsDialog";
import PointerScanResultTable from "@/components/pointer-scanner/PointerScanResultTable";

export default function PointerScanner() {
  const dotNetObj = useDotNet("PointerScanner", "PointerScannerController");
  const [isDialogOpen, setIsDialogOpen] = React.useState(true);
  const [maxOffsetCols, setMaxOffsetCols] = React.useState<number>(0);

  return (
    <>
      <PointerScanResultTable
        dotNetObj={dotNetObj}
        maxOffsetCols={maxOffsetCols}
        isDialogOpen={isDialogOpen}
      />

      <PointerScanOptionsDialog
        dotNetObj={dotNetObj}
        setMaxOffsetCols={setMaxOffsetCols}
        open={isDialogOpen}
        onOpenChange={setIsDialogOpen}
      />
    </>
  );
}
