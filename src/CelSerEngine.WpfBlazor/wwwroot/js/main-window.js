let gridApi;

function ready() {
    // Grid Options: Contains all of the grid configurations
    const gridOptions = {
        defaultColDef: {
            suppressMovable: true,
            sortable: false,
            flex: 1
        },
        rowSelection: 'multiple',
        animateRows: false,
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

var data = [];
const itemHeight = 25;
const maxDivHeight = 32000000;

function applyData(data2) {
    //gridApi.setGridOption("rowData", JSON.parse(data));
    for (let i = 0; i < 2000000; i++) {
        data[i] = { "Address": i, "Value": i };
    }


    //gridApi.setGridOption("rowData", theData);

    const fakeVscroll = document.querySelector(".ag-body-vertical-scroll");
    const clone = fakeVscroll.cloneNode(true);
    fakeVscroll.replaceWith(clone);
    clone.classList.remove("ag-hidden");
    document.querySelector(".ag-body-vertical-scroll-viewport").addEventListener("scroll", () => onScroll());
    document.querySelector(".ag-body-vertical-scroll-container").style.height = `${Math.min(data.length * itemHeight, maxDivHeight)}px`;
    document.querySelector(".ag-body-viewport").addEventListener("wheel", function (e) {

        if (Math.abs(e.deltaY) > 0) {
            const deltaSign = Math.sign(e.deltaY);
            document.querySelector(".ag-body-vertical-scroll-viewport").scrollBy(0, itemHeight * deltaSign);
            this.scrollTop = (this.scrollHeight - this.clientHeight) * deltaSign;
            e.preventDefault();
        }
    });

    document.body.addEventListener("mousedown", function (e) {
        // Check if the middle mouse button was clicked
        if (e.button === 1) {
            // Handle the middle mouse button click event here
            e.preventDefault();
        }
    });

    document.querySelector(".ag-center-cols-container").addEventListener("click", function (e) {
        const clickedRow = e.target.parentElement;
        if (clickedRow.classList.contains("ag-row")) {

            if (!e.shiftKey && !e.ctrlKey) {
                selectedAddresses.clear();
            }

            const rowId = clickedRow.getAttribute("row-id");
            const lastSelectedRow = Array.from(selectedAddresses).slice(-1)[0];
            if (e.shiftKey && lastSelectedRow) {
                selectBetween(lastSelectedRow, rowId);
                gridApi.deselectAll();
                selectedAddresses.forEach((address) => gridApi.getRowNode(address)?.selectThisNode(true));
                return;
            }

            const rowIsSelected = gridApi.getRowNode(rowId).isSelected();
            if (rowIsSelected) {
                selectedAddresses.add(rowId);
            }
            else {
                selectedAddresses.delete(rowId);
            }
        }
    });


    let theData = [];
    for (let i = 0; i < Math.ceil(document.querySelector(".ag-body-vertical-scroll-viewport").clientHeight / itemHeight); i++) {
        theData[i] = { "Address": i, "Value": i };
    }

    gridApi.setGridOption("rowData", theData);
}

function selectBetween(firstAddress, lastAddress) {
    let isSlecting = false;
    for (let i = 0; i < data.length; i++) {

        if (data[i].Address == firstAddress || data[i].Address == lastAddress) {
            isSlecting = !isSlecting;

            if (!isSlecting) {
                selectedAddresses.add(data[i].Address.toString());
                break;
            }
        }

        if (isSlecting) {
            selectedAddresses.add(data[i].Address.toString());
        }
    }
}

var selectedAddresses = new Set();

var lastStartIndex = -1;
function onScroll() {
    let startIndex = lastStartIndex;
    const myGrid = document.querySelector(".ag-body-vertical-scroll-viewport");

    if (myGrid.clientHeight + myGrid.scrollTop == maxDivHeight) {
        startIndex = data.length - Math.ceil(myGrid.clientHeight / itemHeight) ;
    }
    else if (((data.length) * itemHeight) >= maxDivHeight) {
        startIndex = Math.floor((((data.length) * itemHeight) / maxDivHeight) * myGrid.scrollTop / itemHeight);
    }
    else {
        startIndex = Math.floor(myGrid.scrollTop / itemHeight);
    }

    if (lastStartIndex == startIndex) {
        return;
    }

    const endIndex = myGrid.clientHeight / itemHeight;
    var visibleItems = data.slice(startIndex, startIndex + Math.ceil(endIndex));
    gridApi.setGridOption("rowData", visibleItems);

    for (var i = 0; i < visibleItems.length; i++) {
        var isSelected = selectedAddresses.has(visibleItems[i].Address.toString());
        if (isSelected) {
            gridApi.getDisplayedRowAtIndex(i).selectThisNode(true);
        }
    }

}

export { ready, applyData }