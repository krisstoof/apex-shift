using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace ApexShift.Tests.Unit.Input
{
    public sealed class PlayerInputReaderTests
    {
        [Test]
        public void PlayerInputReader_CanBeCreatedAndAssignedInputActions()
        {
            GameObject root = new GameObject("InputRoot");
            Type readerType = Type.GetType("ApexShift.Runtime.PlayerInput.PlayerInputReader, ApexShift.Runtime");
            Assert.IsNotNull(readerType, "Could not resolve PlayerInputReader.");

            Component reader = root.AddComponent(readerType);
            ScriptableObject asset = ScriptableObject.CreateInstance("UnityEngine.InputSystem.InputActionAsset");
            Assert.IsNotNull(asset, "Could not create InputActionAsset.");

            MethodInfo setInputActions = readerType.GetMethod("SetInputActions", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(setInputActions, "Could not resolve SetInputActions.");
            setInputActions.Invoke(reader, new object[] { asset });

            FieldInfo field = readerType.GetField("inputActions", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            Assert.AreSame(asset, field.GetValue(reader));

            UnityEngine.Object.DestroyImmediate(root);
            UnityEngine.Object.DestroyImmediate(asset);
        }

        [Test]
        public void PlayerInputReader_WarnsWhenInputActionsAreMissing()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Input/PlayerInputReader.cs");

            Assert.IsTrue(source.Contains("Debug.LogWarning(\"PlayerInputReader is missing InputActionAsset."));
            Assert.IsFalse(source.Contains("Debug.LogError(\"PlayerInputReader is missing InputActionAsset."));
        }

        [Test]
        public void IsometricPlayerController_DoesNotPollKeyboardOrMouseDirectly()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Player/IsometricPlayerController.cs");

            Assert.IsFalse(source.Contains("System.Reflection"));
            Assert.IsFalse(source.Contains("GetVector2Property"));
            Assert.IsFalse(source.Contains("Keyboard.current"));
            Assert.IsFalse(source.Contains("Mouse.current"));
            Assert.IsFalse(source.Contains("Input.GetAxisRaw"));
            Assert.IsFalse(source.Contains("Input.mousePosition"));
            Assert.IsTrue(source.Contains("PlayerInputReader"));
            Assert.IsTrue(source.Contains("walkSpeed"));
            Assert.IsTrue(source.Contains("sprintSpeed"));
        }

        [Test]
        public void PlayerAnimationDriver_DoesNotPollKeyboardDirectly()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Player/PlayerAnimationDriver.cs");

            Assert.IsTrue(source.Contains("PlayerInputReader"));
            Assert.IsTrue(source.Contains("AttackPressed"));
            Assert.IsTrue(source.Contains("InteractPressed"));
            Assert.IsTrue(source.Contains("Speed"));
            Assert.IsTrue(source.Contains("IsMoving"));
            Assert.IsTrue(source.Contains("IsSprinting"));
            Assert.IsFalse(source.Contains("Keyboard.current"));
            Assert.IsFalse(source.Contains("Input.GetAxisRaw"));
        }

        [Test]
        public void PlayerActionDebugLog_UsesReaderEventsAndOnGUIInsteadOfPolling()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Debug/PlayerActionDebugLog.cs");

            Assert.IsTrue(source.Contains("PlayerInputReader"));
            Assert.IsTrue(source.Contains("InteractPressed"));
            Assert.IsTrue(source.Contains("AttackPressed"));
            Assert.IsTrue(source.Contains("OpenInventoryPressed"));
            Assert.IsTrue(source.Contains("OpenCraftingPressed"));
            Assert.IsTrue(source.Contains("ToggleMapPressed"));
            Assert.IsTrue(source.Contains("PausePressed"));
            Assert.IsTrue(source.Contains("OnGUI"));
            Assert.IsFalse(source.Contains("Keyboard.current"));
            Assert.IsFalse(source.Contains("Mouse.current"));
            Assert.IsFalse(source.Contains("Input.GetAxisRaw"));
            Assert.IsFalse(source.Contains("Input.mousePosition"));
            Assert.IsTrue(source.Contains("F1"));
            Assert.IsTrue(source.Contains("F2"));
            Assert.IsTrue(source.Contains("Reset Position"));
        }

        [Test]
        public void PlayerActionFeedback_UsesReaderEventsAndVisiblePlaceholderFeedback()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Player/PlayerActionFeedback.cs");

            Assert.IsTrue(source.Contains("PlayerInputReader"));
            Assert.IsTrue(source.Contains("AttackPressed"));
            Assert.IsTrue(source.Contains("InteractPressed"));
            Assert.IsTrue(source.Contains("OpenInventoryPressed"));
            Assert.IsTrue(source.Contains("OpenCraftingPressed"));
            Assert.IsTrue(source.Contains("ToggleMapPressed"));
            Assert.IsTrue(source.Contains("PausePressed"));
            Assert.IsTrue(source.Contains("TriggerFeedback"));
            Assert.IsTrue(source.Contains("flashDuration"));
            Assert.IsFalse(source.Contains("Keyboard.current"));
            Assert.IsFalse(source.Contains("Mouse.current"));
            Assert.IsFalse(source.Contains("Input.GetAxisRaw"));
            Assert.IsFalse(source.Contains("Input.mousePosition"));
        }
    }
}
