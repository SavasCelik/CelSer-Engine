let tackedItemsGridApi;

export function initTackedItems() {
    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            flex: 1
        },
        rowSelection: 'multiple',
        overlayLoadingTemplate: '<div></div>',
        columnDefs: [
            {
                field: "IsFrozen",
                headerName: "Freeze",
                cellRenderer: (params) => `
                    <div class="form-check form-switch fs-5">
                        <input class="form-check-input" type="checkbox">
                    </div>`,
            },
            {
                field: "Description",
                headerName: "Description",
            },
            {
                field: "Address",
                headerName: "Address",
            },
            {
                field: "Value",
                headerName: "Value",
            },
        ]
    };

    const trackedItemsGridElement = document.querySelector('#trackedItemsGrid');
    tackedItemsGridApi = agGrid.createGrid(trackedItemsGridElement, gridOptions);
}

export function applyTrackedItems(items) {
    tackedItemsGridApi.setGridOption("rowData", JSON.parse(items));
}