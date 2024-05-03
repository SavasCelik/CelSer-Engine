export function init(selector) {
    document.addEventListener("contextmenu", (event) => {
        event.preventDefault();
        const contextMenu = document.querySelector("#cmenu");
        const appWidth = document.querySelector("#app").clientWidth - 10;
        const appHeight = document.querySelector("#app").clientHeight - 10;

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
}