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
  ColumnDef,
  flexRender,
  getCoreRowModel,
  PaginationState,
  Row,
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
import { cn } from "@/lib/utils";
import TablePagination from "@/components/TablePagination";

type PointerScanResult = {
  moduleNameWithBaseOffset: string;
  pointingToWithValue: string;
  offsets: string[];
};

type PointerScanResultResponse = {
  items: PointerScanResult[];
  totalCount: number;
};

const formSchema = z.object({
  scanAddress: z.string(),
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

      return dotNetObj.invokeMethod(
        "StartPointerScan",
        data.scanAddress,
        Number(data.maxOffset),
        Number(data.maxLevel)
      );
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

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      scanAddress: searchParams.get("searchedAddress") ?? "",
      maxOffset: (0x1000).toString(),
      maxLevel: "4",
    },
    disabled: startPointerScanMutation.isPending,
  });

  function onSubmit(data: FormDataType) {
    startPointerScanMutation.mutate(data);
    setMaxOffsetCols(Number(data.maxLevel));
  }

  const [pagination, setPagination] = React.useState<PaginationState>({
    pageIndex: 0,
    pageSize: 13,
  });
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [maxOffsetCols, setMaxOffsetCols] = React.useState<number>(0);
  const [totalCount, setTotalCount] = React.useState(0);

  const query = useQuery<PointerScanResultResponse>({
    queryKey: ["PointerScanResultsTable", { pagination }],
    queryFn: async () => {
      if (!dotNetObj) {
        return { items: [], totalCount: 0 };
      }

      return await dotNetObj!.invokeMethod(
        "GetPointerScanResults",
        pagination.pageIndex,
        pagination.pageSize
      );
    },
    refetchInterval:
      totalCount > 0 && !startPointerScanMutation.isPending ? 1000 : false,
    placeholderData: keepPreviousData,
  });

  const columns = React.useMemo<ColumnDef<PointerScanResult>[]>(
    () => [
      {
        accessorKey: "moduleNameWithBaseOffset",
        header: "Address",
      },
      ...Array.from(
        {
          length: maxOffsetCols,
        },
        (_, index) => ({
          accessorKey: `offsets[${index}]`,
          header: `Offset ${index + 1}`,
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
    getRowId: (row) => row.moduleNameWithBaseOffset + row.offsets.join(", "),
  });

  React.useEffect(() => {
    if (query.data && totalCount !== query.data.totalCount) {
      setTotalCount(query.data.totalCount);
    }
  }, [query.data, totalCount]);

  return (
    <>
      <div className="bg-card flex h-screen flex-col gap-2 p-2">
        <div className="text-center text-sm">Found: {totalCount}</div>
        <div className="flex-1 overflow-auto rounded-lg border-1">
          <Table
            className={cn({
              "h-full": query.isPending || startPointerScanMutation.isPending,
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
              {query.isPending || startPointerScanMutation.isPending ? (
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
                      row.toggleSelected();
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
          className="focus-visible:outline-none sm:max-w-[625px]"
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
