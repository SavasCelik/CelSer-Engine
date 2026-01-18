import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Loader2Icon } from "lucide-react";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSearchParams } from "react-router";
import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useDotNet } from "@/utils/useDotNet";
import React from "react";
import { useForm } from "react-hook-form";
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import TablePagination from "@/components/TablePagination";
import { TableColumnHeader } from "@/components/TableColumnHeader";
import { Checkbox } from "@/components/ui/checkbox";

type PointerScanResult = {
  moduleNameWithBaseOffset: string;
  pointingToWithValue: string;
  offsets: string[];
};

type PointerScanResultResponse = {
  items: PointerScanResult[];
  totalCount: number;
};

const formSchema = z
  .object({
    scanAddress: z.string(),
    requireAlignedPointers: z.boolean(),
    maxOffset: z.string().refine(
      (val) => {
        const num = Number(val);
        return !isNaN(num) && Number.isInteger(num) && num !== 0;
      },
      {
        message: "Max offset value must be a non-zero integer",
      }
    ),
    maxLevel: z.string().refine(
      (val) => {
        const num = Number(val);
        return !isNaN(num) && Number.isInteger(num) && num > 0;
      },
      {
        message: "Max level must be a positive integer",
      }
    ),
    maxParallelWorkers: z.string().refine(
      (val) => {
        const num = Number(val);
        return (
          !isNaN(num) &&
          Number.isInteger(num) &&
          num > 0 &&
          num <= navigator.hardwareConcurrency
        );
      },
      {
        message:
          "Max parallel workers can be from 1 to " +
          navigator.hardwareConcurrency,
      }
    ),
    limitToMaxOffsetsPerNode: z.boolean(),
    maxOffsetsPerNode: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    if (data.limitToMaxOffsetsPerNode) {
      const num = Number(data.maxOffsetsPerNode);
      if (isNaN(num) || !Number.isInteger(num) || num <= 0) {
        ctx.addIssue({
          code: "custom",
          path: ["maxOffsetsPerNode"],
          message: "Max offsets per node must be a positive integer",
        });
      }
    }
  });

type FormDataType = z.infer<typeof formSchema>;

export default function PointerScanner() {
  const [searchParams] = useSearchParams();
  const dotNetObj = useDotNet("PointerScanner", "PointerScannerController");
  const [isDialogOpen, setIsDialogOpen] = React.useState(true);
  const queryClient = useQueryClient();

  const startPointerScanMutation = useMutation({
    mutationFn: (data: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }
      setIsDialogOpen(false);

      const pointerScanOptions = {
        ...data,
        maxOffset: Number(data.maxOffset),
        maxLevel: Number(data.maxLevel),
        maxParallelWorkers: Number(data.maxParallelWorkers),
        maxOffsetsPerNode: data.limitToMaxOffsetsPerNode
          ? Number(data.maxOffsetsPerNode)
          : 0,
      };

      return dotNetObj.invokeMethod("StartPointerScan", pointerScanOptions);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["PointerScanResultsTable"],
      });
    },
    onError: () => {
      setIsDialogOpen(true);
      form.setError("scanAddress", { message: "Failed to start pointer scan" });
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

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      scanAddress: searchParams.get("searchedAddress") ?? "",
      maxOffset: (0x1000).toString(),
      maxLevel: "4",
      requireAlignedPointers: true,
      maxParallelWorkers: navigator.hardwareConcurrency.toString(),
      limitToMaxOffsetsPerNode: true,
      maxOffsetsPerNode: "3",
    },
    disabled: startPointerScanMutation.isPending,
  });

  function onSubmit(data: FormDataType) {
    startPointerScanMutation.mutate(data);
    setMaxOffsetCols(Number(data.maxLevel));
  }

  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: 20,
  });
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [maxOffsetCols, setMaxOffsetCols] = React.useState<number>(0);
  const [totalCount, setTotalCount] = React.useState(0);

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
      totalCount > 0 &&
      !startPointerScanMutation.isPending &&
      !rescanMutation.isPending
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
  React.useEffect(() => {
    const container = tableContainerRef.current;
    if (!container) return;

    const rowHeight = 28; // row height in px
    const headerHeight = 36; // header height in px

    const resizeObserver = new ResizeObserver(() => {
      const containerHeight = container.clientHeight;
      const visibleRows = Math.floor(
        (containerHeight - headerHeight) / rowHeight
      );

      setPagination((prev) => ({
        ...prev,
        pageSize: visibleRows > 20 ? visibleRows : 20,
      }));
    });

    resizeObserver.observe(container);

    return () => resizeObserver.disconnect();
  }, []);

  React.useEffect(() => {
    if (
      dotNetObj &&
      totalCount > 0 &&
      !startPointerScanMutation.isPending &&
      !rescanMutation.isPending
    ) {
      dotNetObj.invokeMethod("ApplyMultipleSorting", sorting);
    }
  }, [
    sorting,
    dotNetObj,
    totalCount,
    startPointerScanMutation.isPending,
    rescanMutation.isPending,
  ]);

  const rescanInputRef = React.useRef<HTMLInputElement>(null);

  return (
    <>
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
                startPointerScanMutation.isPending ||
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
              startPointerScanMutation.isPending ||
              rescanMutation.isPending ? (
                <TableRow className="hover:bg-transparent">
                  <TableCell colSpan={columns.length}>
                    <Loader2Icon className="m-auto animate-spin" />
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

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent
          className="focus-visible:outline-none sm:max-w-156.25"
          onInteractOutside={(e) => e.preventDefault()}
        >
          <DialogHeader>
            <DialogTitle>Pointer scanner options</DialogTitle>
            <DialogDescription></DialogDescription>
          </DialogHeader>
          <Form {...form}>
            <form className="grid grid-cols-3 gap-2">
              <FormField
                control={form.control}
                name="scanAddress"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Scan Address:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="maxOffset"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Max Offset Value:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="maxLevel"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Max Level:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="maxParallelWorkers"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Max Parallel Workers:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <div className="col-span-3">
                <FormField
                  control={form.control}
                  name="requireAlignedPointers"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-start">
                      <FormControl>
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                      </FormControl>
                      <FormLabel>Addresses must be 32-bit aligned</FormLabel>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </div>
              <div className="col-span-3 flex items-center gap-2">
                <FormField
                  control={form.control}
                  name="limitToMaxOffsetsPerNode"
                  render={({ field }) => (
                    <FormItem className="flex flex-row items-start">
                      <FormControl>
                        <Checkbox
                          checked={field.value}
                          onCheckedChange={field.onChange}
                        />
                      </FormControl>
                      <FormLabel>Limit to max offsets per node:</FormLabel>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={form.control}
                  name="maxOffsetsPerNode"
                  render={({ field }) => {
                    const limitToMaxOffsetsPerNodeValue = form.watch(
                      "limitToMaxOffsetsPerNode"
                    );

                    return (
                      <FormItem>
                        <FormControl>
                          <Input
                            disabled={!limitToMaxOffsetsPerNodeValue}
                            {...field}
                            className="w-16"
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    );
                  }}
                />
              </div>
            </form>
          </Form>
          <DialogFooter>
            <Button
              type="submit"
              onClick={form.handleSubmit(onSubmit)}
              disabled={startPointerScanMutation.isPending}
            >
              {startPointerScanMutation.isPending && (
                <Loader2Icon className="animate-spin" />
              )}
              OK
            </Button>
            <DialogClose asChild>
              <Button variant="secondary">Cancel</Button>
            </DialogClose>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
