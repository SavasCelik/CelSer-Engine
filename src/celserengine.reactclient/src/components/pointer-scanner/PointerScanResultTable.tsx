import { DotNetObject } from "@/utils/useDotNet";
import {
  keepPreviousData,
  useIsMutating,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  Column,
  ColumnDef,
  flexRender,
  getCoreRowModel,
  PaginationState,
  Row,
  RowSelectionState,
  SortingState,
  useReactTable,
} from "@tanstack/react-table";
import React from "react";
import { TableColumnHeader } from "../TableColumnHeader";
import { Input } from "../ui/input";
import { Button } from "../ui/button";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "../ui/table";
import { Loader2Icon, X } from "lucide-react";
import TablePagination from "../TablePagination";
import { cn } from "@/lib/utils";

type PointerScanResult = {
  moduleNameWithBaseOffset: string;
  pointingToWithValue: string;
  offsets: string[];
};

type PointerScanResultResponse = {
  items: PointerScanResult[];
  totalCount: number;
};

type PointerScanResultTableProps = {
  dotNetObj: DotNetObject | null;
  maxOffsetCols: number;
  isDialogOpen: boolean;
};

export default function PointerScanResultTable({
  dotNetObj,
  maxOffsetCols,
  isDialogOpen,
}: PointerScanResultTableProps) {
  const rescanInputRef = React.useRef<HTMLInputElement>(null);
  const queryClient = useQueryClient();
  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: 20,
  });
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [totalCount, setTotalCount] = React.useState(0);

  const isStartPointerScanPending =
    useIsMutating({
      mutationKey: ["StartPointerScan"],
    }) > 0;

  const cancelScanMutation = useMutation({
    mutationFn: () => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      return dotNetObj.invokeMethod("CancelScanAsync");
    },
  });

  const rescanMutation = useMutation({
    mutationFn: (address?: string) => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      return dotNetObj.invokeMethod("Rescan", address);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["PointerScanResultsTable"],
      });
    },
  });

  const query = useQuery<PointerScanResultResponse>({
    queryKey: ["PointerScanResultsTable", { pagination }],
    enabled: dotNetObj != null,
    queryFn: () => {
      return dotNetObj!.invokeMethod(
        "GetPointerScanResults",
        pagination.pageIndex,
        pagination.pageSize
      );
    },
    refetchInterval:
      totalCount > 0 && !isStartPointerScanPending && !rescanMutation.isPending
        ? 1000
        : false,
    placeholderData: keepPreviousData,
  });

  const columns = React.useMemo<ColumnDef<PointerScanResult>[]>(
    () => [
      {
        accessorKey: "moduleNameWithBaseOffset",
        header: ({ column }) => (
          <TableColumnHeader column={column} title="Address" />
        ),
      },
      ...Array.from(
        {
          length: maxOffsetCols,
        },
        (_, index) => ({
          accessorKey: `offsets[${index}]`,
          // header: `Offset ${index + 1}`,
          header: ({ column }: { column: Column<PointerScanResult> }) => (
            <TableColumnHeader column={column} title={`Offset ${index}`} />
          ),
          cell: ({ row }: { row: Row<PointerScanResult> }) =>
            row.original.offsets[index] ?? "",
        })
      ),
      {
        accessorKey: "pointingToWithValue",
        header: "Points To",
      },
    ],
    [maxOffsetCols]
  );

  const table = useReactTable({
    data: query.data?.items ?? [],
    columns,
    rowCount: query.data?.totalCount ?? 0,
    getCoreRowModel: getCoreRowModel(),
    onPaginationChange: setPagination,
    onRowSelectionChange: setRowSelection,
    autoResetPageIndex: false,
    manualPagination: true,
    manualSorting: true,
    enableMultiRowSelection: false,
    // initialState: {
    //   pagination
    // },
    state: {
      pagination,
      rowSelection,
      sorting,
    },
    onSortingChange: setSorting,
    getRowId: (row) => row.moduleNameWithBaseOffset + row.offsets.join(", "),
  });

  React.useEffect(() => {
    if (query.data && totalCount !== query.data.totalCount) {
      setTotalCount(query.data.totalCount);
    }
  }, [query.data, totalCount]);

  // adjust pageSize based on container height
  const tableContainerRef = React.useRef<HTMLDivElement>(null);
  //   React.useEffect(() => {
  //     const container = tableContainerRef.current;
  //     if (!container) return;

  //     const rowHeight = 28; // row height in px
  //     const headerHeight = 36; // header height in px

  //     const resizeObserver = new ResizeObserver(() => {
  //       const containerHeight = container.clientHeight;
  //       const visibleRows = Math.floor(
  //         (containerHeight - headerHeight) / rowHeight
  //       );

  //       setPagination((prev) => ({
  //         ...prev,
  //         pageSize: visibleRows > 20 ? visibleRows : 20,
  //       }));
  //     });

  //     resizeObserver.observe(container);

  //     return () => resizeObserver.disconnect();
  //   }, []);

  React.useEffect(() => {
    if (
      dotNetObj &&
      totalCount > 0 &&
      !isStartPointerScanPending &&
      !rescanMutation.isPending
    ) {
      dotNetObj.invokeMethod("ApplyMultipleSorting", sorting);
    }
  }, [
    sorting,
    dotNetObj,
    totalCount,
    isStartPointerScanPending,
    rescanMutation.isPending,
  ]);

  const isCancellingScan =
    cancelScanMutation.isPending ||
    (cancelScanMutation.isSuccess && isStartPointerScanPending);
  return (
    <div className="bg-card flex h-screen flex-col gap-2 p-2">
      <div className="flex justify-end gap-1">
        <Input className="h-7.5 w-fit text-sm" ref={rescanInputRef} />
        <Button
          disabled={totalCount <= 0}
          onClick={() => rescanMutation.mutate(rescanInputRef.current?.value)}
        >
          Rescan
        </Button>
      </div>
      <div className="text-center text-sm">Found: {totalCount}</div>
      <div
        ref={tableContainerRef}
        className="flex-1 overflow-auto rounded-lg border"
      >
        <Table
          className={cn({
            "h-full":
              query.isPending ||
              isStartPointerScanPending ||
              rescanMutation.isPending,
          })}
        >
          <TableHeader className="stickyTableHeader bg-muted">
            {table.getHeaderGroups().map((headerGroup) => (
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
            {query.isPending ||
            isStartPointerScanPending ||
            rescanMutation.isPending ? (
              <TableRow className="hover:bg-transparent">
                <TableCell colSpan={columns.length}>
                  {!isDialogOpen && (
                    <div className="flex flex-col items-center justify-center gap-4">
                      <Loader2Icon
                        className="text-primary animate-spin"
                        size={60}
                        absoluteStrokeWidth={true}
                      />
                      <div className="text-center">
                        {isCancellingScan ? (
                          <p className="text-sm font-medium">
                            Scan is being cancelled...
                          </p>
                        ) : (
                          <>
                            <p className="text-sm font-medium">
                              Scanning memory...
                            </p>
                            <p className="text-muted-foreground mt-1 text-xs">
                              This may take a moment
                            </p>
                          </>
                        )}
                      </div>
                      <Button
                        variant="outline"
                        disabled={isCancellingScan}
                        onClick={() => cancelScanMutation.mutate()}
                        className="mt-2"
                      >
                        <X />
                        Cancel
                      </Button>
                    </div>
                  )}
                </TableCell>
              </TableRow>
            ) : (
              table.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  onClick={() => {
                    row.toggleSelected(true);
                  }}
                  onDoubleClick={() => {
                    if (dotNetObj) {
                      dotNetObj.invokeMethod(
                        "AddToTrackedItems",
                        pagination.pageIndex,
                        pagination.pageSize,
                        row.index
                      );
                    }
                  }}
                  data-state={row.getIsSelected() && "selected"}
                >
                  {row.getVisibleCells().map((cell) => (
                    <TableCell key={cell.id}>
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </TableCell>
                  ))}
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </div>
      <TablePagination table={table} />
    </div>
  );
}
