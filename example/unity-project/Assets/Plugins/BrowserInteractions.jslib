mergeInto(LibraryManager.library, {
  SendSelectedObject: function (objectId, size) {
    if (window.sendSelectedObjectFromUnity) {
      window.sendSelectedObjectFromUnity(UTF8ToString(objectId));
    }
  },
  
  SendSceneReady: function () {
    if (window.sendSceneReadyFromUnity) {
      window.sendSceneReadyFromUnity();
    }
  },
  
  SendObjectsList: function (objectIds, size) {
    if (window.sendObjectsListFromUnity) {
      window.sendObjectsListFromUnity(UTF8ToString(objectIds));
    }
  },
  
});
