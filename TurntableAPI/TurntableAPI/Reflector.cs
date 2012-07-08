using System;
using System.Collections.Generic;
using System.Reflection;

namespace Utilities
{
    public static class Reflector
    {
        public static List<Tuple<T1, T2>> FindAllMethods<T1, T2>()
            where T1 : Attribute
            where T2 : class
        {
            if (!typeof(T2).IsSubclassOf(typeof(Delegate))) return null;
            List<Tuple<T1, T2>> results = new List<Tuple<T1, T2>>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (!assembly.GlobalAssemblyCache)
                    results.AddRange(FindAllMethods<T1, T2>(assembly));
            return results;
        }

        public static List<Tuple<T1, T2>> FindAllMethods<T1, T2>(Assembly assembly)
            where T1 : Attribute
            where T2 : class
        {
            if (!typeof(T2).IsSubclassOf(typeof(Delegate))) return null;
            List<Tuple<T1, T2>> results = new List<Tuple<T1, T2>>();
            Type[] types = assembly.GetTypes();
            foreach (Type type in types)
                results.AddRange(FindAllMethods<T1, T2>(type));
            return results;
        }

        public static List<Tuple<T1, T2>> FindAllMethods<T1, T2>(Type type)
            where T1 : Attribute
            where T2 : class
        {
            if (!typeof(T2).IsSubclassOf(typeof(Delegate))) return null;
            List<Tuple<T1, T2>> results = new List<Tuple<T1, T2>>();
            MethodInfo[] methods = type.GetMethods();
            foreach (MethodInfo method in methods)
            {
                T1[] attribute = Attribute.GetCustomAttributes(method, typeof(T1), false) as T1[];
                if (attribute.Length == 0) continue;
                T2 callback = Delegate.CreateDelegate(typeof(T2), method, false) as T2;
                if (callback == null) continue;
                Array.ForEach<T1>(attribute, (a) => results.Add(new Tuple<T1, T2>(a, callback)));
            }
            return results;
        }

        public static List<object> Reflect<T>(T obj)
        {
            List<object> ret = new List<object>();
            Type type = typeof(T);
            FieldInfo[] fields = type.GetFields();
            for(int i = 0; i < fields.Length; ++i)
                ret.Add(fields[i].GetValue(obj));
            return ret;
        }

        public static T Mirror<T>(List<object> Info)
        {
            T obj = default(T);
            Type type = obj.GetType();
            FieldInfo[] fields = type.GetFields();
            for (int i = 0; i < fields.Length; ++i)
                fields[i].SetValue(obj, Info[i]);
            return obj;
        }
    }
}
