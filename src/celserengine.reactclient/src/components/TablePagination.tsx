import { cn } from "@/lib/utils";
import {
  Pagination,
  PaginationContent,
  PaginationItem,
  PaginationNext,
  PaginationPrevious,
} from "./ui/pagination";
import { Input } from "./ui/input";
import { RowData, Table } from "@tanstack/react-table";

export default function TablePagination<TData extends RowData>({
  table,
}: {
  table: Table<TData>;
}) {
  return (
    <Pagination>
      <PaginationContent>
        <PaginationItem>
          <PaginationPrevious
            onClick={() => table.previousPage()}
            className={cn({
              "pointer-events-none cursor-not-allowed text-gray-500":
                !table.getCanPreviousPage(),
            })}
          />
        </PaginationItem>
        <PaginationItem>
          <Input
            className="size-8 p-0 text-center"
            value={table.getState().pagination.pageIndex}
            onChange={(val) => {
              let pageIndexDesired = Number(val.target.value) || 0;
              const pageCount = table.getPageCount();
              if (pageIndexDesired > pageCount - 1) {
                pageIndexDesired = pageCount - 1;
              } else if (pageIndexDesired < 0) {
                pageIndexDesired = 0;
              }

              table.setPageIndex(pageIndexDesired);
            }}
          />
        </PaginationItem>
        <PaginationItem>
          <PaginationNext
            onClick={() => table.nextPage()}
            className={cn({
              "pointer-events-none cursor-not-allowed text-gray-500":
                !table.getCanNextPage(),
            })}
          />
        </PaginationItem>
      </PaginationContent>
    </Pagination>
  );
}
