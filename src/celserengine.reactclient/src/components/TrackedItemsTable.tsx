import React from "react";
import { Switch } from "@/components/ui/switch";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  RowSelectionState,
  useReactTable,
} from "@tanstack/react-table";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { DotNetObject } from "@/utils/useDotNet";
import { useQuery } from "@tanstack/react-query";
import {
  FrozenRowsFeature,
  FrozenRowsState,
} from "../tanstack-table-features/FrozenRows";

type FreezeRow = {
  freeze: boolean;
  description: string;
  address: string;
  value: string;
};

interface TrackedItemsTableProps {
  dotNetObj: DotNetObject | null;
}

function TrackedItemsTable({ dotNetObj }: TrackedItemsTableProps) {
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [frozenRows, setFrozenRows] = React.useState<FrozenRowsState>({});
  const query = useQuery<FreezeRow[]>({
    queryKey: ["TrackedItemsTable"],
    queryFn: async () => {
      if (!dotNetObj) {
        return [];
      }

      return await dotNetObj!.invokeMethod("GetTrackedItems");
    },
  });

  const columns = React.useMemo<ColumnDef<FreezeRow>[]>(
    () => [
      {
        accessorKey: "freeze",
        header: "Freeze",
        cell: ({ row }) => (
          <Switch
            className="cursor-pointer"
            checked={row.getIsFrozen()}
            onClick={() => row.toggleFrozen()}
          />
        ),
      },
      {
        accessorKey: "description",
        header: "Description",
      },
      {
        accessorKey: "address",
        header: "Address",
      },
      {
        accessorKey: "value",
        header: "Value",
      },
    ],
    []
  );

  const trackedItemsTable = useReactTable({
    data: query.data || [],
    columns: columns,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    enableRowFreezing: false,
    getRowId: (row) => row.address,
    state: {
      frozenRows,
      rowSelection,
    },
    onFrozenRowsChange: setFrozenRows,
    getRowFreezeValue: (row) => row.value,
    _features: [FrozenRowsFeature],
  });

  return (
    <Table>
      <TableHeader className="stickyTableHeader bg-muted">
        {trackedItemsTable.getHeaderGroups().map((headerGroup) => (
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
        {trackedItemsTable.getRowModel().rows.map((row) => (
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
