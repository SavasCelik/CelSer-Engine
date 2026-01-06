import { DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "./ui/input";
import { TrackedItem } from "@/types/TrackedItem";
import { DotNetObject } from "@/utils/useDotNet";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Row } from "@tanstack/react-table";
import { z } from "zod";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import {
  ArrowLeftIcon,
  ArrowRightIcon,
  Loader2Icon,
  Minus,
  Plus,
} from "lucide-react";
import { ButtonGroup } from "./ui/button-group";
import { cn } from "@/lib/utils";
import {
  InputGroup,
  InputGroupAddon,
  InputGroupInput,
  InputGroupText,
} from "./ui/input-group";
import { useEffect } from "react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { scanValueTypes } from "@/constants/ScanValueTypes";

const formSchema = z.object({
  address: z.string(),
  description: z.string().max(50),
  dataType: z.string(),
  isPointer: z.boolean().optional(),
  offsets: z
    .array(
      z.object({
        value: z.string(),
      })
    )
    .optional(),
  moduleNameWithBaseOffset: z.string().optional(),
});
type FormDataType = z.infer<typeof formSchema>;

type PointerOffsetPathsResponse = {
  moduleNameWithBaseOffset: string;
  offsets: string[];
};

type TrackedItemAdvancedFormProps = {
  row: Row<TrackedItem>;
  dotNetObj: DotNetObject | null;
  onOpenChange: (open: boolean) => void;
};

