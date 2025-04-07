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
  Box,
  Button,
  Checkbox,
  Flex,
  Group,
  Pagination,
  Stack,
  Text,
  Table,
  Paper,
  Center,
} from "@mantine/core";
import classes from "./ScanResultTable.module.css";

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

function ScanResultTable() {
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
    []
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
      {/* <Button
        onClick={() => {
          const newData = {
            address: "1DBC4879999",
            value: "42",
            prevValue: "0",
          };

          _setData((prevData) => [...prevData, newData]);
        }}
      >
        Add Data
      </Button> */}
      <Stack flex={1} justify="space-between" gap={0}>
        <Center>
          <Text size="xs">Found 89,325</Text>
        </Center>
        <Paper withBorder radius="md">
          <Table highlightOnHover>
            <Table.Thead>
              {table.getHeaderGroups().map((headerGroup) => (
                <Table.Tr key={headerGroup.id}>
                  {headerGroup.headers.map((header) => (
                    <Table.Th
                      key={header.id}
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
                          className: `${classes.resizer} ${
                            table.options.columnResizeDirection == "ltr"
                              ? classes.ltr
                              : classes.ltr
                          } ${
                            header.column.getIsResizing() && classes.isResizing
                          }`,
                        }}
                      />
                    </Table.Th>
                  ))}
                </Table.Tr>
              ))}
            </Table.Thead>
            <Table.Tbody>
              {table.getState().columnSizingInfo.isResizingColumn ? (
                <MemoizedTableBody table={table} />
              ) : (
                <TableBody table={table} />
              )}
            </Table.Tbody>
          </Table>
        </Paper>
        <Flex justify="center" mt="xs">
          <Pagination
            size="xs"
            value={table.getState().pagination.pageIndex + 1}
            onChange={(newPageIndex) => table.setPageIndex(newPageIndex - 1)}
            total={table.getPageCount()}
            withEdges={true}
          />
        </Flex>
      </Stack>
    </>
  );
}

function TableBody({ table }: { table: TTable<any> }) {
  return (
    <>
      {table.getRowModel().rows.map((row) => (
        <Table.Tr
          key={row.id}
          h={25}
          className={classes.row}
          onClick={() => row.toggleSelected()}
          style={(theme) => ({
            color:
              row.original.value != row.original.prevValue
                ? theme.colors.red[7]
                : "inherit",
          })}
          data-state={row.getIsSelected() && "selected"}
          data-selected={row.getIsSelected() || undefined}
        >
          {row.getVisibleCells().map((cell) => {
            return (
              <Table.Td key={cell.id} py={0}>
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </Table.Td>
            );
          })}
        </Table.Tr>
      ))}
    </>
  );
}

export const MemoizedTableBody = React.memo(
  TableBody,
  (prev, next) =>
    prev.table.options.data.length === next.table.options.data.length
) as typeof TableBody;

export default ScanResultTable;
