﻿using System.Reflection;

namespace Fake.Collections;

public class TypeList<TBaseType> :List<TBaseType>,ITypeList<TBaseType>
{
    private readonly List<Type> _typeList;
    public bool IsReadOnly => false;
    
    /// <summary>
    /// Creates a new <see cref="TypeList{T}"/> object.
    /// </summary>
    public TypeList()
    {
        _typeList = new List<Type>();
    }
    
    public new Type this[int index]
    {
        get => _typeList[index];
        set {
            CheckType(value);
            _typeList[index] = value;
        }
    }
    
    public new IEnumerator<Type> GetEnumerator()
    {
        return _typeList.GetEnumerator();
    }

    public void Add(Type item)
    {
        CheckType(item);
        _typeList.Add(item);
    }

    public bool Contains(Type item)
    {
        return _typeList.Contains(item);
    }

    public void CopyTo(Type[] array, int arrayIndex)
    {
        _typeList.CopyTo(array, arrayIndex);
    }

    public bool Remove(Type item)
    {
        return _typeList.Remove(item);
    }

    public int IndexOf(Type item)
    {
        return _typeList.IndexOf(item);
    }

    public void Insert(int index, Type item)
    {
        CheckType(item);
        _typeList.Insert(index, item);
    }

    public void Add<T>() where T : TBaseType
    {
        _typeList.Add(typeof(T));
    }

    public bool TryAdd<T>() where T : TBaseType
    {
        if (Contains<T>())
        {
            return false;
        }

        Add<T>();
        return true;
    }

    public bool Contains<T>() where T : TBaseType
    {
        return Contains(typeof(T));
    }

    public void Remove<T>() where T : TBaseType
    {
        _typeList.Remove(typeof(T));
    }
    
    private static void CheckType(Type item)
    {
        if (!typeof(TBaseType).GetTypeInfo().IsAssignableFrom(item))
        {
            throw new ArgumentException($"给定类型 ({item.AssemblyQualifiedName}) 应该是 {typeof(TBaseType).AssemblyQualifiedName} 的实例", nameof(item));
        }
    }
}