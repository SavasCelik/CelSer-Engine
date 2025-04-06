import React, { useState } from "react";
import {
  flexRender,
  getCoreRowModel,
  RowSelectionState,
  useReactTable,
} from "@tanstack/react-table";
import { Table as TTable } from "@tanstack/react-table";
import { useMemo } from "react";
import { Button, Table } from "@mantine/core";
import classes from "./ScanResultTable.module.css";

const addresses = [
  { address: "1DBC293A66C", value: "89", prevValue: "89" },
  { address: "1DBC2A1EBC8", value: "89", prevValue: "89" },
  { address: "1DBC4374E98", value: "89", prevValue: "89" },
  { address: "1DBC438B418", value: "89", prevValue: "89" },
  { address: "1DBC43A5848", value: "89", prevValue: "89", highlighted: true },
  { address: "1DBC43B2870", value: "89", prevValue: "89" },
  { address: "1DBC486B514", value: "89", prevValue: "89" },
  { address: "1DBC486BE54", value: "89", prevValue: "89" },
  { address: "1DBC486C214", value: "89", prevValue: "11" },
  { address: "1DBC4873344", value: "89", prevValue: "89", highlighted: true },
  { address: "1DBC4873734", value: "89", prevValue: "89" },
  { address: "1DBC48739A4", value: "89", prevValue: "89" },
  { address: "1DBC4874124", value: "89", prevValue: "89" },
  { address: "1DBC293A66C", value: "89", prevValue: "89" },
  { address: "1DBC2A1EBC8", value: "89", prevValue: "89" },
  { address: "1DBC4374E98", value: "89", prevValue: "89" },
];

function ScanResultTable() {
  const columns = useMemo(
    () => [
      {
        accessorKey: "address",
        header: "Address",
        minSize: 1,
      },
      {
        accessorKey: "value",
        header: "Value",
      },
      {
        accessorKey: "prevValue",
        header: "Previous Value",
      },
    ],
    []
  );

  const data = useMemo(() => addresses, []);
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});

  const table = useReactTable({
    data,
    columns,
    columnResizeMode: "onChange",
    getCoreRowModel: getCoreRowModel(),
    onRowSelectionChange: setRowSelection,
    state: {
      rowSelection,
    },
  });

  return (
    <Table highlightOnHover>
      <Table.Thead>
        {table.getHeaderGroups().map((headerGroup) => (
          <Table.Tr key={headerGroup.id}>
            {headerGroup.headers.map((header) => (
              <Table.Th
                key={header.id}
                style={{
                  width: header.getSize(),
                }}
              >
                {header.isPlaceholder
                  ? null
                  : flexRender(
                      header.column.columnDef.header,
                      header.getContext()
                    )}
                <div
                  {...{
                    onDoubleClick: () => header.column.resetSize(),
                    onMouseDown: header.getResizeHandler(),
                    onTouchStart: header.getResizeHandler(),
                    className: `${classes.resizer} ${
                      table.options.columnResizeDirection == "ltr"
                        ? classes.ltr
                        : classes.ltr
                    } ${header.column.getIsResizing() && classes.isResizing}`,
                  }}
                />
              </Table.Th>
            ))}
          </Table.Tr>
        ))}
      </Table.Thead>
      <Table.Tbody>
        <MemoizedTableBody table={table} />
      </Table.Tbody>
    </Table>
  );
}

function TableBody({ table }: { table: TTable<any> }) {
  return (
    <>
      {table.getRowModel().rows.map((row) => (
        <Table.Tr
          key={row.id}
          className={classes.row}
          onClick={() => row.toggleSelected()}
          //   bg={
          //     row.original.highlighted
          //       ? "var(--mantine-color-CelSerEngineColor-light)"
          //       : ""
          //   }
          style={(theme) => ({
            // backgroundColor: row.original.highlighted
            //   ? "var(--mantine-color-CelSerEngineColor-light)"
            //   : "transparent",
            color:
              row.original.value != row.original.prevValue
                ? theme.colors.red[7]
                : "inherit",
          })}
          data-state={row.getIsSelected() && "selected"}
          data-selected={row.getIsSelected() || undefined}
        >
          {row.getVisibleCells().map((cell) => {
            for (let i = 0; i < 10000; i++) {
              Math.random();
            }
            return (
              <Table.Td key={cell.id} py={0}>
                <Button onClick={() => row.toggleSelected()}></Button>
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </Table.Td>
            );
          })}
        </Table.Tr>
      ))}
    </>
  );
}

export const MemoizedTableBody = React.memo(
  TableBody,
  (prev, next) =>
    // prev.table.options.data === next.table.options.data &&
    prev.table.options.state.rowSelection ===
    next.table.options.state.rowSelection
) as typeof TableBody;

export default ScanResultTable;
