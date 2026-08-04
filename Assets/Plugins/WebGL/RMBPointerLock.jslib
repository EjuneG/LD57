// Locks the pointer while the right mouse button is held, so drag deltas keep
// flowing past the browser window edge (matching desktop-build behavior).
//
// Browsers only allow requestPointerLock() synchronously inside a genuine
// user-input event handler. Setting Cursor.lockState from C# runs inside
// requestAnimationFrame, so the engine defers the request until the NEXT
// click - useless for a hold-to-drag gesture. Hooking the real DOM mousedown
// is the only reliable way. Unity syncs its internal lock state automatically
// via the pointerlockchange event.
mergeInto(LibraryManager.library, {
  RMBPointerLock_SetEnabled: function (enabled) {
    if (!Module.__rmbPointerLock) {
      var state = { enabled: false };
      Module.__rmbPointerLock = state;
      document.addEventListener("mousedown", function (e) {
        var canvas = Module["canvas"];
        if (!state.enabled || e.button !== 2 || !canvas || e.target !== canvas) return;
        if (document.pointerLockElement === canvas || !canvas.requestPointerLock) return;
        try {
          var p = canvas.requestPointerLock();
          if (p && p.catch) p.catch(function () {});
        } catch (err) {}
      }, true);
      document.addEventListener("mouseup", function (e) {
        if (e.button !== 2) return;
        var canvas = Module["canvas"];
        if (canvas && document.pointerLockElement === canvas && document.exitPointerLock) {
          document.exitPointerLock();
        }
      }, true);
    }
    Module.__rmbPointerLock.enabled = !!enabled;
  }
});
