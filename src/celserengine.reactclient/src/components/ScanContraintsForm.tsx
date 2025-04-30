import { useState } from "react";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm, UseFormReturn } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { Check, Loader2Icon, Search, X } from "lucide-react";
import { Toggle } from "./ui/toggle";
import { DotNetObject } from "../utils/useDotNet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";

const maxIntPtrString = "7FFFFFFFFFFFFFFF";
const maxIntPtr = BigInt(`0x${maxIntPtrString}`);

const scanCompareTypes = [
  {
    id: "exactValue",
    label: "Exact Value",
    hasSearchValue: true,
  },
  {
    id: "biggerThan",
    label: "Bigger than...",
    hasSearchValue: true,
  },
  {
    id: "smallerThan",
    label: "Smaller than...",
    hasSearchValue: true,
  },
  {
    id: "valueBetween",
    label: "Value between...",
    hasSearchValue: true,
  },
  {
    id: "unknownInitialValue",
    label: "Unknown initial value",
    hasSearchValue: false,
  },
  {
    id: "increasedValue",
    label: "Increased value",
    hasSearchValue: false,
  },
  {
    id: "increasedValueBy",
    label: "Increased value by...",
    hasSearchValue: true,
  },
  {
    id: "decreasedValue",
    label: "Decreased value",
    hasSearchValue: false,
  },
  {
    id: "decreasedValueBy",
    label: "Decreased value by...",
    hasSearchValue: true,
  },
  {
    id: "changedValue",
    label: "Changed value",
    hasSearchValue: false,
  },
  {
    id: "unchangedValue",
    label: "Unchanged value",
    hasSearchValue: false,
  },
] as const;

const scanValueTypes = [
  {
    id: "short",
    label: "Short (2 Bytes)",
  },
  {
    id: "integer",
    label: "Integer (4 Bytes)",
  },
  {
    id: "float",
    label: "Float (4 Bytes)",
  },
  {
    id: "long",
    label: "Long (8 Bytes)",
  },
  {
    id: "double",
    label: "Double (8 Bytes)",
  },
] as const;

const memoryTypesArr = [
  {
    id: "image",
    label: "Image",
  },
  {
    id: "private",
    label: "Private",
  },
  {
    id: "mapped",
    label: "Mapped",
  },
] as const;

const formSchema = z
  .object({
    scanValue: z.string().optional(),
    fromValue: z.string().optional(),
    toValue: z.string().optional(),
    scanCompareType: z.enum(
      scanCompareTypes.map((type) => type.id) as [string, ...string[]],
      {
        errorMap: () => ({ message: "Please select a scan type." }),
      }
    ),
    scanValueType: z.enum(["integer", "float"], {
      errorMap: () => ({ message: "Please select a value type." }),
    }),
    startAddress: z.string().refine((val) => validateHexAddress(val), {
      message: "Start address must be a valid hex value.",
    }),
    stopAddress: z.string().refine((val) => validateHexAddress(val), {
      message: "Stop address must be a valid hex value.",
    }),
    writable: z.enum(["yes", "no", "dontcare"], {
      errorMap: () => ({ message: "Please select a writeable option." }),
    }),
    executable: z.enum(["yes", "no", "dontcare"], {
      errorMap: () => ({ message: "Please select an executable option." }),
    }),
    copyOnWrite: z.enum(["yes", "no", "dontcare"], {
      errorMap: () => ({ message: "Please select a copy-on-write option." }),
    }),
    memoryTypes: z.array(z.enum(["image", "private", "mapped"])).optional(),
  })
  .superRefine((data, ctx) => {
    if (
      data.scanCompareType === "valueBetween" &&
      (!data.fromValue || !data.toValue)
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message:
          "Both 'fromValue' and 'toValue' are required for 'Value between...'",
        path: ["toValue"],
      });
    }

    const selectedScanType = scanCompareTypes.find(
      (type) => type.id === data.scanCompareType
    );
    if (
      data.scanCompareType !== "valueBetween" &&
      selectedScanType?.hasSearchValue &&
      !data.scanValue
    ) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: "Provide a value to scan for.",
        path: ["scanValue"],
      });
    }
  });
type FormDataType = z.infer<typeof formSchema>;

