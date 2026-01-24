import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { TrackedItem } from "@/types/TrackedItem";
import { DotNetObject } from "@/utils/useDotNet";
import { Row } from "@tanstack/react-table";
import TrackedItemAdvancedForm from "./TrackedItemAdvancedForm";
import TrackedItemSimpleForm from "./TrackedItemSimpleForm";

type TrackedItemDialogProps = {
  rows: Row<TrackedItem>[];
  trackedItemKey: keyof TrackedItem;
  dotNetObj: DotNetObject | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export default function TrackedItemDialog({
  rows,
  trackedItemKey,
  dotNetObj,
  open,
  onOpenChange,
}: TrackedItemDialogProps) {
  const trackedItemKeyDisplayText =
    trackedItemKey.charAt(0).toUpperCase() + trackedItemKey.slice(1);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="w-[55%] min-w-100 focus-visible:outline-none sm:max-w-full">
        <DialogHeader>
          <DialogTitle>Change {trackedItemKeyDisplayText}</DialogTitle>
          <DialogDescription></DialogDescription>
        </DialogHeader>
        {trackedItemKey === "address" ? (
          <TrackedItemAdvancedForm
            row={rows[0]}
            dotNetObj={dotNetObj}
            onOpenChange={onOpenChange}
          />
        ) : (
          <TrackedItemSimpleForm
            rows={rows}
            trackedItemKey={trackedItemKey}
            dotNetObj={dotNetObj}
            onOpenChange={onOpenChange}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
