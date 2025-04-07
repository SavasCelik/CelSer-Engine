"use client";

import { useState, useMemo } from "react";
import { Search, X, ChevronDown, ChevronUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Badge } from "@/components/ui/badge";
import {
  createColumnHelper,
  flexRender,
  getCoreRowModel,
  getSortedRowModel,
  useReactTable,
  type SortingState,
} from "@tanstack/react-table";
import { Progress } from "@/components/ui/progress";
import { Label } from "@/components/ui/label";

// Define types for our tables
type AddressRow = {
  address: string;
  value: number;
  previousValue: number;
  changed: boolean;
};

type FreezeRow = {
  freeze: boolean;
  description: string;
  address: string;
  value: number;
};

export default function Index() {
  const [searchQuery, setSearchQuery] = useState("lol");
  const [addressSorting, setAddressSorting] = useState<SortingState>([]);
  const [freezeSorting, setFreezeSorting] = useState<SortingState>([]);

  // Sample data for the address table
  const addressData = useMemo<AddressRow[]>(
    () => [
      { address: "1DBC293A66C", value: 89, previousValue: 89, changed: false },
      { address: "1DBC2A1EBC8", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4374E98", value: 89, previousValue: 89, changed: false },
      { address: "1DBC438B418", value: 89, previousValue: 89, changed: false },
      { address: "1DBC43A5848", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4382870", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4868514", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4868E54", value: 89, previousValue: 89, changed: false },
      { address: "1DBC486C214", value: 89, previousValue: 11, changed: true },
      { address: "1DBC4873344", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4873734", value: 89, previousValue: 89, changed: false },
      { address: "1DBC48739A4", value: 89, previousValue: 89, changed: false },
      { address: "1DBC4874124", value: 89, previousValue: 89, changed: false },
    ],
    []
  );

  // Sample data for the freeze table
  const freezeData = useMemo<FreezeRow[]>(
    () => [
      {
        freeze: false,
        description: "Description",
        address: "D48257F820",
        value: 1,
      },
      {
        freeze: true,
        description: "Description",
        address: "D48258F168",
        value: 1,
      },
      ...Array(10)
        .fill(0)
        .map(() => ({
          freeze: false,
          description: "Description",
          address: "D48257F8F8",
          value: 1,
        })),
    ],
    []
  );

  // Column definitions for address table
  const addressColumnHelper = createColumnHelper<AddressRow>();
  const addressColumns = useMemo(
    () => [
      addressColumnHelper.accessor("address", {
        header: "Address",
        cell: (info) => info.getValue(),
      }),
      addressColumnHelper.accessor("value", {
        header: "Value",
        cell: (info) => info.getValue(),
      }),
      addressColumnHelper.accessor("previousValue", {
        header: "Previous Value",
        cell: (info) => info.getValue(),
      }),
    ],
    []
  );

  // Column definitions for freeze table
  const freezeColumnHelper = createColumnHelper<FreezeRow>();
  const freezeColumns = useMemo(
    () => [
      freezeColumnHelper.accessor("freeze", {
        header: "Freeze",
        cell: (info) => <Switch checked={info.getValue()} />,
      }),
      freezeColumnHelper.accessor("description", {
        header: "Description",
        cell: (info) => info.getValue(),
      }),
      freezeColumnHelper.accessor("address", {
        header: "Address",
        cell: (info) => info.getValue(),
      }),
      freezeColumnHelper.accessor("value", {
        header: "Value",
        cell: (info) => info.getValue(),
      }),
    ],
    []
  );

  // Set up address table
  const addressTable = useReactTable({
    data: addressData,
    columns: addressColumns,
    state: {
      sorting: addressSorting,
    },
    onSortingChange: setAddressSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  // Set up freeze table
  const freezeTable = useReactTable({
    data: freezeData,
    columns: freezeColumns,
    state: {
      sorting: freezeSorting,
    },
    onSortingChange: setFreezeSorting,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  return (
    <div className="flex flex-col h-screen">
      {/* Title bar */}
      <div className="text-xs text-center">0x0000GAC4 - OneDrive.exe</div>

      {/* Progress bar */}
      <Progress value={50} />

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Upper panel */}
        <div className="flex flex-row">
          {/* Left panel - Memory addresses */}
          <div className="flex-1 overflow-hidden flex flex-col">
            <div className="text-sm">Found 89,325</div>

            <div className="flex-1 overflow-auto">
              <table className="w-full text-sm">
                <thead>
                  {addressTable.getHeaderGroups().map((headerGroup) => (
                    <tr key={headerGroup.id} className="bg-muted">
                      {headerGroup.headers.map((header) => (
                        <th
                          key={header.id}
                          className="text-left p-2 border-r border-border cursor-pointer"
                          onClick={header.column.getToggleSortingHandler()}
                        >
                          <div className="flex items-center">
                            {flexRender(
                              header.column.columnDef.header,
                              header.getContext()
                            )}
                            {{
                              asc: <ChevronUp className="ml-1 h-3 w-3" />,
                              desc: <ChevronDown className="ml-1 h-3 w-3" />,
                            }[header.column.getIsSorted() as string] ?? null}
                          </div>
                        </th>
                      ))}
                    </tr>
                  ))}
                </thead>
                <tbody>
                  {addressTable.getRowModel().rows.map((row) => (
                    <tr
                      key={row.id}
                      className={`border-b border-border ${
                        row.original.changed
                          ? "bg-destructive/20 text-destructive"
                          : ""
                      }`}
                    >
                      {row.getVisibleCells().map((cell, index) => (
                        <td
                          key={cell.id}
                          className={`p-2 ${
                            index < row.getVisibleCells().length - 1
                              ? "border-r border-border"
                              : ""
                          }`}
                        >
                          {flexRender(
                            cell.column.columnDef.cell,
                            cell.getContext()
                          )}
                        </td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          {/* Right panel - Search options */}
          <div className="w-[325px] flex flex-col p-2">
            <div className="relative">
              <Search className="absolute left-2 top-1.5 w-4 text-muted-foreground pointer-events-none" />
              <Input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-8 pr-8"
              />
              <X
                className="absolute right-2 top-1.5 w-4 text-muted-foreground cursor-pointer"
                onClick={() => setSearchQuery("")}
              />
            </div>

            <div className="flex gap-1">
              <Button variant="default">First Scan</Button>
              <Button variant="secondary">New Scan</Button>
            </div>

            <div className="flex items-center">
              <Label htmlFor="scanType" className="w-[90px]">
                Scan Type
              </Label>
              <Select defaultValue="exact">
                <SelectTrigger id="scanType" className="w-full">
                  <SelectValue placeholder="Scan Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="exact">Exact Value</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center">
              <Label htmlFor="valueType" className="w-[90px]">
                Value Type
              </Label>
              <Select defaultValue="integer">
                <SelectTrigger id="valueType" className="w-full">
                  <SelectValue placeholder="Value Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="integer">Integer (4 Bytes)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="mt-4">
              <div className="text-center text-sm border-b mb-2">
                Memory Scan Options
              </div>
              <Select defaultValue="all">
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="All" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All</SelectItem>
                </SelectContent>
              </Select>
              <div className="flex gap-2 items-center">
                <Label>Start</Label>
                <Input defaultValue="0" className="w-full" />
              </div>
              <div className="flex gap-2 items-center">
                <Label>Stop</Label>
                <Input defaultValue="7FFFFFFFFFFFFFFF" className="w-full" />
              </div>
              i was here
              <div className="grid grid-cols-3 gap-2 mb-2">
                <div>
                  <span className="text-sm block mb-1">Writeable</span>
                  <Select defaultValue="yes">
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Yes" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="yes">Yes</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <span className="text-sm block mb-1">Executable</span>
                  <Select defaultValue="dontcare">
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Don't Care" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="dontcare">Don't Care</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div>
                  <span className="text-sm block mb-1">Copy On Write</span>
                  <Select defaultValue="no">
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="No" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="no">No</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-sm">Memory Types</span>
                <div className="flex gap-1">
                  <Badge
                    variant="secondary"
                    className="flex items-center gap-1"
                  >
                    Image <X className="h-3 w-3" />
                  </Badge>
                  <Badge
                    variant="secondary"
                    className="flex items-center gap-1"
                  >
                    Private <X className="h-3 w-3" />
                  </Badge>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Bottom table */}
        {/* <div className="flex-1 overflow-auto mt-2">
          <table className="w-full text-sm">
            <thead>
              {freezeTable.getHeaderGroups().map((headerGroup) => (
                <tr key={headerGroup.id} className="bg-muted">
                  {headerGroup.headers.map((header) => (
                    <th
                      key={header.id}
                      className="text-left p-2 border-r border-border cursor-pointer"
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      <div className="flex items-center">
                        {flexRender(
                          header.column.columnDef.header,
                          header.getContext()
                        )}
                        {{
                          asc: <ChevronUp className="ml-1 h-3 w-3" />,
                          desc: <ChevronDown className="ml-1 h-3 w-3" />,
                        }[header.column.getIsSorted() as string] ?? null}
                      </div>
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody>
              {freezeTable.getRowModel().rows.map((row) => (
                <tr key={row.id} className="border-b border-border">
                  {row.getVisibleCells().map((cell, index) => (
                    <td
                      key={cell.id}
                      className={`p-2 ${
                        index < row.getVisibleCells().length - 1
                          ? "border-r border-border"
                          : ""
                      }`}
                    >
                      {flexRender(
                        cell.column.columnDef.cell,
                        cell.getContext()
                      )}
                    </td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div> */}
      </div>
    </div>
  );
}
