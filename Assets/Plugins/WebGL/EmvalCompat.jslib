// Shim de compatibilidad para libAppUITextMateLib.a (com.unity.dt.app-ui).
// La librería se compiló con un embind antiguo que importa _emval_take_value y
// _emval_as, que ya no existen en el Emscripten 4.x de Unity 6000.6. Se
// reimplementan aquí con la semántica original sobre los helpers actuales.
mergeInto(LibraryManager.library, {
  _emval_take_value__deps: ['$Emval', '$requireRegisteredType'],
  _emval_take_value: function (type, arg) {
    type = requireRegisteredType(type, '_emval_take_value');
    var v = type['readValueFromPointer'](arg);
    return Emval.toHandle(v);
  },

  _emval_as__deps: ['$Emval', '$requireRegisteredType'],
  _emval_as: function (handle, returnType, destructorsRef) {
    handle = Emval.toValue(handle);
    returnType = requireRegisteredType(returnType, 'emval::as');
    var destructors = [];
    var result = returnType['toWireType'](destructors, handle);
    if (destructors.length) {
      HEAPU32[destructorsRef >> 2] = Emval.toHandle(destructors);
    }
    return result;
  },
});