function validateHexAddress(value: string) {
  if (!value) {
    return false;
  }

  try {
    const bigIntVal = BigInt("0x" + value);
    return bigIntVal >= 0 && bigIntVal <= maxIntPtr;
  } catch {
    return false;
  }
}

type ScanConstraintsFormProps = {
  dotNetObj: DotNetObject | null;
};

function ScanConstraintsForm({ dotNetObj }: ScanConstraintsFormProps) {
  // When the error message should be cleared when the user starts typing i could use this solution: https://github.com/orgs/react-hook-form/discussions/7333
  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      scanValue: "",
      fromValue: "",
      toValue: "",
      scanCompareType: scanCompareTypes[0].id,
      scanValueType: "integer",
      startAddress: "0",
      stopAddress: maxIntPtrString,
      writable: "yes",
      executable: "dontcare",
      copyOnWrite: "no",
      memoryTypes: ["image", "private"],
    },
  });
  const selectedScanType = form.watch("scanCompareType");
  const [isFirstScan, setIsFirstScan] = useState(true);
  const availableScanCompareTypes = isFirstScan
    ? scanCompareTypes.slice(0, 5)
    : scanCompareTypes.filter((type) => type.id !== "unknownInitialValue");
  const queryClient = useQueryClient();

  const onScanMutation = useMutation({
    mutationFn: (values: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      if (isFirstScan) {
        return dotNetObj.invokeMethod("OnFirstScanAsync", values);
      } else {
        return dotNetObj.invokeMethod("OnNextScanAsync", values);
      }
    },
    onError: (error) => {
      toast.error(error.message);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["ScanResultItemsTable"] });
    },
  });

  const cancelScanMutation = useMutation({
    mutationFn: () => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      return dotNetObj.invokeMethod("CancelScanAsync");
    },
  });

  function onFirstScan(values: FormDataType) {
    onScanMutation.mutate(values, {
      onSuccess: () => {
        setIsFirstScan(false);
      },
    });
  }

  function onNextScan(values: FormDataType) {
    onScanMutation.mutate(values);
  }

  const isCancellingScan =
    cancelScanMutation.isPending ||
    (cancelScanMutation.isSuccess && onScanMutation.isPending);

  return (
    <Form {...form}>
      <form autoComplete="off" className="mt-5 flex w-[330px] flex-col gap-2">
        {selectedScanType === "valueBetween" ? (
          <>
            {SearchInputField(form, "fromValue")}
            {SearchInputField(form, "toValue")}
          </>
        ) : scanCompareTypes.find((type) => type.id === selectedScanType)
            ?.hasSearchValue ? (
          SearchInputField(form, "scanValue")
        ) : null}
        <div className="flex gap-1">
          {onScanMutation.isPending ? (
            <Button
              type="button"
              variant="destructive"
              disabled={isCancellingScan}
              onClick={() => cancelScanMutation.mutate()}
            >
              {isCancellingScan && <Loader2Icon className="animate-spin" />}
              Cancel
            </Button>
          ) : isFirstScan ? (
            <Button
              type="submit"
              variant="default"
              onClick={form.handleSubmit(onFirstScan)}
            >
              First Scan
            </Button>
          ) : (
            <>
              <Button
                type="submit"
                variant="default"
                onClick={form.handleSubmit(onNextScan)}
              >
                Next Scan
              </Button>
              <Button type="button" variant="outline">
                New Scan
              </Button>
            </>
          )}
        </div>
        <FormField
          control={form.control}
          name="scanCompareType"
          render={({ field }) => (
            <FormItem className="flex items-center gap-0">
              <FormLabel className="w-[90px]">Scan Type</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger className="w-full" size="xs">
                    <SelectValue placeholder="Scan Type" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {availableScanCompareTypes.map((type) => (
                    <SelectItem key={type.id} value={type.id}>
                      {type.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="scanValueType"
          render={({ field }) => (
            <FormItem className="flex items-center gap-0">
              <FormLabel className="w-[90px]">Value Type</FormLabel>
              <Select onValueChange={field.onChange} defaultValue={field.value}>
                <FormControl>
                  <SelectTrigger className="w-full" size="xs">
                    <SelectValue placeholder="Value Type" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {scanValueTypes.map((type) => (
                    <SelectItem key={type.id} value={type.id}>
                      {type.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />
        <div className="mt-2 border-b text-center text-sm">
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
        <FormField
          control={form.control}
          name="startAddress"
          render={({ field }) => (
            <FormItem className="flex items-center gap-2">
              <FormLabel>Start</FormLabel>
              <div className="flex flex-col w-full">
                <FormControl>
                  <Input {...field} className="h-7.5 w-full" />
                </FormControl>
                <FormMessage />
              </div>
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="stopAddress"
          render={({ field }) => (
            <FormItem className="flex items-center gap-2">
              <FormLabel>Start</FormLabel>
              <div className="flex flex-col w-full">
                <FormControl>
                  <Input {...field} className="h-7.5 w-full" />
                </FormControl>
                <FormMessage />
              </div>
            </FormItem>
          )}
        />
        <div className="grid grid-cols-3 gap-2">
          <FormField
            control={form.control}
            name="writable"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Writable</FormLabel>
                <Select
                  onValueChange={field.onChange}
                  defaultValue={field.value}
                >
                  <FormControl>
                    <SelectTrigger className="w-full" size="xs">
                      <SelectValue placeholder="Yes" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="yes">Yes</SelectItem>
                    <SelectItem value="no">No</SelectItem>
                    <SelectItem value="dontcare">Don't Care</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="executable"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Executable</FormLabel>
                <Select
                  onValueChange={field.onChange}
                  defaultValue={field.value}
                >
                  <FormControl>
                    <SelectTrigger className="w-full" size="xs">
                      <SelectValue placeholder="Don't Care" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="yes">Yes</SelectItem>
                    <SelectItem value="no">No</SelectItem>
                    <SelectItem value="dontcare">Don't Care</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="copyOnWrite"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Copy On Write</FormLabel>
                <Select
                  onValueChange={field.onChange}
                  defaultValue={field.value}
                >
                  <FormControl>
                    <SelectTrigger className="w-full" size="xs">
                      <SelectValue placeholder="No" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="yes">Yes</SelectItem>
                    <SelectItem value="no">No</SelectItem>
                    <SelectItem value="dontcare">Don't Care</SelectItem>
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
        <div className="flex">
          <FormField
            control={form.control}
            name="memoryTypes"
            render={() => (
              <FormItem className="flex gap-0">
                <FormLabel className="w-[100px]">Memory Types</FormLabel>
                <div className="grid grid-cols-3 gap-1">
                  {memoryTypesArr.map((item) => (
                    <FormField
                      key={item.id}
                      control={form.control}
                      name="memoryTypes"
                      render={({ field }) => {
                        return (
                          <FormItem key={item.id}>
                            <FormControl>
                              <Toggle
                                variant="primary"
                                pressed={field.value?.includes(item.id)}
                                className={
                                  field.value?.includes(item.id) ? "" : "px-4"
                                }
                                onPressedChange={(pressed) => {
                                  return pressed
                                    ? field.onChange([
                                        ...(field.value || []),
                                        item.id,
                                      ])
                                    : field.onChange(
                                        field.value?.filter(
                                          (value) => value !== item.id
                                        )
                                      );
                                }}
                              >
                                <FormLabel className="gap-1">
                                  {field.value?.includes(item.id) && <Check />}
                                  {item.label}
                                </FormLabel>
                              </Toggle>
                            </FormControl>
                          </FormItem>
                        );
                      }}
                    />
                  ))}
                </div>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
      </form>
    </Form>
  );
}

function SearchInputField(
  form: UseFormReturn<FormDataType>,
  inputName: keyof FormDataType
) {
  return (
    <FormField
      control={form.control}
      name={inputName}
      render={({ field }) => (
        <FormItem>
          <FormControl>
            <div className="relative">
              <Search className="text-muted-foreground pointer-events-none absolute top-0 left-2 h-7.5 w-4" />
              <Input className="h-7.5 pr-8 pl-8" {...field} />
              {field.value && (
                <X
                  className="text-muted-foreground absolute top-0 right-2 h-7.5 w-4 cursor-pointer"
                  onClick={() => form.setValue(inputName, "")}
                />
              )}
            </div>
          </FormControl>
          <FormMessage />
        </FormItem>
      )}
    />
  );
}

export default ScanConstraintsForm;
