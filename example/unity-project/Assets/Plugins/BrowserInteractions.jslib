mergeInto(LibraryManager.library, {
  SendSelectedObject: function (objectId, size) {
    window.sendSelectedObjectFromUnity(UTF8ToString(objectId));
  },
  
  SendSceneReady: function () {
    window.sendSceneReadyFromUnity();
  },
  
  SendObjectsList: function (objectIds, size) {
    window.sendObjectsListFromUnity(UTF8ToString(objectIds));
  },
  
});
