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
            Assert.IsTrue(source.Contains("CharacterController"));
            Assert.IsTrue(source.Contains("Rigidbody"));
            Assert.IsTrue(source.Contains("ApplyMovement"));
            Assert.IsTrue(source.Contains("SetMovementEnabled"));
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
            Assert.IsTrue(source.Contains("Idle"));
            Assert.IsTrue(source.Contains("Walking"));
            Assert.IsTrue(source.Contains("Running"));
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
            Assert.IsTrue(source.Contains("Delta:"));
            Assert.IsTrue(source.Contains("targetPositionDelta"));
            Assert.IsTrue(source.Contains("Cam Delta:"));
            Assert.IsTrue(source.Contains("secondaryTarget"));
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

        [Test]
        public void PlayerMotionVisualFeedback_ProvidesMovementFallback()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Player/PlayerMotionVisualFeedback.cs");

            Assert.IsTrue(source.Contains("PlayerInputReader"));
            Assert.IsTrue(source.Contains("bobHeight"));
            Assert.IsTrue(source.Contains("walkBobSpeed"));
            Assert.IsTrue(source.Contains("sprintBobSpeed"));
            Assert.IsTrue(source.Contains("Vector3.Lerp"));
            Assert.IsFalse(source.Contains("Keyboard.current"));
            Assert.IsFalse(source.Contains("Mouse.current"));
            Assert.IsFalse(source.Contains("Input.GetAxisRaw"));
            Assert.IsFalse(source.Contains("Input.mousePosition"));
            Assert.IsTrue(source.Contains("ResolveVisualRoot"));
            Assert.IsTrue(source.Contains("SkinnedMeshRenderer"));
            Assert.IsTrue(source.Contains("enableBobbing"));
            Assert.IsTrue(source.Contains("SetBobbingEnabled"));
        }

        [Test]
        public void PlayerAnimationDriver_LogsSetupAndChecksStateFallbackSafely()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Player/PlayerAnimationDriver.cs");

            Assert.IsTrue(source.Contains("logAnimationSetup"));
            Assert.IsTrue(source.Contains("hasStateFallback"));
            Assert.IsTrue(source.Contains("CanUseStateFallback"));
            Assert.IsTrue(source.Contains("ContainsClip"));
            Assert.IsTrue(source.Contains("Idle/Walking/Running clips were not found"));
        }

        [Test]
        public void IsometricCameraFollow_ExposesSmoothingToggle()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Camera/IsometricCameraFollow.cs");

            Assert.IsTrue(source.Contains("enableSmoothing"));
            Assert.IsTrue(source.Contains("SetSmoothingEnabled"));
            Assert.IsTrue(source.Contains("disableWhenCinemachineBrainExists"));
            Assert.IsTrue(source.Contains("HasCinemachineBrain"));
        }

        [Test]
        public void PlayerActionDebugLog_ExposesRuntimeToggles()
        {
            string source = File.ReadAllText("Assets/_Project/Scripts/Runtime/Debug/PlayerActionDebugLog.cs");

            Assert.IsTrue(source.Contains("Movement Enabled"));
            Assert.IsTrue(source.Contains("Bobbing Enabled"));
            Assert.IsTrue(source.Contains("Camera Smoothing"));
            Assert.IsTrue(source.Contains("SetMovementController"));
            Assert.IsTrue(source.Contains("SetMotionFeedback"));
            Assert.IsTrue(source.Contains("SetCameraFollow"));
        }

        [Test]
        public void CinemachineCameraSceneBuilder_IsReferencedBySceneBuilders()
        {
            string baseScene = File.ReadAllText("Assets/_Project/Scripts/Editor/ApexShiftSceneBuilder.cs");
            string worldBuilder = File.ReadAllText("Assets/_Project/Scripts/Editor/World/HandcraftedBiomeWorldBuilder.cs");
            string helper = File.ReadAllText("Assets/_Project/Scripts/Editor/Camera/CinemachineCameraSceneBuilder.cs");

            Assert.IsTrue(baseScene.Contains("CinemachineCameraSceneBuilder.CreateIsometricCameraRig"));
            Assert.IsTrue(worldBuilder.Contains("CinemachineCameraSceneBuilder.CreateIsometricCameraRig"));
            Assert.IsTrue(baseScene.Contains("pitch: 35.264f"));
            Assert.IsTrue(baseScene.Contains("yaw: 45f"));
            Assert.IsTrue(worldBuilder.Contains("pitch: 35.264f"));
            Assert.IsTrue(worldBuilder.Contains("yaw: 45f"));
            Assert.IsTrue(helper.Contains("CreateIsometricCameraRig"));
            Assert.IsTrue(helper.Contains("CinemachineBrain"));
            Assert.IsTrue(helper.Contains("CinemachineCamera"));
            Assert.IsTrue(helper.Contains("CinemachineFollow"));
            Assert.IsTrue(helper.Contains("followDistance"));
            Assert.IsTrue(helper.Contains("orthographicSize"));
            Assert.IsFalse(helper.Contains("GetField(\"Target\""));
            Assert.IsFalse(helper.Contains("ReflectionTypeLoadException"));
        }

        [Test]
        public void UnityCameraDocs_MatchImplementedCinemachineSetup()
        {
            string docs = File.ReadAllText("Docs/unity-camera.md");

            Assert.IsTrue(docs.Contains("Cinemachine-based orthographic isometric camera"));
            Assert.IsTrue(docs.Contains("Orthographic Size: 14"));
            Assert.IsTrue(docs.Contains("Pitch: 35.264"));
            Assert.IsTrue(docs.Contains("Yaw: 45"));
            Assert.IsTrue(docs.Contains("Main Camera"));
            Assert.IsTrue(docs.Contains("PlayerFollowCamera"));
            Assert.IsTrue(docs.Contains("CinemachineFollow"));
            Assert.IsTrue(docs.Contains("IsometricCameraFollow"));
        }
    }
}
