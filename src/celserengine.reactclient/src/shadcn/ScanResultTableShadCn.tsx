import React, { useState } from "react";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getPaginationRowModel,
  PaginationState,
  RowSelectionState,
  useReactTable,
} from "@tanstack/react-table";
import { Table as TTable } from "@tanstack/react-table";
import { useMemo } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import clsx from "clsx";
import { cn } from "@/lib/utils";

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

function ScanResultTableShadCn() {
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
    [],
  );

  //   const data = useMemo(() => addresses, []);
  const [data, _setData] = useState(addresses);
  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: 13,
  });

  const table = useReactTable({
    data,
    columns,
    columnResizeMode: "onChange",
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onPaginationChange: setPagination,
    autoResetPageIndex: false,
    state: {
      pagination,
    },
  });

  return (
    <>
      <div className="flex flex-1 flex-col overflow-hidden">
        <div className="text-center">Found 89,325</div>
        <div className="flex-1 overflow-auto rounded-lg border-1">
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow key={headerGroup.id}>
                  {headerGroup.headers.map((header) => (
                    <TableHead
                      key={header.id}
                      className="bg-muted"
                      style={{
                        width: header.getSize(),
                      }}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext(),
                          )}
                      <div
                        {...{
                          onDoubleClick: () => header.column.resetSize(),
                          onMouseDown: header.getResizeHandler(),
                          onTouchStart: header.getResizeHandler(),
                          //   className: `${classes.resizer} ${
                          //     table.options.columnResizeDirection == "ltr"
                          //       ? classes.ltr
                          //       : classes.ltr
                          //   } ${
                          //     header.column.getIsResizing() && classes.isResizing
                          //   }`,
                        }}
                      />
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getState().columnSizingInfo.isResizingColumn ? (
                <MemoizedTableBody table={table} />
              ) : (
                <TableBodyNormal table={table} />
              )}
            </TableBody>
          </Table>
        </div>
        <div>
          <Pagination>
            <PaginationContent>
              <PaginationItem>
                <PaginationPrevious
                  href="#"
                  onClick={() => table.previousPage()}
                  className={clsx({
                    "pointer-events-none cursor-not-allowed text-gray-500":
                      !table.getCanPreviousPage(),
                  })}
                />
              </PaginationItem>
              <PaginationItem>
                <PaginationLink href="#" isActive>
                  {table.getState().pagination.pageIndex}
                </PaginationLink>
              </PaginationItem>
              <PaginationItem>
                <PaginationNext
                  href="#"
                  onClick={() => table.nextPage()}
                  className={clsx({
                    "pointer-events-none cursor-not-allowed text-gray-500":
                      !table.getCanNextPage(),
                  })}
                />
              </PaginationItem>
            </PaginationContent>
          </Pagination>
        </div>
      </div>
    </>
  );
}

function TableBodyNormal({ table }: { table: TTable<any> }) {
  return (
    <>
      {table.getRowModel().rows.map((row) => (
        <TableRow
          key={row.id}
          //   className={classes.row}
          onClick={() => row.toggleSelected()}
          data-state={row.getIsSelected() && "selected"}
          data-selected={row.getIsSelected() || undefined}
          className="hover:data-[state=selected]:bg-primary/50 data-[state=selected]:bg-primary/60"
        >
          {row.getVisibleCells().map((cell) => {
            return (
              <TableCell key={cell.id} className="p-1">
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </TableCell>
            );
          })}
        </TableRow>
      ))}
    </>
  );
}

export const MemoizedTableBody = React.memo(
  TableBodyNormal,
  (prev, next) =>
    prev.table.options.data.length === next.table.options.data.length,
) as typeof TableBodyNormal;

export default ScanResultTableShadCn;
