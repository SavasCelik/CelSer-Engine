import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "./ui/input";
import { TrackedItem } from "@/types/TrackedItem";
import { DotNetObject } from "@/utils/useDotNet";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Row } from "@tanstack/react-table";
import { z } from "zod";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "./ui/form";
import { Loader2Icon } from "lucide-react";
import { useEffect } from "react";

type TrackedItemDialogProps = {
  rows: Row<TrackedItem>[];
  trackedItemKey: keyof TrackedItem;
  dotNetObj: DotNetObject | null;
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

const formSchema = z.object({
  newValue: z.string(),
});
type FormDataType = z.infer<typeof formSchema>;

export default function TrackedItemDialog({
  rows,
  trackedItemKey,
  dotNetObj,
  open,
  onOpenChange,
}: TrackedItemDialogProps) {
  const trackedItemKeyDisplayText =
    trackedItemKey.charAt(0).toUpperCase() + trackedItemKey.slice(1);
  const queryClient = useQueryClient();

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
  });

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      newValue: "",
    },
    disabled: editTrackedItemsMutation.isPending,
  });

  useEffect(() => {
    if (rows.length) {
      form.setValue("newValue", rows[0].original[trackedItemKey] as string);
    }
  }, [form, rows, trackedItemKey]);

  function onSaveChanges(data: FormDataType) {
    editTrackedItemsMutation.mutate(data);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="focus-visible:outline-none sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Change {trackedItemKeyDisplayText}</DialogTitle>
          <DialogDescription></DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form>
            <FormField
              control={form.control}
              name="newValue"
              render={({ field }) => (
                <FormItem className="grid grid-cols-5 items-center gap-0 py-4">
                  <FormLabel>{trackedItemKeyDisplayText}</FormLabel>
                  <FormControl>
                    <Input {...field} className="col-span-4" />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <DialogFooter>
              <Button
                type="submit"
                onClick={form.handleSubmit(onSaveChanges)}
                disabled={editTrackedItemsMutation.isPending}
              >
                {editTrackedItemsMutation.isPending && (
                  <Loader2Icon className="animate-spin" />
                )}
                Save changes
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  );
}
