let modulesSelect;

export function initIndex() {
    new SlimSelect({
        select: '#selectScanDataType',
        settings: {
            showSearch: false,
        }
    });
    new SlimSelect({
        select: '#scanCompareType',
        settings: {
            showSearch: false,
        }
    });

    new SlimSelect({
        select: '#writable',
        settings: {
            showSearch: false,
        }
    });

    new SlimSelect({
        select: '#executable',
        settings: {
            showSearch: false,
        }
    });

    new SlimSelect({
        select: '#copyOnWrite',
        settings: {
            showSearch: false,
        }
    });

    new SlimSelect({
        select: '#memoryTypes',
        settings: {
            showSearch: false,
        }
    });

    initModulesSelect();

    Split(['#split-0', '#split-1'], {
        direction: 'vertical',
        snapOffset: 0,
        minSize: [200, 100],
        gutterSize: 1,
        elementStyle: function (dimension, size, gutterSize) {
            return {
                'flex-basis': 'calc(' + size + '% - ' + gutterSize + 'px)',
            }
        },
        gutterStyle: function (dimension, gutterSize) {
            return {
                'flex-basis': gutterSize + 'px',
                'min-height': gutterSize + 'px',
            }
        },
    });
}

export function focusFirstInvalidInput() {
    setTimeout(() => document.querySelector(".invalid")?.focus(), 100);
}

export function focusSearchValueInput() {
    document.querySelector("#value-text-field").focus();
}

export function applyData(data) {
    var x = document.getElementById("modulesSelect");

    while (x.length > 0) {
        x.remove(0);
    }

    for (var i = 0; i < data.length; i++) {
        var option = document.createElement("option");
        option.text = data[i];
        option.value = data[i];
        x.add(option);
    }
    destroyModulesSelect();
    initModulesSelect();
}

function destroyModulesSelect() {
    modulesSelect.destroy();
    //document.querySelectorAll("#modulesSelect option").forEach(x => x.removeAttribute("id"));
}

function initModulesSelect() {
    modulesSelect = new SlimSelect({
        select: '#modulesSelect',
        settings: {
            //showSearch: false,
        }
    });
}