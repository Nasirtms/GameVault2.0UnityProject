mergeInto(LibraryManager.library, {
    IsMobileBrowser: function () {
        var ua = navigator.userAgent || navigator.vendor || window.opera;

        var isMobileUA = /android|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(ua);

        var hasTouch = ('ontouchstart' in window) || navigator.maxTouchPoints > 0;

        return (isMobileUA || hasTouch) ? 1 : 0;
    }
});