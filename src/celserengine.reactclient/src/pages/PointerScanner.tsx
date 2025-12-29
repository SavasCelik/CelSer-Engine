import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Loader2Icon } from "lucide-react";
import { z } from "zod";
import { zodResolver } from "@hookform/resolvers/zod";
import { useSearchParams } from "react-router";
import { useMutation } from "@tanstack/react-query";
import { useDotNet } from "@/utils/useDotNet";
import React from "react";
import { useForm } from "react-hook-form";

const formSchema = z.object({
  scanAddress: z.string(),
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
      return !isNaN(num) && Number.isInteger(num) && num > 0;
    },
    {
      message: "Max level must be a positive integer",
    }
  ),
});
type FormDataType = z.infer<typeof formSchema>;

export default function PointerScanner() {
  const [searchParams] = useSearchParams();
  const dotNetObj = useDotNet("PointerScanner", "PointerScannerController");
  const [isDialogOpen, setIsDialogOpen] = React.useState(true);

  const startPointerScanMutation = useMutation({
    mutationFn: (data: FormDataType) => {
      if (!dotNetObj) {
        return Promise.reject();
      }

      return dotNetObj.invokeMethod(
        "StartPointerScan",
        data.scanAddress,
        Number(data.maxOffset),
        Number(data.maxLevel)
      );
    },
    onSuccess: () => {
      setIsDialogOpen(false);
    },
    onError: (error) => {
      form.setError("scanAddress", { message: error.message });
      setTimeout(() => form.setFocus("scanAddress"), 100);
    },
  });

  const form = useForm<FormDataType>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      scanAddress: searchParams.get("searchedAddress") ?? "",
      maxOffset: (0x1000).toString(),
      maxLevel: "4",
    },
    disabled: startPointerScanMutation.isPending,
  });

  function onSubmit(data: FormDataType) {
    startPointerScanMutation.mutate(data);
  }

  return (
    <>
      <div>PointerScanner Page {searchParams.get("searchedAddress")}</div>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent
          className="focus-visible:outline-none sm:max-w-[625px]"
          onInteractOutside={(e) => e.preventDefault()}
        >
          <DialogHeader>
            <DialogTitle>Pointer scanner options</DialogTitle>
            <DialogDescription></DialogDescription>
          </DialogHeader>
          <Form {...form}>
            <form className="grid grid-cols-3 gap-2">
              <FormField
                control={form.control}
                name="scanAddress"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Scan Address:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="maxOffset"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Max Offset Value:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="maxLevel"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Max Level:</FormLabel>
                    <FormControl>
                      <Input {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
            </form>
          </Form>
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
        </DialogContent>
      </Dialog>
    </>
  );
}
