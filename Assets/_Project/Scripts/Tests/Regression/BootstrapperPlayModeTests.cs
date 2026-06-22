using ApexShift.Runtime.Bootstrap;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace ApexShift.Tests.Regression
{
    public class BootstrapperPlayModeTests
    {
        [UnityTest]
        public IEnumerator GameBootstrapperLogsInitializationMessage()
        {
            GameObject root = new GameObject("BootstrapperRoot");
            root.AddComponent<GameBootstrapper>();

            LogAssert.Expect(LogType.Log, "Apex Shift base playable scene initialized.");

            yield return null;
        }
    }
}
