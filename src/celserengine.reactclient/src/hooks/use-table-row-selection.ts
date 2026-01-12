import { RowModel, RowSelectionState, Table } from "@tanstack/react-table";
import { useCallback, useRef } from "react";

export function useTableRowSelection<TData>(table: Table<TData>) {
  const lastSelectedIndexRef = useRef<number>(-1);

  const handleShiftClick = (
    start: number,
    end: number,
    rowModel: RowModel<TData>,
    rowSelection: RowSelectionState
  ) => {
    const newSelection = { ...rowSelection };
    for (let i = start; i <= end; i++) {
      const currentRow = rowModel.rows[i];
      newSelection[currentRow.id] = true;
    }
    return newSelection;
  };

  const handleCtrlClick = (
    index: number,
    rowModel: RowModel<TData>,
    rowSelection: RowSelectionState
  ) => {
    const newSelection = { ...rowSelection };
    const selectedRow = rowModel.rows[index];
    newSelection[selectedRow.id] = !newSelection[selectedRow.id];
    return newSelection;
  };

  const handleSingleClick = (index: number, rowModel: RowModel<TData>) => {
    const selectedRow = rowModel.rows[index];
    return { [selectedRow.id]: true };
  };

  const handleRowSelection = useCallback(
    (index: number, event?: React.MouseEvent): void => {
      if (index === -1) {
        table.resetRowSelection();
        lastSelectedIndexRef.current = -1;
        return;
      }

      const isShiftKey = event?.shiftKey;
      const isCtrlKey = event?.ctrlKey || event?.metaKey; // Support both Ctrl and Command (Mac)
      const { rowSelection } = table.getState();
      const rowModel = table.getRowModel();
      let newRowSelection: RowSelectionState = {};

      if (isShiftKey && lastSelectedIndexRef.current >= 0) {
        newRowSelection = handleShiftClick(
          Math.min(lastSelectedIndexRef.current, index),
          Math.max(lastSelectedIndexRef.current, index),
          rowModel,
          rowSelection
        );
      } else if (isCtrlKey) {
        newRowSelection = handleCtrlClick(index, rowModel, rowSelection);
      } else {
        newRowSelection = handleSingleClick(index, rowModel);
      }

      lastSelectedIndexRef.current = index;
      table.setRowSelection(newRowSelection);
    },
    [table]
  );

  return handleRowSelection;
}
