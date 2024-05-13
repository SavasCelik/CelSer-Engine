let tackedItemsGridApi;
let dotNetHelper;

export function initTackedItems(_dotNetHelper) {
    dotNetHelper = _dotNetHelper;

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
                        <input class="form-check-input freezeInput" type="checkbox" ${params.data.IsFrozen ? 'checked' : ''}>
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
        ],
        onCellDoubleClicked: onCellDoubleClicked,
        onCellClicked: onCellClicked,
    };

    const trackedItemsGridElement = document.querySelector('#trackedItemsGrid');
    tackedItemsGridApi = agGrid.createGrid(trackedItemsGridElement, gridOptions);
}

export function applyTrackedItems(items) {
    tackedItemsGridApi.setGridOption("rowData", JSON.parse(items));
}

export function updateTrackedItemValues(items) {
    const allData = JSON.parse(items);

    for (let i = 0; i < allData.length; i++) {
        tackedItemsGridApi.forEachNode(x => {
            if (x.data.Address === allData[i].Address) {
                x.setDataValue("Value", allData[i].Value);
            }
        });
    }
}

async function onCellDoubleClicked(params) {
    await dotNetHelper.invokeMethodAsync("OnCellDoubleClickedAsync", params.rowIndex, params.colDef.field);
}

async function onCellClicked(params) {
    const targetElement = params.event.target;

    if (targetElement.classList.contains("freezeInput")) {
        await dotNetHelper.invokeMethodAsync("UpdateFreezeStateByRowIndex", params.rowIndex, targetElement.checked);
    }
}