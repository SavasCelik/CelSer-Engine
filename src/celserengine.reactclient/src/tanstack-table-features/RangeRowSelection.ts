// import {
//   Row,
//   RowData,
//   RowSelectionState,
//   Table,
//   TableFeature,
// } from "@tanstack/react-table";

// export type LastSelectedIndexState = number;

// export interface RangeRowSelectionTableState {
//   lastSelectedIndex: LastSelectedIndexState;
// }

// export interface RangeRowSelectionRow {
//   getRangeSelectionHandler: (index: number, event: any) => void;
// }

// declare module "@tanstack/react-table" {
//   interface TableState extends RangeRowSelectionTableState {}
//   interface Row<TData extends RowData> extends RangeRowSelectionRow {}
// }

// export const RangeRowSelectionFeature: TableFeature<any> = {
//   createRow: <TData extends RowData>(
//     row: Row<TData>,
//     table: Table<TData>
//   ): void => {
//     row.getRangeSelectionHandler = (index, event) => {
//       const isShiftKey = event.shiftKey;
//       const isCtrlKey = event.ctrlKey || event.metaKey; // Support both Ctrl and Command (Mac)
//       const { lastSelectedIndex, rowSelection } = table.getState();
//       const rowModel = table.getRowModel();
//       let newRowSelection: RowSelectionState = {};

//       if (isShiftKey && lastSelectedIndex >= 0) {
//         newRowSelection = handleShiftClick(
//           Math.min(lastSelectedIndex, index),
//           Math.max(lastSelectedIndex, index),
//           rowModel,
//           rowSelection
//         );
//       } else if (isCtrlKey) {
//         newRowSelection = handleCtrlClick(index, rowModel, rowSelection);
//       } else {
//         newRowSelection = handleSingleClick(index, rowModel);
//       }

//       table.setState((prevState) => ({
//         ...prevState,
//         lastSelectedIndex: index,
//       }));
//       table.setRowSelection(newRowSelection);
//     };
//   },
// };

// const handleShiftClick = (
//   start: number,
//   end: number,
//   rowModel: any,
//   rowSelection: RowSelectionState
// ) => {
//   const newSelection = { ...rowSelection };
//   for (let i = start; i <= end; i++) {
//     const currentRow = rowModel.rows[i];
//     newSelection[currentRow.id] = true;
//   }
//   return newSelection;
// };

// const handleCtrlClick = (
//   index: number,
//   rowModel: any,
//   rowSelection: RowSelectionState
// ) => {
//   const newSelection = { ...rowSelection };
//   const selectedRow = rowModel.rows[index];
//   newSelection[selectedRow.id] = !newSelection[selectedRow.id];
//   return newSelection;
// };

// const handleSingleClick = (index: number, rowModel: any) => {
//   const selectedRow = rowModel.rows[index];
//   return { [selectedRow.id]: true };
// };
