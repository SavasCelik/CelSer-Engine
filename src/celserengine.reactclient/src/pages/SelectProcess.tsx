import { Input } from "@/components/ui/input";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  useReactTable,
} from "@tanstack/react-table";
import { Loader2Icon, Search, X } from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import {
  Table,
  TableHeader,
  TableRow,
  TableHead,
  TableBody,
  TableCell,
} from "@/components/ui/table";
import { useDotNet } from "@/utils/useDotNet";
import { useQuery } from "@tanstack/react-query";
import { cn } from "@/lib/utils";

type Process = {
  displayText: string;
  iconBase64Source: string;
  processId: number;
};

export default function SelectProcess() {
  const dotNetObj = useDotNet("SelectProcess", "SelectProcessController");
  const [globalFilter, setGlobalFilter] = useState("");
  const globalFilterInputRef = useRef<HTMLInputElement>(null);
  const [rowSelection, setRowSelection] = useState({});

  const query = useQuery<Process[]>({
    queryKey: ["GetProcesses", dotNetObj],
    queryFn: async () => {
      if (!dotNetObj) {
        return [];
      }

      return await dotNetObj.invokeMethod("GetProcesses");
    },
  });

  const columns = useMemo<ColumnDef<Process>[]>(
    () => [
      {
        accessorKey: "displayText",
        header: "Process",
        cell: ({ row }) => {
          const process = row.original;
          return (
            <div className="flex items-center">
              <img
                src={`data:image/png;base64,${process.iconBase64Source}`}
                alt={process.displayText}
                className="mr-2 size-5"
              />
              {process.displayText}
            </div>
          );
        },
      },
    ],
    []
  );

  const selectProcessTable = useReactTable({
    data: query.data ?? [],
    columns: columns,
    state: {
      rowSelection,
      globalFilter,
    },
    enableRowSelection: true,
    enableMultiRowSelection: false,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    globalFilterFn: "includesString",
    onGlobalFilterChange: setGlobalFilter,
  });

  useEffect(() => {
    globalFilterInputRef.current?.focus();
  }, [globalFilterInputRef]);

  return (
    <div className="bg-card flex h-screen flex-col gap-3 p-2">
      <div className="relative">
        <Search className="text-muted-foreground pointer-events-none absolute top-0 left-2 h-7.5 w-4" />
        <Input
          className="h-7.5 pr-8 pl-8"
          ref={globalFilterInputRef}
          value={globalFilter}
          onChange={(e) => setGlobalFilter(e.target.value)}
          placeholder="Search Process"
        />
        {globalFilter && (
          <X
            className="text-muted-foreground absolute top-0 right-2 h-7.5 w-4 cursor-pointer"
            onClick={() => {
              setGlobalFilter("");
              globalFilterInputRef.current?.focus();
            }}
          />
        )}
      </div>
      <div className="flex-1 overflow-auto rounded-lg border-1">
        <Table className={cn({ "h-full": query.isLoading })}>
          <TableHeader className="stickyTableHeader bg-muted">
            {selectProcessTable.getHeaderGroups().map((headerGroup) => (
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
            {query.isPending ? (
              <TableRow className="hover:bg-transparent">
                <TableCell>
                  <Loader2Icon className="m-auto animate-spin" />
                </TableCell>
              </TableRow>
            ) : (
              selectProcessTable.getRowModel().rows.map((row) => (
                <TableRow
                  key={row.id}
                  onDoubleClick={() => {
                    dotNetObj?.invokeMethod(
                      "SetSelectedProcessById",
                      row.original.processId
                    );
                  }}
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
    </div>
  );
}
