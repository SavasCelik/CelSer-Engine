let gridApi;
let dotNetHelper;
let verticalViewport;
const itemHeight = 25;
const maxDivHeight = 32000000;
let totalItemCount = 0;

function initVirtualizedAgGrid(_dotNetHelper) {
    dotNetHelper = _dotNetHelper;

    const loadingOverlay = class CustomLoadingOverlay {
        init(params) {
            this.eGui = document.createElement('div');

            if (params.isScanning) {
                this.eGui.innerHTML =
                    `<div class="ag-overlay-loading-center">
                    <div class="spinner-border m-auto" role="status"></div>
                </div>`;
            }
            else {
                this.eGui.innerHTML = ``;
            }
        }

        getGui() {
            return this.eGui;
        }

        refresh(params) {
            return false;
        }
    };

    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            sortable: false,
            flex: 1
        },
        rowSelection: 'multiple',
        animateRows: false,
        loadingOverlayComponent: loadingOverlay,
        loadingOverlayComponentParams: {
            isScanning: false,
        },
        getRowId: (params) => params.data.Item.Address,
        // Column Definitions: Defines the columns to be displayed.
        columnDefs: [
            {
                field: "Item.Address",
                headerName: "Address"
            },
            {
                field: "Item.Value",
                headerName: "Value"
            },
            {
                field: "Item.PreviousValue",
                headerName: "Previous"
            }
        ],
        onModelUpdated: (params) => {
            const nodesToSelect = [];
            params.api.forEachNode((node) => {
                if (node.data && node.data.IsSelected) {
                    nodesToSelect.push(node);
                }
            });
            params.api.setNodesSelected({ nodes: nodesToSelect, newValue: true });
        },
        getRowStyle: params => {
            if (params.node.data && params.node.data.Item.PreviousValue != params.node.data.Item.Value) {
                return { color: 'red' };
            }
            else {
                return { color: 'inherit' };
            }
        },
        onRowDoubleClicked: onRowDoubleClicked
    };

    const myGridElement = document.querySelector('#scanResultsGrid');
    gridApi = agGrid.createGrid(myGridElement, gridOptions);

    myGridElement.dataset.previousHeight = myGridElement.clientHeight;
    new ResizeObserver(onResize).observe(myGridElement);
    const fakeVscroll = document.querySelector(".ag-body-vertical-scroll");
    const clone = fakeVscroll.cloneNode(true);
    fakeVscroll.replaceWith(clone);
    clone.classList.remove("ag-hidden");
    verticalViewport = document.querySelector(".ag-body-vertical-scroll-viewport");
    handleEvents();
}

function handleEvents() {
    verticalViewport.addEventListener("scroll", async () => await onScroll());
    document.querySelector(".ag-body-vertical-scroll-container").style.height = `${Math.min(totalItemCount * itemHeight, maxDivHeight)}px`;
    document.querySelector(".ag-body-viewport").addEventListener("wheel", function (e) {
        if (Math.abs(e.deltaY) > 0) {
            const deltaSign = Math.sign(e.deltaY);
            verticalViewport.scrollBy(0, itemHeight * deltaSign);
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
                gridApi.forEachNode(async (node) => node.selectThisNode(await dotNetHelper.invokeMethodAsync("IsItemSelectedAsync", node.data.Item.Address)));
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

async function onResize(entry) {
    if (totalItemCount == 0)
        return;

    const currentHeight = entry[0].target.clientHeight;
    const previousHeight = entry[0].target.dataset.previousHeight || 0;

    if (currentHeight > previousHeight) {
        if (currentHeight - previousHeight >= itemHeight) {
            entry[0].target.dataset.previousHeight = currentHeight;
            const visibleRowCount = getVisibleRowCount();
            await loadItemsIntoGrid(getStartIndex(visibleRowCount), visibleRowCount);
        }
    } else if (currentHeight < previousHeight) {
        if (previousHeight - currentHeight >= itemHeight) {
            entry[0].target.dataset.previousHeight = currentHeight;
        }
    }
}

async function onRowDoubleClicked(row) {
    await dotNetHelper.invokeMethodAsync('OnRowDoubleClickedDispatcher', row.data.Item.Address);
}

function showScanningOverlay() {
    gridApi.setGridOption("loadingOverlayComponentParams", { isScanning: true });
    gridApi.showLoadingOverlay();
}

async function itemsChanged(totalCount) {
    totalItemCount = totalCount;
    const visibleRowCount = getVisibleRowCount();
    let startIndex = getStartIndex(visibleRowCount);
    document.querySelector(".ag-body-vertical-scroll-container").style.height = `${Math.min(totalItemCount * itemHeight, maxDivHeight)}px`;
    await loadItemsIntoGrid(startIndex, visibleRowCount);
}

async function loadItemsIntoGrid(startIndex, rowCount) {
    const visibleItems = await dotNetHelper.invokeMethodAsync('GetItemsAsync', startIndex, rowCount)
        .then(x => JSON.parse(x));
    gridApi.setGridOption("rowData", visibleItems);

    return visibleItems;
}

function getStartIndex(visibleRowCount) {
    let startIndex = -1;

    if (verticalViewport.clientHeight + verticalViewport.scrollTop == maxDivHeight) {
        startIndex = totalItemCount - visibleRowCount;
    }
    else if ((totalItemCount * itemHeight) >= maxDivHeight) {
        startIndex = Math.floor(((totalItemCount * itemHeight) / maxDivHeight) * verticalViewport.scrollTop / itemHeight);
    }
    else {
        startIndex = Math.floor(verticalViewport.scrollTop / itemHeight);
    }

    return startIndex;
}

function getVisibleRowCount() {
    return Math.ceil(verticalViewport.clientHeight / itemHeight);
}

var lastStartIndex = -1;
async function onScroll() {
    const visibleRowCount = getVisibleRowCount();
    let startIndex = getStartIndex(visibleRowCount);

    if (lastStartIndex == startIndex) {
        return;
    }

    lastStartIndex = startIndex;
    const visibleItems = await loadItemsIntoGrid(startIndex, visibleRowCount);
}

export { initVirtualizedAgGrid, itemsChanged, showScanningOverlay }