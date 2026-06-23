using System;
using System.Reflection;
using NUnit.Framework;

namespace ApexShift.Tests.Editor
{
    internal static class EditorTestReflection
    {
        public static Type GetTypeByName(string typeName)
        {
            Type type = Type.GetType(typeName);
            Assert.IsNotNull(type, "Could not resolve " + typeName + ".");
            return type;
        }

        public static MethodInfo GetStaticMethod(Type type, string methodName, BindingFlags flags)
        {
            MethodInfo method = type.GetMethod(methodName, flags);
            Assert.IsNotNull(method, "Could not resolve " + methodName + ".");
            return method;
        }
    }
}
