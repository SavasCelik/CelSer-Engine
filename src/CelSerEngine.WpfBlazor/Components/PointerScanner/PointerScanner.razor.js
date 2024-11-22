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

export function applyPointerScannerResults(pointerScanResult) {
    const newColDefs = [
        {
            field: "baseAddress",
            headerName: "Base Address",
            sortable: false
        }
    ];

    // create offset cols
    for (var i = 0; i < pointerScanResult.maxLevel; i++) {
        const captureIndex = i; // this could be done using the p parameter in the valueGetter, but this is way easier.
        newColDefs.push({
            valueGetter: p => p.data.offsetArray[captureIndex],
            headerName: `Offset ${i}`,
            sortable: false
        });
    }

    newColDefs.push({
        field: "pointsTo",
        headerName: "Points To",
        sortable: false
    });

    pointerScannerResultsGridApi.setGridOption("columnDefs", newColDefs);
    pointerScannerResultsGridApi.setGridOption("rowData", pointerScanResult.pointers);
}