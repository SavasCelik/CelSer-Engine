let contextMenuById = new Map();
let isContextMenuOpen = false;
const appContainer = document.querySelector("#app");

export function init(selector, ctxMenuId) {
    const contextMenu = document.getElementById(ctxMenuId);
    contextMenuById.set(ctxMenuId, contextMenu);

    document.addEventListener("contextmenu", (event) => {
        const selectedRow = event.target.closest(selector);

        if (!selectedRow) {
            return;
        }

        const appWidth = appContainer.clientWidth - 10;
        const appHeight = appContainer.clientHeight - 10;
        event.preventDefault();
        hideContextMenu();
        contextMenu.style.display = "block";

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
        isContextMenuOpen = true;
    });

    if (contextMenuById.size === 1) {
        document.addEventListener("click", () => {
            hideContextMenu();
        });
    }
}

function hideContextMenu() {
    if (!isContextMenuOpen) {
        return;
    }

    for (let [, contextMenu] of contextMenuById) {
        contextMenu.style.display = "none";
    }

    isContextMenuOpen = false;
}