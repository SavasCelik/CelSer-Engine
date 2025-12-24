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
import { useTableRowSelection } from "@/hooks/use-table-row-selection";
import { TrackedItem } from "@/types/TrackedItem";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
const TrackedItemDialog = React.lazy(() => import("./TrackedItemDialog"));

interface TrackedItemsTableProps {
  dotNetObj: DotNetObject | null;
}

function TrackedItemsTable({ dotNetObj }: TrackedItemsTableProps) {
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [frozenRows, setFrozenRows] = React.useState<FrozenRowsState>({});
  const [shouldRefetch, setShouldRefetch] = React.useState(false);
  const [selectedTrackedItemKey, setSelectedTrackedItemKey] =
    React.useState<keyof TrackedItem>("value");
  const [isDialogOpen, setIsDialogOpen] = React.useState(false);

  const query = useQuery<TrackedItem[]>({
    queryKey: ["TrackedItemsTable"],
    queryFn: async () => {
      if (!dotNetObj) {
        return [];
      }

      return await dotNetObj!.invokeMethod("GetTrackedItems");
    },
    refetchInterval: shouldRefetch ? 1000 : false,
  });

  React.useEffect(() => {
    if (query.data?.length === 0) {
      setShouldRefetch(false);
    } else {
      setShouldRefetch(true);
    }
  }, [query.data?.length]);

  const columns = React.useMemo<ColumnDef<TrackedItem>[]>(
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
    state: {
      frozenRows,
      rowSelection,
    },
    onFrozenRowsChange: setFrozenRows,
    getRowFreezeValue: (row) => row.value,
    _features: [FrozenRowsFeature],
  });

  const handleRowSelection = useTableRowSelection(trackedItemsTable);

  return (
    <>
      <Table
        onKeyUp={(e) => {
          if (e.code === "Space") {
            trackedItemsTable.toggleFreezeOnSelection();
          }
        }}
        tabIndex={0}
        className="focus-visible:outline-none"
      >
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
            <ContextMenu
              key={row.id}
              onOpenChange={(open) => {
                if (open) {
                  // Ensure the row is selected when context menu is opened this also works when multiple rows are selected
                  if (!row.getIsSelected()) {
                    handleRowSelection(row.index);
                  }
                }
              }}
            >
              <ContextMenuTrigger asChild>
                <TableRow
              onClick={(e) => handleRowSelection(row.index, e)}
              data-state={row.getIsSelected() && "selected"}
            >
              {row.getVisibleCells().map((cell) => (
                <TableCell
                  key={cell.id}
                  onDoubleClick={() => {
                    setSelectedTrackedItemKey(
                      cell.column.id as keyof TrackedItem
                    );
                    setIsDialogOpen(true);
                  }}
                >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                </TableCell>
              ))}
            </TableRow>
              </ContextMenuTrigger>
              <ContextMenuContent>
                <ContextMenuItem
                  onClick={() => {
                    setSelectedTrackedItemKey("value");
                    // Delay opening the dialog to allow context menu to close properly
                    setTimeout(() => {
                      setIsDialogOpen(true);
                    }, 100);
                  }}
                >
                  Change Value
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    setSelectedTrackedItemKey("description");
                    // Delay opening the dialog to allow context menu to close properly
                    setTimeout(() => {
                      setIsDialogOpen(true);
                    }, 100);
                  }}
                >
                  Change Description
                </ContextMenuItem>
              </ContextMenuContent>
            </ContextMenu>
          ))}
        </TableBody>
      </Table>

      {isDialogOpen && (
        <TrackedItemDialog
          rows={trackedItemsTable.getSelectedRowModel().rows}
          trackedItemKey={selectedTrackedItemKey}
          dotNetObj={dotNetObj}
          open={isDialogOpen}
          onOpenChange={setIsDialogOpen}
        />
      )}
    </>
  );
}

export default TrackedItemsTable;
