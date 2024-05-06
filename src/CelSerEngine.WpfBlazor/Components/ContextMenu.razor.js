const contextMenu = document.querySelector("#cmenu");
const appWidth = document.querySelector("#app").clientWidth - 10;
const appHeight = document.querySelector("#app").clientHeight - 10;

export function init(selector) {
    document.addEventListener("contextmenu", (event) => {
        const selectedRow = event.target.closest(selector);

        if (!selectedRow) {
            return;
        }

        event.preventDefault();

        if (event.pageX + contextMenu.clientWidth > appWidth) {
            contextMenu.style.left = appWidth - contextMenu.clientWidth + "px";
        }
        else {
            contextMenu.style.left = event.pageX + "px";
        }

        if (event.pageY + contextMenu.clientHeight > appHeight) {
            contextMenu.style.top = appHeight - contextMenu.clientHeight + "px";
        }
        else {
            contextMenu.style.top = event.pageY + "px";
        }

        contextMenu.style.display = "block";
    });

    document.addEventListener("click", () => {
        hideContextMenu();
    });
}

function hideContextMenu() {
    contextMenu.style.display = "none";
}