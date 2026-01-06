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
import { cn } from "@/lib/utils";
import {
  keepPreviousData,
  useIsMutating,
  useQuery,
} from "@tanstack/react-query";
import { DotNetObject } from "../utils/useDotNet";
import { Skeleton } from "./ui/skeleton";
import { useTableRowSelection } from "@/hooks/use-table-row-selection";
import TablePagination from "./TablePagination";

type RusultItem = {
  address: string;
  value: string;
  previousValue: string;
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
  const [totalCount, setTotalCount] = React.useState(0);

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
    refetchInterval:
      totalCount > 0 && !isScanPending && !isNewScanPending ? 1000 : false,
    placeholderData: keepPreviousData,
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
    // initialState: {
    //   pagination
    // },
    state: {
      pagination,
      rowSelection,
    },
    getRowId: (row) => row.address,
  });

  React.useEffect(() => {
    //reset page index and selection when scans are done
    if (isScanPending === 0 && isNewScanPending === 0) {
      table.resetRowSelection();
      table.resetPageIndex();
    }
  }, [isScanPending, isNewScanPending, table]);

  React.useEffect(() => {
    if (query.data && totalCount !== query.data.totalCount) {
      setTotalCount(query.data.totalCount);
    }
  }, [query.data, totalCount]);

  return (
    <>
      <div className="flex min-h-[455px] flex-1 flex-col">
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
              {isScanPending ? (
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
                <MemoizedTableBody table={table} dotNetObj={dotNetObj} />
              ) : (
                <TableBodyNormal table={table} dotNetObj={dotNetObj} />
              )}
            </TableBody>
          </Table>
        </div>
        <div className="mt-1">
          <TablePagination table={table} />
        </div>
      </div>
    </>
  );
}

function TableBodyNormal({
  table,
  dotNetObj,
}: {
  table: TTable<RusultItem>;
  dotNetObj: DotNetObject | null;
}) {
  const handleRowSelection = useTableRowSelection(table);

  return (
    <>
      {table.getRowModel().rows.map((row) => (
        <TableRow
          key={row.id}
          onDoubleClick={() => {
            if (dotNetObj) {
              const { pagination } = table.getState();
              dotNetObj.invokeMethod(
                "AddToTrackedItems",
                pagination.pageIndex,
                pagination.pageSize,
                row.index
              );
            }
          }}
          onClick={(e) => handleRowSelection(row.index, e)}
          // onClick={(e) => row.toggleSelected()}
          data-state={row.getIsSelected() && "selected"}
          className={cn({
            "text-red-700": row.original.value != row.original.previousValue,
          })}
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
