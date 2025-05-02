import React from "react";
import {
  flexRender,
  getCoreRowModel,
  PaginationState,
  RowSelectionState,
  useReactTable,
} from "@tanstack/react-table";
import { Table as TTable } from "@tanstack/react-table";
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
  PaginationNext,
  PaginationPrevious,
} from "@/components/ui/pagination";
import { cn } from "@/lib/utils";
import { useIsMutating, useQuery } from "@tanstack/react-query";
import { DotNetObject } from "../utils/useDotNet";
import { Skeleton } from "./ui/skeleton";
import { Input } from "./ui/input";

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
  const columns = React.useMemo(
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
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const isScanPending = useIsMutating({ mutationKey: ["OnScan"] });
  const isNewScanPending = useIsMutating({ mutationKey: ["NewScan"] });

  React.useEffect(() => {
    //reset page index when scans are done
    if (isScanPending === 0 && isNewScanPending === 0) {
      setPagination((prev) => ({
        ...prev,
        pageIndex: 0,
      }));
    }
  }, [isScanPending, isNewScanPending]);

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
    onRowSelectionChange: setRowSelection,
    autoResetPageIndex: false,
    manualPagination: true,
    state: {
      pagination,
      rowSelection,
    },
    getRowId: (row) => row.address,
  });
  const [totalCount, setTotalCount] = React.useState(0);

  React.useEffect(() => {
    if (query.data && totalCount !== query.data.totalCount) {
      setTotalCount(query.data.totalCount);
    }
  }, [query.data, totalCount]);

  return (
    <>
      <div className="flex flex-1 flex-col overflow-hidden min-h-[455px]">
        <div className="text-center text-sm">Found: {totalCount}</div>
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
              {(query.isFetching && query.data?.totalCount == 0) ||
              isScanPending ? (
                Array.from({ length: pagination.pageSize }).map((_, index) => (
                  <TableRow
                    key={"skeleton-row-" + index}
                    className="hover:bg-transparent"
                  >
                    <TableCell colSpan={columns.length} className="p-2">
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
        <div className="mt-1">
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
                <PaginationItem>
                  <Input
                    className="size-8 p-0 text-center"
                    value={table.getState().pagination.pageIndex}
                    onChange={(val) => {
                      let pageIndexDesired = Number(val.target.value) || 0;
                      const pageCount = table.getPageCount();
                      if (pageIndexDesired > pageCount - 1) {
                        pageIndexDesired = pageCount - 1;
                      } else if (pageIndexDesired < 0) {
                        pageIndexDesired = 0;
                      }

                      setPagination((prev) => ({
                        ...prev,
                        pageIndex: pageIndexDesired,
                      }));
                    }}
                  />
                </PaginationItem>
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
  const lastSelectedIndexRef = React.useRef<number | null>(null);
  const rowSelection = table.getState().rowSelection;
  const rowModel = table.getRowModel();

  const handleRowClick = (index: number, event: React.MouseEvent) => {
    const isShiftKey = event.shiftKey;
    const isCtrlKey = event.ctrlKey || event.metaKey; // Support both Ctrl and Command (Mac)
    let newRowSelection: RowSelectionState = {};

    if (isShiftKey && lastSelectedIndexRef.current !== null) {
      // Get the range of rows to select
      const start = Math.min(lastSelectedIndexRef.current, index);
      const end = Math.max(lastSelectedIndexRef.current, index);

      // Preserve existing selections
      newRowSelection = { ...rowSelection };

      // Select all rows in the range
      for (let i = start; i <= end; i++) {
        const currentRow = rowModel.rows[i];
        newRowSelection[currentRow.id] = true;
      }
    } else if (isCtrlKey) {
      // For Ctrl+click, toggle the clicked row's selection state
      // while preserving other selections
      const selectedRow = rowModel.rows[index];
      newRowSelection = { ...rowSelection };
      newRowSelection[selectedRow.id] = !newRowSelection[selectedRow.id];
    } else {
      // For single click, select only the clicked row
      const selectedRow = rowModel.rows[index];
      newRowSelection[selectedRow.id] = true;
    }

    // Update last selected index
    lastSelectedIndexRef.current = index;
    table.setRowSelection(newRowSelection);
  };

  return (
    <>
      {rowModel.rows.map((row) => (
        <TableRow
          key={row.id}
          onClick={(e) => handleRowClick(row.index, e)}
          data-state={row.getIsSelected() && "selected"}
          data-selected={row.getIsSelected() || undefined}
          className="hover:data-[state=selected]:bg-primary/50 data-[state=selected]:bg-primary/60"
        >
          {row.getVisibleCells().map((cell) => {
            return (
              <TableCell key={cell.id}>
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
