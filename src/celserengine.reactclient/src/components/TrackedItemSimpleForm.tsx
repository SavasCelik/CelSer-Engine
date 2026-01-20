import { DialogFooter } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "./ui/input";
import { TrackedItem } from "@/types/TrackedItem";
import { DotNetObject } from "@/utils/useDotNet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Row } from "@tanstack/react-table";
import { z } from "zod";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Form } from "./ui/form";
import { Loader2Icon } from "lucide-react";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "./ui/select";
import { scanValueTypes } from "@/constants/ScanValueTypes";
import { Field, FieldError, FieldGroup, FieldLabel } from "./ui/field";

type TrackedItemSimpleFormProps = {
  rows: Row<TrackedItem>[];
  trackedItemKey: keyof TrackedItem;
  dotNetObj: DotNetObject | null;
  onOpenChange: (open: boolean) => void;
};

const formSchema = z.object({
  newValue: z.string().refine((val) => val.trim().length > 0, {
    message: "Value cannot be empty",
  }),
});
type FormDataType = z.infer<typeof formSchema>;

export default function TrackedItemSimpleForm({
  rows,
  trackedItemKey,
  dotNetObj,
  onOpenChange,
}: TrackedItemSimpleFormProps) {
  const trackedItemKeyDisplayText =
    trackedItemKey.charAt(0).toUpperCase() + trackedItemKey.slice(1);
  const queryClient = useQueryClient();
  const isDataTypeDialog = trackedItemKey === "dataType";

  const editTrackedItemsMutation = useMutation({
    mutationFn: (data: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      const indices = rows.map((row) => row.index);

      return dotNetObj.invokeMethod(
        "UpdateItems",
        indices,
        trackedItemKey,
        data.newValue
      );
    },
    onSuccess: () => {
      onOpenChange(false);
      queryClient.invalidateQueries({
        queryKey: ["TrackedItemsTable"],
      });
    },
    onError: (error) => {
      form.setError("newValue", { message: error.message });
      setTimeout(() => form.setFocus("newValue"), 100);
    },
  });

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      newValue: rows[0].original[trackedItemKey] as string,
    },
    disabled: editTrackedItemsMutation.isPending,
  });

  function onSaveChanges(data: FormDataType) {
    editTrackedItemsMutation.mutate(data);
  }

  return (
    <Form {...form}>
      <form>
        <FieldGroup>
          <Controller
            name="newValue"
            control={form.control}
            render={({ field, fieldState }) => (
              <Field
                data-invalid={fieldState.invalid}
                className="grid grid-cols-5 gap-y-1"
                orientation="horizontal"
              >
                <FieldLabel htmlFor={field.name}>
                  {trackedItemKeyDisplayText}
                </FieldLabel>
                {isDataTypeDialog ? (
                  <Select onValueChange={field.onChange} value={field.value}>
                    <SelectTrigger
                      {...field}
                      id={field.name}
                      className="col-span-4"
                      aria-invalid={fieldState.invalid}
                    >
                      <SelectValue placeholder="Value Type" />
                    </SelectTrigger>
                    <SelectContent>
                      {scanValueTypes.map((type) => (
                        <SelectItem key={type.id} value={type.id}>
                          {type.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                ) : (
                  <Input
                    className="col-span-4"
                    id={field.name}
                    {...field}
                    aria-invalid={fieldState.invalid}
                  />
                )}
                {fieldState.invalid && (
                  <FieldError
                    className="col-span-4 col-start-2"
                    errors={[fieldState.error]}
                  />
                )}
              </Field>
            )}
          />
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
        </FieldGroup>
      </form>
    </Form>
  );
}
