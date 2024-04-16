let gridApi;
let dotNetHelper;

function initVirtualizedAgGrid(_dotNetHelper) {
    dotNetHelper = _dotNetHelper;

    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            sortable: false,
            flex: 1
        },
        rowSelection: 'multiple',
        animateRows: false,
        getRowId: (params) => params.data.Address,
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
    handleEvents();
}

function handleEvents() {
    const fakeVscroll = document.querySelector(".ag-body-vertical-scroll");
    const clone = fakeVscroll.cloneNode(true);
    fakeVscroll.replaceWith(clone);
    clone.classList.remove("ag-hidden");
    document.querySelector(".ag-body-vertical-scroll-viewport").addEventListener("scroll", async () => await onScroll());
    document.querySelector(".ag-body-vertical-scroll-container").style.height = `${Math.min(totalItemCount * itemHeight, maxDivHeight)}px`;
    document.querySelector(".ag-body-viewport").addEventListener("wheel", function (e) {
        if (Math.abs(e.deltaY) > 0) {
            const deltaSign = Math.sign(e.deltaY);
            document.querySelector(".ag-body-vertical-scroll-viewport").scrollBy(0, itemHeight * deltaSign);
            this.scrollTop = (this.scrollHeight - this.clientHeight) * deltaSign;
            e.preventDefault();
        }
    });

    document.querySelector(".ag-center-cols-container").addEventListener("click", async function (e) {
        const clickedRow = e.target.parentElement;
        if (clickedRow.classList.contains("ag-row")) {
            const rowId = clickedRow.getAttribute("row-id");

            if (!e.shiftKey && !e.ctrlKey) {
                await dotNetHelper.invokeMethodAsync("ClearSelectedItems", rowId);
            }

            if (e.shiftKey) {
                e.preventDefault();
                await dotNetHelper.invokeMethodAsync("SelectTillItemAsync", rowId);
                gridApi.forEachNode(async (node) => node.selectThisNode(await dotNetHelper.invokeMethodAsync("IsItemSelectedAsync", node.data.Address)));
                return;
            }

            const rowIsSelected = gridApi.getRowNode(rowId).isSelected();
            if (rowIsSelected) {
                dotNetHelper.invokeMethodAsync("AddSelectedItemAsync", rowId);
            }
            else {
                dotNetHelper.invokeMethodAsync("RemoveSelectedItemAsync", rowId);
            }
        }
    });
}

const itemHeight = 25;
const maxDivHeight = 32000000;
let totalItemCount = 0;

function applyData(visibleItems, totalCount) {
    totalItemCount = totalCount;
    document.querySelector(".ag-body-vertical-scroll-container").style.height = `${Math.min(totalItemCount * itemHeight, maxDivHeight)}px`;
    gridApi.setGridOption("rowData", JSON.parse(visibleItems));
}

var lastStartIndex = -1;
async function onScroll() {
    let startIndex = lastStartIndex;
    const myGrid = document.querySelector(".ag-body-vertical-scroll-viewport");

    if (myGrid.clientHeight + myGrid.scrollTop == maxDivHeight) {
        startIndex = totalItemCount - Math.ceil(myGrid.clientHeight / itemHeight);
    }
    else if ((totalItemCount * itemHeight) >= maxDivHeight) {
        startIndex = Math.floor(((totalItemCount * itemHeight) / maxDivHeight) * myGrid.scrollTop / itemHeight);
    }
    else {
        startIndex = Math.floor(myGrid.scrollTop / itemHeight);
    }

    if (lastStartIndex == startIndex) {
        return;
    }

    lastStartIndex = startIndex;
    const endIndex = myGrid.clientHeight / itemHeight;
    const visibleItems = await dotNetHelper.invokeMethodAsync('GetItemsAsync', startIndex, Math.ceil(endIndex))
        .then(x => JSON.parse(x));
    gridApi.setGridOption("rowData", visibleItems);

    for (var i = 0; i < visibleItems.length; i++) {
        var isSelected = await dotNetHelper.invokeMethodAsync("IsItemSelectedAsync", visibleItems[i].Address.toString());
        if (isSelected) {
            gridApi.getDisplayedRowAtIndex(i).selectThisNode(true);
        }
    }
}

export { initVirtualizedAgGrid, applyData }