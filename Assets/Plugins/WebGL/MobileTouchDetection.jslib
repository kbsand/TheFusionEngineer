mergeInto(LibraryManager.library, {
  TFE_IsTouchDevice: function () {
    return (('ontouchstart' in window) ||
            (navigator.maxTouchPoints && navigator.maxTouchPoints > 0)) ? 1 : 0;
  }
});
