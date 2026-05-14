mergeInto(LibraryManager.library, {
  ReloadPageJS: function () {
    try {
      // Most compatible across browsers
      window.location.reload();
    } catch (e) {
      // Ultimate fallback
      window.location.href = window.location.href;
    }
  }
});