// Unity WebGL plugin: open mobile browser keyboard and send result back to Unity.
// Copy this file into your Unity project: Assets/Plugins/WebGL/MobileKeyboard.jslib

mergeInto(LibraryManager.library, {
  ShowMobileKeypad: function() {
    if (typeof showMobileKeypad === 'function') showMobileKeypad();
  },
  SetMobileKeyboardPlaceholder: function(ptr) {
    window.mobileKeyboardPlaceholder = ptr ? UTF8ToString(ptr) : '';
  },
  SetMobileKeyboardInitialValue: function(ptr) {
    window.mobileKeyboardInitialValue = ptr ? UTF8ToString(ptr) : '';
  }
});
