"use client";

import { useState, useMemo } from "react";
import {
  Search,
  X,
  ChevronDown,
  ChevronUp,
  Loader2,
  LoaderCircle,
  Check,
} from "lucide-react";
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
import { MultiSelect } from "@/components/multi-select";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group";
import { Toggle } from "@/components/ui/toggle";
import ScanResultTableShadCn from "./ScanResultTableShadCn";
import TrackedItemsTable from "./TrackedItemsTable";

// Define types for our tables
type AddressRow = {
  address: string;
  value: number;
  previousValue: number;
  changed: boolean;
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
    [],
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
    [],
  );

  const [selected, setSelected] = useState<string[]>([]);
  const [memoryTypes, setMemoryTypes] = useState({
    image: true,
    private: true,
    mapped: false,
  });

  return (
    <div className="bg-card flex h-screen flex-col p-3">
      {/* Title bar */}
      <div className="text-center text-xs">0x0000GAC4 - OneDrive.exe</div>

      {/* Progress bar */}
      <Progress value={50} />

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Upper panel */}
        <div className="flex flex-row gap-2">
          {/* Left panel - Memory addresses */}
          <ScanResultTableShadCn />

          {/* Right panel - Search options */}
          <div className="mt-5 flex w-[330px] flex-col gap-2">
            <div className="relative">
              <Search className="text-muted-foreground pointer-events-none absolute top-0 left-2 h-7.5 w-4" />
              <Input
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="h-7.5 pr-8 pl-8"
              />
              <X
                className="text-muted-foreground absolute top-0 right-2 h-7.5 w-4 cursor-pointer"
                onClick={() => setSearchQuery("")}
              />
            </div>

            <div className="flex gap-1">
              <Button variant="default">First Scan</Button>
              <Button variant="outline">New Scan</Button>
            </div>

            <div className="flex items-center">
              <Label htmlFor="scanType" className="w-[90px]">
                Scan Type
              </Label>
              <Select defaultValue="exact">
                <SelectTrigger id="scanType" className="w-full" size="xs">
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
                <SelectTrigger id="valueType" className="w-full" size="xs">
                  <SelectValue placeholder="Value Type" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="integer">Integer (4 Bytes)</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="mt-4 flex flex-col gap-2">
              <div className="border-b text-center text-sm">
                Memory Scan Options
              </div>
              <Select defaultValue="all">
                <SelectTrigger className="w-full" size="xs">
                  <SelectValue placeholder="All" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All</SelectItem>
                </SelectContent>
              </Select>
              <div className="flex items-center gap-2">
                <Label htmlFor="startAddress">Start</Label>
                <Input
                  id="startAddress"
                  defaultValue="0"
                  className="h-7.5 w-full"
                />
              </div>
              <div className="flex items-center gap-2">
                <Label htmlFor="stopAddress">Stop</Label>
                <Input
                  id="stopAddress"
                  defaultValue="7FFFFFFFFFFFFFFF"
                  className="h-7.5 w-full"
                />
              </div>
              <div className="grid grid-cols-3 gap-2">
                <div className="flex flex-col gap-2">
                  <Label htmlFor="writeable">Writeable</Label>
                  <Select defaultValue="yes">
                    <SelectTrigger id="writeable" className="w-full" size="xs">
                      <SelectValue placeholder="Yes" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="yes">Yes</SelectItem>
                      <SelectItem value="no">No</SelectItem>
                      <SelectItem value="dontcare">Don't Care</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="executable">Executable</Label>
                  <Select defaultValue="dontcare">
                    <SelectTrigger id="executable" className="w-full" size="xs">
                      <SelectValue placeholder="Don't Care" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="yes">Yes</SelectItem>
                      <SelectItem value="no">No</SelectItem>
                      <SelectItem value="dontcare">Don't Care</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex flex-col gap-2">
                  <Label htmlFor="copyOnWrite">Copy On Write</Label>
                  <Select defaultValue="no">
                    <SelectTrigger
                      id="copyOnWrite"
                      className="w-full"
                      size="xs"
                    >
                      <SelectValue placeholder="No" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="yes">Yes</SelectItem>
                      <SelectItem value="no">No</SelectItem>
                      <SelectItem value="dontcare">Don't Care</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
              </div>
              <div className="flex">
                <Label className="w-[100px]">Memory Types</Label>
                <div className="grid grid-cols-3 gap-1">
                  <Toggle
                    variant="primary"
                    pressed={memoryTypes.image}
                    className={memoryTypes.image ? "" : "px-4"}
                    onPressedChange={(pressed) =>
                      setMemoryTypes({ ...memoryTypes, image: pressed })
                    }
                  >
                    <Label className="gap-1">
                      {memoryTypes.image && <Check />}
                      Image
                    </Label>
                  </Toggle>
                  <Toggle
                    pressed={memoryTypes.private}
                    className={memoryTypes.private ? "" : "px-4"}
                    onPressedChange={(pressed) =>
                      setMemoryTypes({ ...memoryTypes, private: pressed })
                    }
                  >
                    <Label className="gap-1">
                      {memoryTypes.private && <Check />}
                      Private
                    </Label>
                  </Toggle>
                  <Toggle
                    pressed={memoryTypes.mapped}
                    className={memoryTypes.mapped ? "" : "px-4"}
                    onPressedChange={(pressed) =>
                      setMemoryTypes({ ...memoryTypes, mapped: pressed })
                    }
                  >
                    <Label className="gap-1">
                      {memoryTypes.mapped && <Check />}
                      Mapped
                    </Label>
                  </Toggle>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Bottom table */}
        <TrackedItemsTable />
      </div>
    </div>
  );
}
