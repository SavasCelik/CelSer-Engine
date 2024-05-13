let gridApi;
let filterTextField;
let dotNetHelper;

function ready(data, _dotNetHelper) {
    dotNetHelper = _dotNetHelper;
    filterTextField = document.querySelector("#filter-text-field");
    filterTextField.addEventListener("input", onFilterTextFieldChanged)
    filterTextField.focus();

    // Grid Options: Contains all of the grid configurations
    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            flex: 1
        },
        rowSelection: 'single',
        // Row Data: The data to be displayed.
        rowData: JSON.parse(data),
        // Column Definitions: Defines the columns to be displayed.
        columnDefs: [
            {
                field: "Name",
                headerName: "Process",
                sortable: false,
                resizable: false,
                getQuickFilterText: (params) => params.data.Name,
                cellRenderer: (params) => `<img height="20" width="20" src="data:image/png;base64,${params.data.IconBase64Source}" /> <span>${params.data.Name}</span>`
            }
        ],
        onRowDoubleClicked: selectProcess
    };

    const myGridElement = document.querySelector('#myGrid');
    gridApi = agGrid.createGrid(myGridElement, gridOptions);
}

async function selectProcess(row) {
    await dotNetHelper.invokeMethodAsync("SetSelectedProcessById", row.data.Id)
}

function onFilterTextFieldChanged() {
    gridApi.setGridOption(
        "quickFilterText",
        filterTextField.value,
    );
}

export { ready }