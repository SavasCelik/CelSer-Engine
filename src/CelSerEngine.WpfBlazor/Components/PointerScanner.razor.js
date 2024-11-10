let pointerScannerResultsGridApi;

export function initPointerScanner() {
    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            flex: 1
        },
        rowSelection: 'single',
        // Column Definitions: Defines the columns to be displayed.
        columnDefs: [
            {
                field: "baseAddress",
                headerName: "Base Address",
                sortable: false,
                resizable: false,
            }
        ],
        overlayLoadingTemplate: `<div class="spinner-border m-auto" role="status"></div>`,
    };
    
    pointerScannerResultsGridApi = agGrid.createGrid(document.querySelector('#pointerScannerResultsGrid'), gridOptions);
}

export function applyPointerScannerResults(items) {
    pointerScannerResultsGridApi.setGridOption("rowData", items);
}