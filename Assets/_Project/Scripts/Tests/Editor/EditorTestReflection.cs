using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ApexShift.Tests.Editor
{
    internal static class EditorTestReflection
    {
        public static Type GetTypeByName(string typeName)
        {
            Type type = Type.GetType(typeName);
            if (type != null)
            {
                return type;
            }

            string fullName = typeName;
            int assemblySeparator = typeName.IndexOf(',');
            if (assemblySeparator >= 0)
            {
                fullName = typeName.Substring(0, assemblySeparator).Trim();
            }

            type = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(candidate => candidate != null);

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
