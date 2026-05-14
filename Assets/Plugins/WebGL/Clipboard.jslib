mergeInto(LibraryManager.library, {
    CopyToClipboard: function (textPtr) {
        var text = UTF8ToString(textPtr);

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(text);
        } else {
            var textarea = document.createElement("textarea");
            textarea.value = text;
            document.body.appendChild(textarea);
            textarea.select();
            document.execCommand("copy");
            document.body.removeChild(textarea);
        }
    },

    PasteFromClipboard: function (gameObjectPtr, methodNamePtr) {
        var gameObject = UTF8ToString(gameObjectPtr);
        var methodName = UTF8ToString(methodNamePtr);

        if (navigator.clipboard && navigator.clipboard.readText) {
            navigator.clipboard.readText().then(function (text) {
                SendMessage(gameObject, methodName, text);
            }).catch(function () {
                SendMessage(gameObject, methodName, "");
            });
        } else {
            SendMessage(gameObject, methodName, "");
        }
    }
});