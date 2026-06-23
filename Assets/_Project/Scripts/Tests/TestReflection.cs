using System;
using System.Reflection;
using NUnit.Framework;

namespace ApexShift.Tests
{
    internal static class TestReflection
    {
        public static MethodInfo GetInstanceMethod(Type type, string methodName)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "Could not resolve " + methodName + ".");
            return method;
        }

        public static FieldInfo GetInstanceField(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Could not resolve " + fieldName + ".");
            return field;
        }
    }
}
