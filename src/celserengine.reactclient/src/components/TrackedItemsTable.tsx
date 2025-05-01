import { Switch } from "@/components/ui/switch";
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { useMemo, useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

type FreezeRow = {
  freeze: boolean;
  description: string;
  address: string;
  value: number;
};

function TrackedItemsTable() {
  const [rowSelection, setRowSelection] = useState({});
  const freezeData = useMemo<FreezeRow[]>(
    () => [
      {
        freeze: false,
        description: "Description",
        address: "D48257F820",
        value: 1,
      },
      {
        freeze: true,
        description: "Description",
        address: "D48258F168",
        value: 1,
      },
      ...Array(10)
        .fill(0)
        .map(() => ({
          freeze: false,
          description: "Description",
          address: "D48257F8F8",
          value: 1,
        })),
    ],
    []
  );

  // Column definitions for freeze table
  const freezeColumnHelper = createColumnHelper<FreezeRow>();
  const freezeColumns = useMemo(
    () => [
      freezeColumnHelper.accessor("freeze", {
        header: "Freeze",
        cell: ({ row }) => (
          <Switch
            className="cursor-pointer"
            {...{
              checked: row.getIsSelected(),
              disabled: !row.getCanSelect(),
              onClick: row.getToggleSelectedHandler(),
            }}
          />
        ),
      }),
      freezeColumnHelper.accessor("description", {
        header: "Description",
        cell: (info) => info.getValue(),
      }),
      freezeColumnHelper.accessor("address", {
        header: "Address",
        cell: (info) => info.getValue(),
      }),
      freezeColumnHelper.accessor("value", {
        header: "Value",
        cell: (info) => info.getValue(),
      }),
    ],
    []
  );

  // Set up freeze table
  const freezeTable = useReactTable({
    data: freezeData,
    columns: freezeColumns,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    state: {
      rowSelection,
    },
    enableRowSelection: true,
  });

  return (
    <Table>
      <TableHeader className="stickyTableHeader bg-muted">
        {freezeTable.getHeaderGroups().map((headerGroup) => (
          <TableRow key={headerGroup.id}>
            {headerGroup.headers.map((header) => (
              <TableHead key={header.id}>
                <div className="flex items-center">
                  {flexRender(
                    header.column.columnDef.header,
                    header.getContext()
                  )}
                </div>
              </TableHead>
            ))}
          </TableRow>
        ))}
      </TableHeader>
      <TableBody>
        {freezeTable.getRowModel().rows.map((row) => (
          <TableRow key={row.id}>
            {row.getVisibleCells().map((cell) => (
              <TableCell key={cell.id}>
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}

export default TrackedItemsTable;
