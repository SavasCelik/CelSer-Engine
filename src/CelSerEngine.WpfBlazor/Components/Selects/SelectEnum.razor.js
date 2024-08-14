let slimSelectElement;
let selectElement;
let selectId;

export function initSelectEnum(id, data) {
    selectId = id;
    selectElement = document.getElementById(selectId);
    updateSelect(data);
}

export function updateSelect(data) {
    selectElement.innerHTML = "";

    for (let i = 0; i < data.length; i++) {
        const option = document.createElement("option");
        option.text = data[i].name;
        option.value = data[i].id;
        selectElement.add(option);
    }

    if (slimSelectElement) {
        slimSelectElement.destroy();
    }
    slimSelectElement = new SlimSelect({
        select: '#' + selectId,
        settings: {
            showSearch: false,
        }
    });
    const event = new Event('change');
    selectElement.dispatchEvent(event);
}

export function setSelection(selection) {
    selectElement.value = selection;
    const event = new Event('change');
    selectElement.dispatchEvent(event);
}