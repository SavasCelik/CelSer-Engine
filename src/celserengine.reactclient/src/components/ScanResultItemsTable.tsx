import React from "react";
import {
  flexRender,
  getCoreRowModel,
  PaginationState,
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
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { cn } from "@/lib/utils";
import { useIsMutating, useQuery } from "@tanstack/react-query";
import { DotNetObject } from "../utils/useDotNet";
import { Skeleton } from "./ui/skeleton";

type RusultItem = {
  address: string;
  value: string;
  prevValue: string;
  highlighted?: boolean;
};

type ScanResultResponse = {
  items: RusultItem[];
  totalCount: number;
};

interface ScanResultItemsTableProps {
  dotNetObj: DotNetObject | null;
}

function ScanResultItemsTable({ dotNetObj }: ScanResultItemsTableProps) {
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
        accessorKey: "previousValue",
        header: "Previous Value",
      },
    ],
    []
  );
  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: 13,
  });

  const isScanPending = useIsMutating({ mutationKey: ["OnScan"] });
  const query = useQuery<ScanResultResponse>({
    queryKey: ["ScanResultItemsTable", { pagination }],
    queryFn: async () => {
      if (!dotNetObj) {
        return { items: [], totalCount: 0 };
      }

      return await dotNetObj!.invokeMethod(
        "GetScanResultItems",
        pagination.pageIndex,
        pagination.pageSize
      );
    },
  });

  const table = useReactTable({
    data: query.data?.items ?? [],
    columns,
    columnResizeMode: "onChange",
    rowCount: query.data?.totalCount ?? 0,
    getCoreRowModel: getCoreRowModel(),
    onPaginationChange: setPagination,
    autoResetPageIndex: false,
    manualPagination: true,
    state: {
      pagination,
    },
  });

  return (
    <>
      <div className="flex flex-1 flex-col overflow-hidden">
        <div className="text-center text-sm">
          Found {query.data?.totalCount}
        </div>
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
                            header.getContext()
                          )}
                      <div
                        {...{
                          onDoubleClick: () => header.column.resetSize(),
                          onMouseDown: header.getResizeHandler(),
                          onTouchStart: header.getResizeHandler(),
                        }}
                      />
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {query.isFetching || isScanPending ? (
                Array.from({ length: pagination.pageSize }).map((_, index) => (
                  <TableRow
                    key={"skeleton-row-" + index}
                    className="hover:bg-transparent"
                  >
                    <TableCell colSpan={columns.length}>
                      <Skeleton className="h-[10.55px]" />
                    </TableCell>
                  </TableRow>
                ))
              ) : query.isError ? (
                <TableRow>
                  <TableCell colSpan={columns.length}>
                    <div className="flex h-10 items-center justify-center text-sm text-red-500">
                      Error loading data
                    </div>
                  </TableCell>
                </TableRow>
              ) : table.getState().columnSizingInfo.isResizingColumn ? (
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
                  className={cn({
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
                  className={cn({
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
    prev.table.options.data.length === next.table.options.data.length
) as typeof TableBodyNormal;

export default ScanResultItemsTable;
