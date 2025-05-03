import {
  TableFeature,
  Table,
  OnChangeFn,
  RowData,
  Row,
  makeStateUpdater,
  memo,
  getMemoOptions,
  RowModel,
} from "@tanstack/react-table";

export type FrozenRowsState = Record<string, string>;

export interface FrozenRowsTableState {
  frozenRows: FrozenRowsState;
}

export interface FrozenRowsOptions<TData extends RowData> {
  enableRowFreezing?: boolean;
  onFrozenRowsChange?: OnChangeFn<FrozenRowsState>;
  getRowFreezeValue?: (row: TData) => string;
}

export interface RowFrozenRow {
  getIsFrozen: () => boolean;
  toggleFrozen: () => void;
  freezeValue: string;
}

export interface FrozenRowsInstance<TData extends RowData> {
  getFrozenRowModel: () => RowModel<TData>;
}

// Extend the TableState to include frozenRows
declare module "@tanstack/react-table" {
  interface Table<TData extends RowData> extends FrozenRowsInstance<TData> {}

  interface TableState extends FrozenRowsTableState {}

  interface TableOptionsResolved<TData extends RowData>
    extends FrozenRowsOptions<TData> {}

  interface Row<TData extends RowData> extends RowFrozenRow {}
}

// The feature implementation
export const FrozenRowsFeature: TableFeature<any> = {
  getInitialState: (state): FrozenRowsTableState => {
    return {
      frozenRows: {},
      ...state,
    };
  },

  getDefaultOptions: <TData extends RowData>(
    table: Table<TData>
  ): FrozenRowsOptions<TData> => {
    return {
      enableRowFreezing: false,
      onFrozenRowsChange: makeStateUpdater("frozenRows", table),
      getRowFreezeValue: () => "",
    };
  },

  createTable: <TData extends RowData>(table: Table<TData>): void => {
    table.getFrozenRowModel = memo(
      () => [table.getState().frozenRows, table.getCoreRowModel()],
      (rowSelection, rowModel) => {
        if (!Object.keys(rowSelection).length) {
          return {
            rows: [],
            flatRows: [],
            rowsById: {},
          };
        }

        return frozenRowsFn(table, rowModel);
      },
      getMemoOptions(table.options, "debugTable", "getFrozenRowModel")
    );
  },

  createRow: <TData extends RowData>(
    row: Row<TData>,
    table: Table<TData>
  ): void => {
    // here we extend the row with the methods defined in the RowFrozenRow interface

    row.getIsFrozen = () => {
      const { frozenRows } = table.getState();
      return frozenRows[row.id] != undefined;
    };

    row.toggleFrozen = () => {
      table.options.onFrozenRowsChange?.((prev) => {
        const newFrozenRows = { ...prev };
        if (row.getIsFrozen()) {
          delete newFrozenRows[row.id];
        } else {
          newFrozenRows[row.id] = row.freezeValue;
        }
        return newFrozenRows;
      });
    };

    row.freezeValue = table.options.getRowFreezeValue
      ? table.options.getRowFreezeValue(row.original)
      : "";
  },
};

export function frozenRowsFn<TData extends RowData>(
  table: Table<TData>,
  rowModel: RowModel<TData>
): RowModel<TData> {
  const frozenRows = table.getState().frozenRows;

  const newFrozenFlatRows: Row<TData>[] = [];
  const newFrozenRowsById: Record<string, Row<TData>> = {};

  // Filters top level and nested rows
  const recurseRows = (rows: Row<TData>[] /*, depth = 0*/): Row<TData>[] => {
    return rows
      .map((row) => {
        const isFrozen = frozenRows[row.id] || false;

        if (isFrozen) {
          newFrozenFlatRows.push(row);
          newFrozenRowsById[row.id] = row;
        }

        // if (row.subRows?.length) {
        //   row = {
        //     ...row,
        //     subRows: recurseRows(row.subRows, depth + 1),
        //   }
        // }

        if (isFrozen) {
          return row;
        }
      })
      .filter(Boolean) as Row<TData>[];
  };

  return {
    rows: recurseRows(rowModel.rows),
    flatRows: newFrozenFlatRows,
    rowsById: newFrozenRowsById,
  };
}
