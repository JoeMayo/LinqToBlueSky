﻿/***********************************************************
 * Credits:
 * 
 * MSDN Documentation -
 * Walkthrough: Creating an IQueryable LINQ Provider
 * 
 * http://msdn.microsoft.com/en-us/library/bb546158.aspx
 * 
 * Matt Warren's Blog -
 * LINQ: Building an IQueryable Provider:
 * 
 * http://blogs.msdn.com/mattwar/default.aspx
 * *********************************************************/

using System.Reflection;

namespace LinqToBlueSky;

internal static class TypeSystem
{
    internal static Type? GetElementType(Type seqType)
    {
        Type? ienum = FindIEnumerable(seqType);
        if (ienum == null) return seqType;
        return ienum.GenericTypeArguments[0];
    }

    private static Type? FindIEnumerable(Type seqType)
    {
        TypeInfo seqTypeInfo = seqType.GetTypeInfo();
        if (seqType == null || seqType == typeof(string))
            return null;

        if (seqTypeInfo.IsArray)
        {
            Type? elementType = seqTypeInfo.GetElementType();
            if (elementType != null)
                return typeof(IEnumerable<>).MakeGenericType(elementType);
        }

        if (seqTypeInfo.IsGenericType)
        {
            foreach (Type arg in seqTypeInfo.GenericTypeArguments)
            {
                Type? ienum = typeof(IEnumerable<>).MakeGenericType(arg);

                if (ienum != null && ienum.GetTypeInfo().IsAssignableFrom(seqTypeInfo))
                    return ienum;
            }
        }

        Type[] ifaces = seqTypeInfo.ImplementedInterfaces.ToArray();
        if (ifaces != null && ifaces.Length > 0)
        {
            foreach (Type iface in ifaces)
            {
                Type? ienum = FindIEnumerable(iface);
                if (ienum != null) return ienum;
            }
        }

        if (seqTypeInfo.BaseType != null && seqTypeInfo.BaseType != typeof(object))
            return FindIEnumerable(seqTypeInfo.BaseType);

        return null;
    }
}
