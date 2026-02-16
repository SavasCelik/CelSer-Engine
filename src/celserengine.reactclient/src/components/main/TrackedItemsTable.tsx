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
import { useDotNet } from "@/utils/useDotNet";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  FrozenRowsFeature,
  FrozenRowsState,
} from "@/tanstack-table-features/FrozenRows";
import { useTableRowSelection } from "@/hooks/use-table-row-selection";
import { TrackedItem } from "@/types/TrackedItem";
import {
  ContextMenu,
  ContextMenuContent,
  ContextMenuItem,
  ContextMenuTrigger,
} from "@/components/ui/context-menu";
import { scanValueTypeById } from "@/constants/ScanValueTypes";
const TrackedItemDialog = React.lazy(() => import("./TrackedItemDialog"));

function TrackedItemsTable() {
  const dotNetObj = useDotNet("TrackedItems", "TrackedItemsController");
  const [rowSelection, setRowSelection] = React.useState<RowSelectionState>({});
  const [frozenRows, setFrozenRows] = React.useState<FrozenRowsState>({});
  const [shouldRefetch, setShouldRefetch] = React.useState(false);
  const [selectedTrackedItemKey, setSelectedTrackedItemKey] =
    React.useState<keyof TrackedItem>("value");
  const [isDialogOpen, setIsDialogOpen] = React.useState(false);
  const queryClient = useQueryClient();

  const query = useQuery<TrackedItem[]>({
    queryKey: ["TrackedItemsTable"],
    enabled: dotNetObj != null,
    queryFn: () => {
      return dotNetObj!.invokeMethod("GetTrackedItems");
    },
    refetchInterval: shouldRefetch ? 1000 : false,
  });

  const removeTrackedItemsMutation = useMutation({
    mutationFn: (indices: number[]) => {
      if (!dotNetObj) {
        return Promise.reject();
      }
      return dotNetObj.invokeMethod("RemoveItems", indices);
    },
    onSuccess: () => {
      handleRowSelection(-1); // Clear selection
      queryClient.invalidateQueries({
        queryKey: ["TrackedItemsTable"],
      });
    },
  });

  React.useEffect(() => {
    if (query.data?.length === 0) {
      setShouldRefetch(false);
    } else {
      setShouldRefetch(true);
    }
  }, [query.data?.length]);

  React.useEffect(() => {
    if (!dotNetObj) {
      return;
    }

    const onItemsChanged = () => {
      queryClient.invalidateQueries({
        queryKey: ["TrackedItemsTable"],
      });
    };

    dotNetObj.registerComponent({
      onItemsChanged,
    });
  }, [dotNetObj, queryClient]);

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
        size: 70,
      },
      {
        accessorKey: "description",
        header: "Description",
        size: 190,
      },
      {
        accessorKey: "address",
        header: "Address",
        cell: ({ row }) =>
          row.original.isPointer
            ? `P->${row.original.pointingTo}`
            : row.original.address,
        size: 190,
      },
      {
        accessorKey: "dataType",
        header: "Type",
        cell: ({ row }) => {
          return scanValueTypeById[row.original.dataType].label;
        },
      },
      {
        accessorKey: "value",
        header: "Value",
        enableResizing: false,
      },
    ],
    []
  );

  const trackedItemsTable = useReactTable({
    data: query.data || [],
    columns: columns,
    columnResizeMode: "onChange",
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
    enableRowFreezing: false,
    defaultColumn: {
      enableResizing: true,
      minSize: 60,
    },
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
        className="w-fit table-fixed focus-visible:outline-none"
      >
        <TableHeader className="stickyTableHeader bg-muted">
          {trackedItemsTable.getHeaderGroups().map((headerGroup) => (
            <TableRow key={headerGroup.id}>
              {headerGroup.headers.map((header, index) => {
                const colSize = header.getSize();
                const isLast = index === headerGroup.headers.length - 1;

                return (
                  <TableHead
                    key={header.id}
                    style={{
                      width: isLast ? "100%" : colSize,
                    }}
                    className="relative"
                  >
                    <div className="flex h-full items-center justify-between">
                      <div className="truncate">
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                      </div>
                      {header.column.columnDef.enableResizing && (
                        <div
                          className="flex h-full w-2 shrink-0 cursor-ew-resize items-center justify-center"
                          onDoubleClick={() => header.column.resetSize()}
                          onMouseDown={header.getResizeHandler()}
                          onTouchStart={header.getResizeHandler()}
                        >
                          <div className="h-2/6 w-0.5 rounded-full bg-neutral-700"></div>
                        </div>
                      )}
                    </div>
                  </TableHead>
                );
              })}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {trackedItemsTable.getRowModel().rows.map((row) => (
            <ContextMenu
              key={row.id}
              modal={false}
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
                        const trackedItem: TrackedItem = row.original;

                        if (cell.column.id in trackedItem) {
                          setSelectedTrackedItemKey(
                            cell.column.id as keyof TrackedItem
                          );
                          setIsDialogOpen(true);
                        }
                      }}
                      className="truncate"
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
                    setIsDialogOpen(true);
                  }}
                >
                  Change Value
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    setSelectedTrackedItemKey("description");
                    setIsDialogOpen(true);
                  }}
                >
                  Change Description
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    setSelectedTrackedItemKey("dataType");
                    setIsDialogOpen(true);
                  }}
                >
                  Change Type
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    const indices = trackedItemsTable
                      .getSelectedRowModel()
                      .rows.map((r) => r.index);
                    removeTrackedItemsMutation.mutate(indices);
                  }}
                >
                  Remove selected items
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    trackedItemsTable.toggleFreezeOnSelection();
                  }}
                >
                  Toggle selected items
                </ContextMenuItem>
                <ContextMenuItem
                  onClick={() => {
                    dotNetObj?.invokeMethod("OpenPointerScanner", row.index);
                  }}
                >
                  Pointer scan for this address
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
