import { useMemo, useState } from "react";
import {
  flexRender,
  getCoreRowModel,
  useReactTable,
} from "@tanstack/react-table";
import {
  ScrollArea,
  Checkbox,
  TextInput,
  Table,
  Switch,
  Box,
  Flex,
  Paper,
} from "@mantine/core";
import classes from "./NewScanResultTable.module.css";

function NewScanResultTable() {
  const [data, setData] = useState(() => [
    {
      freeze: false,
      description: "Description",
      address: "D48257F820",
      value: "1",
    },
    {
      freeze: true,
      description: "Description",
      address: "D48258F168",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
    {
      freeze: false,
      description: "Description",
      address: "D48257F8F8",
      value: "1",
    },
  ]);

  const columns = useMemo(
    () => [
      {
        accessorKey: "freeze",
        header: "Freeze",
        cell: ({ row }: { row: any }) => (
          <Flex>
            <Switch
              size="xs"
              checked={row.original.freeze}
              onChange={(e) => {
                row.original.freeze = e.currentTarget.checked;
                setData([...data]);
              }}
            />
          </Flex>
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
    [],
  );

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
  });

  return (
    <Paper
      withBorder
      radius="md"
      pt={5}
      h="100%"
      style={{ overflow: "hidden" }}
    >
      <ScrollArea
        type="auto"
        scrollbars="y"
        offsetScrollbars={true}
        h="100%"
        styles={{
          scrollbar: { display: "block" },
        }}
      >
        <Table highlightOnHover>
          <Table.Thead className={classes.header}>
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
                          header.getContext(),
                        )}
                  </Table.Th>
                ))}
              </Table.Tr>
            ))}
          </Table.Thead>
          <Table.Tbody>
            {table.getRowModel().rows.map((row) => (
              <Table.Tr key={row.id} h={25}>
                {row.getVisibleCells().map((cell) => (
                  <Table.Td key={cell.id} py={0}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </Table.Td>
                ))}
              </Table.Tr>
            ))}
          </Table.Tbody>
        </Table>
      </ScrollArea>
    </Paper>
  );
}

export default NewScanResultTable;
