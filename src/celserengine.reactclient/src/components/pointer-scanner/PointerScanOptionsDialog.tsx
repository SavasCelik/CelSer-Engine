import { Controller, useForm } from "react-hook-form";
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "../ui/dialog";
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
  FieldLegend,
  FieldSeparator,
  FieldSet,
} from "../ui/field";
import { Input } from "../ui/input";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "../ui/accordion";
import { Checkbox } from "../ui/checkbox";
import { Tooltip, TooltipContent, TooltipTrigger } from "../ui/tooltip";
import { Button } from "../ui/button";
import { Loader2Icon } from "lucide-react";
import { zodResolver } from "@hookform/resolvers/zod";
import { DotNetObject } from "@/utils/useDotNet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useSearchParams } from "react-router";
import z from "zod";

const maxSupportedMaxLevel = 30;
const formSchema = z
  .object({
    scanAddress: z.string(),
    requireAlignedPointers: z.boolean(),
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
        return (
          !isNaN(num) &&
          Number.isInteger(num) &&
          num > 0 &&
          num <= maxSupportedMaxLevel
        );
      },
      {
        message: "Max level must be a positive integer and less than 31",
      }
    ),
    maxParallelWorkers: z.string().refine(
      (val) => {
        const num = Number(val);
        return (
          !isNaN(num) &&
          Number.isInteger(num) &&
          num > 0 &&
          num <= navigator.hardwareConcurrency
        );
      },
      {
        message:
          "Max parallel workers can be from 1 to " +
          navigator.hardwareConcurrency,
      }
    ),
    limitToMaxOffsetsPerNode: z.boolean(),
    maxOffsetsPerNode: z.string().optional(),
    preventLoops: z.boolean(),
    allowThreadStacksAsStatic: z.boolean(),
    threadStacks: z.string().optional(),
    stackSize: z.string().optional(),
    allowReadOnlyPointers: z.boolean(),
    onlyOneStaticInPath: z.boolean(),
    onlyResidentMemory: z.boolean(),
    storagePath: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    if (data.limitToMaxOffsetsPerNode) {
      const num = Number(data.maxOffsetsPerNode);
      if (isNaN(num) || !Number.isInteger(num) || num <= 0) {
        ctx.addIssue({
          code: "custom",
          path: ["maxOffsetsPerNode"],
          message: "Max offsets per node must be a positive integer",
        });
      }
    }

    if (data.allowThreadStacksAsStatic) {
      const threadStacksNum = Number(data.threadStacks);
      const stackSizeNum = Number(data.stackSize);

      if (
        isNaN(threadStacksNum) ||
        !Number.isInteger(threadStacksNum) ||
        threadStacksNum <= 0
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["threadStacks"],
          message: "Thread stacks must be a positive integer",
        });
      }

      if (
        isNaN(stackSizeNum) ||
        !Number.isInteger(stackSizeNum) ||
        stackSizeNum <= 0
      ) {
        ctx.addIssue({
          code: "custom",
          path: ["stackSize"],
          message: "Stack size must be a positive integer",
        });
      }
    }
  });

type FormDataType = z.infer<typeof formSchema>;

