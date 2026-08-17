window.atlasKeyboard = {
    init: function (dotNetHelper) {
        document.addEventListener('keydown', function (e) {
            if (e.ctrlKey && e.key === ' ') {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('HandleShortcut', 'ctrl+space');
            } else if (e.ctrlKey && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('HandleShortcut', 'ctrl+k');
            } else if (e.ctrlKey && (e.key === 'n' || e.key === 'N')) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('HandleShortcut', 'ctrl+n');
            } else if (e.altKey && e.key >= '1' && e.key <= '8') {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('HandleShortcut', 'alt+' + e.key);
            } else if (e.altKey && (e.key === 'd' || e.key === 'D')) {
                e.preventDefault();
                dotNetHelper.invokeMethodAsync('HandleShortcut', 'alt+d');
            }
        });
    }
};
