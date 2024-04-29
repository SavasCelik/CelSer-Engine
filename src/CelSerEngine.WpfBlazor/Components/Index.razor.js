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

export function focusFirstInvalidInput() {
    setTimeout(() => document.querySelector(".invalid")?.focus(), 100);
}