export default function TrackedItemAdvancedForm({
  row,
  dotNetObj,
  onOpenChange,
}: TrackedItemAdvancedFormProps) {
  const queryClient = useQueryClient();
  const editTrackedItemsMutation = useMutation({
    mutationFn: (data: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      return dotNetObj.invokeMethod("UpdateItem", row.index, {
        address: data.address,
        description: data.description,
        isPointer: data.isPointer,
        offsets: data.offsets?.reverse().map((x) => x.value),
        moduleNameWithBaseOffset: data.moduleNameWithBaseOffset,
      });
    },
    onSuccess: () => {
      onOpenChange(false);
      queryClient.invalidateQueries({
        queryKey: ["TrackedItemsTable"],
      });
    },
  });

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      address: row.original.pointingTo ?? row.original.address,
      description: row.original.description,
      dataType: row.original.dataType,
      isPointer: row.original.isPointer,
      offsets: row.original.offsets.map((offset) => ({ value: offset })),
      moduleNameWithBaseOffset: row.original.moduleNameWithBaseOffset ?? "",
    },
    disabled: editTrackedItemsMutation.isPending,
  });

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: "offsets",
  });

  const selectedOffsets = form.watch("offsets");
  const selectedOffsetValues = selectedOffsets?.map((o) => o.value);
  const enteredModuleNameWithBaseOffset = form.watch(
    "moduleNameWithBaseOffset"
  );

  const getPointerOffsetPathsQuery = useQuery<PointerOffsetPathsResponse>({
    queryKey: [
      "GetPointerOffsetPaths",
      enteredModuleNameWithBaseOffset,
      selectedOffsetValues,
    ],
    enabled: dotNetObj != null && row.original.isPointer,
    queryFn: () =>
      dotNetObj!.invokeMethod(
        "GetPointerOffsetPaths",
        enteredModuleNameWithBaseOffset,
        selectedOffsetValues
      ),
  });

  const currentAddress = form.watch("address");
  const currentDataType = form.watch("dataType");
  const getAddressValue = useQuery<string>({
    queryKey: ["GetAddressValue", currentAddress, currentDataType],
    enabled: dotNetObj != null,
    queryFn: () =>
      dotNetObj!.invokeMethod(
        "GetAddressValue",
        currentAddress,
        currentDataType
      ),
  });

  const changeOffset = (index: number, delta: number) => {
    const field = `offsets.${index}.value` as const;
    const currentValue = form.getValues(field);

    const newValue = parseInt(currentValue, 16) + delta;
    form.setValue(field, newValue.toString(16).toUpperCase());
  };

  useEffect(() => {
    if (!row.original.isPointer) return;

    const queryData = getPointerOffsetPathsQuery.data;
    if (queryData) {
      form.setValue("address", queryData.offsets[queryData.offsets.length - 1]);
    }
  }, [row.original.isPointer, getPointerOffsetPathsQuery.data, form]);

  function onSaveChanges(data: FormDataType) {
    editTrackedItemsMutation.mutate(data);
  }

  return (
    <Form {...form}>
      <form className="grid gap-y-2">
        <FormField
          control={form.control}
          name="address"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Address</FormLabel>
              <FormControl>
                <InputGroup>
                  <InputGroupInput
                    {...field}
                    readOnly={row.original.isPointer}
                    className={cn("flex-[1_0_55%]", {
                      "opacity-50": row.original.isPointer,
                    })}
                  />
                  <InputGroupAddon align="inline-end" className="min-w-0">
                    <InputGroupText
                      className="block truncate"
                      title={getAddressValue.data}
                    >
                      ={getAddressValue.data}
                    </InputGroupText>
                  </InputGroupAddon>
                </InputGroup>
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="dataType"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Type</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger {...field} className="col-span-4 w-full">
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
        {row.original.isPointer && (
          <>
            <div className="grid min-w-0 gap-y-1">
              {[...fields].reverse().map((field, reversedIndex) => {
                const lastIndex = fields.length - 1;
                const originalIndex = lastIndex - reversedIndex;
                const isLastField = originalIndex === lastIndex;
                const queryData = getPointerOffsetPathsQuery.data;

                const previousOffsetPointingTo =
                  originalIndex === 0
                    ? queryData?.moduleNameWithBaseOffset
                    : queryData?.offsets[originalIndex - 1];

                const currentOffsetPointingTo =
                  queryData?.offsets[originalIndex];

                return (
                  <FormField
                    key={field.id}
                    control={form.control}
                    name={`offsets.${originalIndex}.value`}
                    render={({ field }) => (
                      <FormItem className="flex min-w-0">
                        <ButtonGroup>
                          <Button
                            type="button"
                            variant={"secondary"}
                            onClick={() => changeOffset(originalIndex, -8)}
                          >
                            <ArrowLeftIcon />
                          </Button>
                          <FormControl>
                            <Input
                              {...field}
                              className="h-7.5 w-13 p-0 text-center"
                            />
                          </FormControl>
                          <Button
                            type="button"
                            variant={"secondary"}
                            onClick={() => {
                              changeOffset(originalIndex, 8);
                            }}
                          >
                            <ArrowRightIcon />
                          </Button>
                        </ButtonGroup>
                        <div className="flex min-w-0 items-center gap-0.5 text-sm">
                          {isLastField ? (
                            <span className="truncate">
                              {previousOffsetPointingTo}+{field.value} ={" "}
                              {currentOffsetPointingTo}
                            </span>
                          ) : (
                            <>
                              <span className="shrink-0">
                                [{previousOffsetPointingTo}+{field.value}]
                              </span>

                              <ArrowRightIcon size={14} className="shrink-0" />

                              <span className="truncate">
                                {currentOffsetPointingTo}
                              </span>
                            </>
                          )}
                        </div>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                );
              })}
            </div>
            <FormField
              control={form.control}
              name="moduleNameWithBaseOffset"
              render={({ field }) => (
                <FormItem className="grid-cols-2 gap-1">
                  <FormControl>
                    <Input {...field} />
                  </FormControl>
                  <div className="flex items-center gap-0.5 text-sm">
                    <ArrowRightIcon size={14} className="shrink-0" />
                    <span className="truncate">
                      {
                        getPointerOffsetPathsQuery.data
                          ?.moduleNameWithBaseOffset
                      }
                    </span>
                  </div>
                  <FormMessage />
                </FormItem>
              )}
            />
            <div className="flex gap-1">
              <Button
                type="button"
                variant={"secondary"}
                onClick={() => append({ value: "0" })}
              >
                <Plus />
                Add Offset
              </Button>
              <Button
                type="button"
                variant={"secondary"}
                onClick={() => remove(fields.length - 1)}
              >
                <Minus />
                Remove Offset
              </Button>
            </div>
          </>
        )}
        <DialogFooter>
          <Button
            type="submit"
            onClick={form.handleSubmit(onSaveChanges)}
            disabled={
              !form.formState.isValid || editTrackedItemsMutation.isPending
            }
          >
            {editTrackedItemsMutation.isPending && (
              <Loader2Icon className="animate-spin" />
            )}
            Save changes
          </Button>
        </DialogFooter>
      </form>
    </Form>
  );
}
