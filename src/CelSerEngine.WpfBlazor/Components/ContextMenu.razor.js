let contextMenuById = new Map();
const appWidth = document.querySelector("#app").clientWidth - 10;
const appHeight = document.querySelector("#app").clientHeight - 10;
let isContextMenuOpen = false;

export function init(selector, ctxMenuId) {
    const contextMenu = document.getElementById(ctxMenuId);
    contextMenuById.set(ctxMenuId, contextMenu);

    document.addEventListener("contextmenu", (event) => {
        const selectedRow = event.target.closest(selector);

        if (!selectedRow) {
            return;
        }

        event.preventDefault();
        hideContextMenu();

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