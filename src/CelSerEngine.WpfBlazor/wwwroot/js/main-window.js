let gridApi;

function ready() {
    // Grid Options: Contains all of the grid configurations
    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            sortable: false,
            flex: 1
        },
        rowSelection: 'single',
        pagination: true,
        paginationPageSizeSelector: false,
        paginationPageSize: 500,
        getRowId: (params) => params.data.Address,
        // Row Data: The data to be displayed.
        //rowData: JSON.parse(data),
        // Column Definitions: Defines the columns to be displayed.
        columnDefs: [
            {
                field: "Address",
                headerName: "Address"
            },
            {
                field: "Value",
                headerName: "Value"
            }
        ]
    };

    const myGridElement = document.querySelector('#scanResultsGrid');
    gridApi = agGrid.createGrid(myGridElement, gridOptions);
}

function applyData(data) {
    gridApi.setGridOption("rowData", JSON.parse(data));
}

export { ready, applyData }