type PointerScanOptionsDialogProps = {
  dotNetObj: DotNetObject | null;
  setMaxOffsetCols: (maxOffsetCols: number) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

export default function PointerScanOptionsDialog({
  dotNetObj,
  setMaxOffsetCols,
  open,
  onOpenChange,
}: PointerScanOptionsDialogProps) {
  const [searchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const startPointerScanMutation = useMutation({
    mutationKey: ["StartPointerScan"],
    mutationFn: (data: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }
      onOpenChange(false);

      const pointerScanOptions = {
        ...data,
        maxOffset: Number(data.maxOffset),
        maxLevel: Number(data.maxLevel),
        maxParallelWorkers: Number(data.maxParallelWorkers),
        maxOffsetsPerNode: data.limitToMaxOffsetsPerNode
          ? Number(data.maxOffsetsPerNode)
          : 0,
        threadStacks: data.allowThreadStacksAsStatic
          ? Number(data.threadStacks)
          : 0,
        stackSize: data.allowThreadStacksAsStatic ? Number(data.stackSize) : 0,
      };

      return dotNetObj.invokeMethod("StartPointerScan", pointerScanOptions);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["PointerScanResultsTable"],
      });
    },
    onError: () => {
      onOpenChange(true);
    },
  });

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      scanAddress: searchParams.get("searchedAddress") ?? "",
      maxOffset: (0x1000).toString(),
      maxLevel: "4",
      requireAlignedPointers: true,
      maxParallelWorkers: navigator.hardwareConcurrency.toString(),
      limitToMaxOffsetsPerNode: true,
      maxOffsetsPerNode: "3",
      preventLoops: true,
      allowThreadStacksAsStatic: true,
      threadStacks: "2",
      stackSize: (0x1000).toString(),
      allowReadOnlyPointers: false,
      onlyOneStaticInPath: false,
      onlyResidentMemory: false,
    },
    disabled: startPointerScanMutation.isPending,
  });

  async function onSubmit(data: FormDataType) {
    const storage = await dotNetObj?.invokeMethod<string>("SelectStorage");

    if (!storage) {
      return;
    }

    data.storagePath = storage;
    startPointerScanMutation.mutate(data);
    setMaxOffsetCols(Number(data.maxLevel));
  }
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="focus-visible:outline-none sm:max-w-156.25"
        onInteractOutside={(e) => e.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>Pointer scanner options</DialogTitle>
          <DialogDescription></DialogDescription>
        </DialogHeader>
        <form>
          <FieldGroup className="gap-4">
            <FieldGroup>
              <Controller
                name="scanAddress"
                control={form.control}
                render={({ field, fieldState }) => (
                  <Field data-invalid={fieldState.invalid}>
                    <FieldLabel htmlFor={field.name}>Scan Address</FieldLabel>
                    <Input
                      {...field}
                      id={field.name}
                      aria-invalid={fieldState.invalid}
                    />
                    {fieldState.invalid && (
                      <FieldError errors={[fieldState.error]} />
                    )}
                  </Field>
                )}
              />
              <div className="grid grid-cols-3 gap-4">
                <Controller
                  name="maxOffset"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Max Offset</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        aria-invalid={fieldState.invalid}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />
                <Controller
                  name="maxLevel"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>Max Level</FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        aria-invalid={fieldState.invalid}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />
                <Controller
                  name="maxParallelWorkers"
                  control={form.control}
                  render={({ field, fieldState }) => (
                    <Field data-invalid={fieldState.invalid}>
                      <FieldLabel htmlFor={field.name}>
                        Max Parallel Workers
                      </FieldLabel>
                      <Input
                        {...field}
                        id={field.name}
                        aria-invalid={fieldState.invalid}
                      />
                      {fieldState.invalid && (
                        <FieldError errors={[fieldState.error]} />
                      )}
                    </Field>
                  )}
                />
              </div>
            </FieldGroup>
            <FieldSeparator />
            <FieldSet>
              <Accordion type="single" collapsible>
                <AccordionItem value="advanced-options">
                  <AccordionTrigger className="mb-3 p-0">
                    <FieldLegend className="mb-0">Advanced options</FieldLegend>
                  </AccordionTrigger>
                  <AccordionContent className="m-1">
                    <FieldGroup className="grid grid-cols-2">
                      <div className="flex flex-col gap-2">
                        <Controller
                          name="requireAlignedPointers"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                              className="h-7"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    Addresses must be 32-bit aligned
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Limits pointer scanning to 32-bit aligned
                                    addresses (divisible by 4), which is typical
                                    for valid pointers and can reduce scan noise
                                  </p>
                                  <p>Disable to include unaligned addresses</p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Faster, cleaner
                                    results
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Some valid pointers
                                    may be skipped.
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                        <Controller
                          name="preventLoops"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    No looping pointers
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Removes pointer paths that loop back on
                                    themselves
                                  </p>
                                  <p>
                                    For example,
                                    <code className="px-[0.3rem] py-[0.2rem] font-mono text-sm font-semibold">
                                      base →{" "}
                                      <span className="underline decoration-emerald-500 decoration-2">
                                        p1
                                      </span>{" "}
                                      → p2 → p3 →{" "}
                                      <span className="underline decoration-emerald-500 decoration-2">
                                        p1
                                      </span>{" "}
                                      → p4
                                    </code>
                                    is unnecessary, since
                                    <code className="px-[0.3rem] py-[0.2rem] font-mono text-sm font-semibold">
                                      base → p1 → p4
                                    </code>
                                    works just as well
                                  </p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Fewer results and less
                                    disk space used
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Slightly slower scans
                                    due to extra checks
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                        <Controller
                          name="allowReadOnlyPointers"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    Include read-only pointers
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Includes read-only memory when scanning for
                                    pointer paths. This allows paths that pass
                                    through read-only blocks to be found
                                  </p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Works even when
                                    pointers are marked read-only
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Takes longer and adds
                                    lots of likely useless results
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                        <Controller
                          name="onlyOneStaticInPath"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    Stop traversing a path when a static has
                                    been found
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Don't follow a pointer path past the first
                                    static pointer found
                                  </p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Faster scan with fewer
                                    results
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Some pointer paths
                                    beyond the first static may be skipped
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                        <Controller
                          name="onlyResidentMemory"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    Only scan resident memory
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Scan only resident memory (memory currently
                                    loaded in RAM)
                                  </p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Limits scan to
                                    resident memory
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Paged out memory is
                                    ignored
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                      </div>
                      <div className="flex flex-col gap-2">
                        <div className="flex gap-1">
                          <Controller
                            name="limitToMaxOffsetsPerNode"
                            control={form.control}
                            render={({ field, fieldState }) => (
                              <Field
                                data-invalid={fieldState.invalid}
                                orientation="horizontal"
                                className="h-7 w-50"
                              >
                                <Checkbox
                                  id={field.name}
                                  name={field.name}
                                  checked={field.value}
                                  onCheckedChange={field.onChange}
                                  aria-invalid={fieldState.invalid}
                                />
                                <Tooltip
                                  delayDuration={500}
                                  disableHoverableContent={true}
                                >
                                  <TooltipTrigger asChild>
                                    <FieldLabel
                                      htmlFor={field.name}
                                      className="font-normal"
                                    >
                                      Limit offsets per node
                                    </FieldLabel>
                                  </TooltipTrigger>
                                  <TooltipContent>
                                    <p>
                                      Restrict the number of offsets that can be
                                      followed from each pointer node
                                    </p>

                                    <p className="mt-2">
                                      <strong>Pro:</strong> Very fast and
                                      produces the shortest pointer paths
                                    </p>
                                    <p>
                                      <strong>Con:</strong> Some pointer paths
                                      may be skipped if the limit is reached
                                    </p>
                                  </TooltipContent>
                                </Tooltip>
                                {fieldState.invalid && (
                                  <FieldError errors={[fieldState.error]} />
                                )}
                              </Field>
                            )}
                          />
                          <Controller
                            name="maxOffsetsPerNode"
                            control={form.control}
                            render={({ field, fieldState }) => (
                              <Field
                                data-invalid={fieldState.invalid}
                                orientation="horizontal"
                                className="w-16"
                              >
                                <Input
                                  className="h-7"
                                  {...field}
                                  id={field.name}
                                  aria-invalid={fieldState.invalid}
                                  disabled={
                                    !form.watch("limitToMaxOffsetsPerNode")
                                  }
                                />
                                {fieldState.invalid && (
                                  <FieldError errors={[fieldState.error]} />
                                )}
                              </Field>
                            )}
                          />
                        </div>
                        <Controller
                          name="allowThreadStacksAsStatic"
                          control={form.control}
                          render={({ field, fieldState }) => (
                            <Field
                              data-invalid={fieldState.invalid}
                              orientation="horizontal"
                            >
                              <Checkbox
                                id={field.name}
                                name={field.name}
                                checked={field.value}
                                onCheckedChange={field.onChange}
                                aria-invalid={fieldState.invalid}
                              />
                              <Tooltip
                                delayDuration={500}
                                disableHoverableContent={true}
                              >
                                <TooltipTrigger asChild>
                                  <FieldLabel
                                    htmlFor={field.name}
                                    className="font-normal"
                                  >
                                    Allow thread stacks as static
                                  </FieldLabel>
                                </TooltipTrigger>
                                <TooltipContent>
                                  <p>
                                    Allow thread stack memory to be considered
                                    static
                                  </p>

                                  <p className="mt-2">
                                    <strong>Pro:</strong> Thread stack addresses
                                    can be used as base pointers
                                  </p>
                                  <p>
                                    <strong>Con:</strong> Some stack pointers
                                    may be temporary and become invalid
                                  </p>
                                </TooltipContent>
                              </Tooltip>
                              {fieldState.invalid && (
                                <FieldError errors={[fieldState.error]} />
                              )}
                            </Field>
                          )}
                        />
                        <div className="flex flex-col gap-1 ps-7">
                          <Controller
                            name="threadStacks"
                            control={form.control}
                            render={({ field, fieldState }) => (
                              <Field
                                data-invalid={fieldState.invalid}
                                orientation="horizontal"
                                data-disabled={
                                  !form.watch("allowThreadStacksAsStatic")
                                }
                              >
                                <FieldLabel
                                  htmlFor={field.name}
                                  className="w-70 font-normal"
                                >
                                  Number of threads
                                </FieldLabel>
                                <Input
                                  {...field}
                                  id={field.name}
                                  aria-invalid={fieldState.invalid}
                                  className="h-7"
                                  disabled={
                                    !form.watch("allowThreadStacksAsStatic")
                                  }
                                />
                                {fieldState.invalid && (
                                  <FieldError errors={[fieldState.error]} />
                                )}
                              </Field>
                            )}
                          />
                          <Controller
                            name="stackSize"
                            control={form.control}
                            render={({ field, fieldState }) => (
                              <Field
                                data-invalid={fieldState.invalid}
                                orientation="horizontal"
                                data-disabled={
                                  !form.watch("allowThreadStacksAsStatic")
                                }
                              >
                                <FieldLabel
                                  htmlFor={field.name}
                                  className="w-70 font-normal"
                                >
                                  Thread's stack size
                                </FieldLabel>
                                <Input
                                  {...field}
                                  id={field.name}
                                  aria-invalid={fieldState.invalid}
                                  className="h-7"
                                  disabled={
                                    !form.watch("allowThreadStacksAsStatic")
                                  }
                                />
                                {fieldState.invalid && (
                                  <FieldError errors={[fieldState.error]} />
                                )}
                              </Field>
                            )}
                          />
                        </div>
                      </div>
                    </FieldGroup>
                  </AccordionContent>
                </AccordionItem>
              </Accordion>
            </FieldSet>
          </FieldGroup>
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
        </form>
      </DialogContent>
    </Dialog>
  );
}
