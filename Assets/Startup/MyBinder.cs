using System;
using System.Collections.Generic;
using Bossy;

public class MyBinder : IBossyBinder
{
    private Dictionary<Type, object> _bindings = new();
    
    public void RegisterSingleton<T>(T instance)
    {
        _bindings[typeof(T)] = instance;
    }
    
    public bool TryGet(Type requestedType, out object obj)
    {
        return _bindings.TryGetValue(requestedType, out obj);
    }
}
