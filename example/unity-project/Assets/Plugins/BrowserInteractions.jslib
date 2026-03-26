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
  
  RequestDataFromWeb: function (queryPtr, callbackPtr) {
    var query = UTF8ToString(queryPtr);
    window.requestDataFromWebFromUnity(query, function (result) {
      var bufferSize = lengthBytesUTF8(result) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(result, buffer, bufferSize);
      {{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);
      _free(buffer);
    });
  },
  
  RegisterOnNavigationChanged: function (callbackPtr) {
    window.registerOnNavigationChangedFromUnity(function (data) {
      var bufferSize = lengthBytesUTF8(data) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(data, buffer, bufferSize);
      {{{ makeDynCall('vi', 'callbackPtr') }}}(buffer);
      _free(buffer);
    });
  },
  
});
