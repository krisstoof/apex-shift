using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.EditorTools.Camera
{
    public static class CinemachineCameraSceneBuilder
    {
        public static GameObject CreateIsometricCameraRig(
            Transform parent,
            Transform player,
            float pitch = 35.264f,
            float yaw = 45f,
            float roll = 0f,
            float orthographicSize = 14f,
            float followDistance = 20f)
        {
            Vector3 focusOffset = new Vector3(0f, 1.25f, 0f);
            Quaternion rigRotation = Quaternion.Euler(pitch, yaw, roll);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";

            CameraComponent camera = cameraObject.AddComponent<CameraComponent>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            cameraObject.transform.rotation = rigRotation;

            bool hasBrain = TryAddCinemachineBrain(cameraObject);
            GameObject followCamera = new GameObject("PlayerFollowCamera");
            followCamera.transform.SetParent(cameraObject.transform, false);
            followCamera.transform.localRotation = Quaternion.identity;
            followCamera.transform.localPosition = Vector3.zero;

            bool hasCinemachineCamera = TryAddCinemachineCamera(followCamera, player, orthographicSize, focusOffset, followDistance);
            if (!hasBrain || !hasCinemachineCamera)
            {
                var fallback = cameraObject.GetComponent<ApexShift.Runtime.Camera.IsometricCameraFollow>();
                if (fallback == null)
                {
                    fallback = cameraObject.AddComponent<ApexShift.Runtime.Camera.IsometricCameraFollow>();
                }

                fallback.SetTarget(player);
                fallback.SetInitialRotation(rigRotation);
                fallback.SetFollowDistance(followDistance);
                fallback.SetOrthographicSize(orthographicSize);
                fallback.SnapToTarget();
            }

            return cameraObject;
        }

        private static bool TryAddCinemachineBrain(GameObject cameraObject)
        {
            Type brainType = FindType("CinemachineBrain");
            if (brainType == null)
            {
                return false;
            }

            cameraObject.AddComponent(brainType);
            return true;
        }

        private static bool TryAddCinemachineCamera(
            GameObject followCamera,
            Transform player,
            float orthographicSize,
            Vector3 focusOffset,
            float followDistance)
        {
            Type cinemachineType = FindType("CinemachineCamera") ?? FindType("CinemachineVirtualCamera");
            if (cinemachineType == null)
            {
                return false;
            }

            Component component = followCamera.AddComponent(cinemachineType);
            if (component == null)
            {
                return false;
            }

            SetProperty(component, "Follow", player);
            SetProperty(component, "LookAt", player);
            SetTarget(component, player);
            SetPriority(component, 10);
            SetOrthographicLens(component, orthographicSize);
            return true;
        }

        private static void SetTarget(Component component, Transform player)
        {
            FieldInfo targetField = component.GetType().GetField("Target", BindingFlags.Instance | BindingFlags.Public);
            if (targetField == null)
            {
                return;
            }

            object target = targetField.GetValue(component);
            if (target == null)
            {
                return;
            }

            Type targetType = target.GetType();
            FieldInfo trackingTargetField = targetType.GetField("TrackingTarget", BindingFlags.Instance | BindingFlags.Public);
            trackingTargetField?.SetValue(target, player);

            FieldInfo lookAtTargetField = targetType.GetField("LookAtTarget", BindingFlags.Instance | BindingFlags.Public);
            lookAtTargetField?.SetValue(target, player);

            FieldInfo customLookAtTargetField = targetType.GetField("CustomLookAtTarget", BindingFlags.Instance | BindingFlags.Public);
            customLookAtTargetField?.SetValue(target, true);

            targetField.SetValue(component, target);
        }

        private static void SetPriority(Component component, int priority)
        {
            FieldInfo priorityField = component.GetType().BaseType?.GetField("Priority", BindingFlags.Instance | BindingFlags.Public);
            if (priorityField == null)
            {
                return;
            }

            object value = priorityField.GetValue(component);
            if (value == null)
            {
                return;
            }

            Type priorityType = value.GetType();
            PropertyInfo priorityValueProperty = priorityType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            priorityValueProperty?.SetValue(value, priority);
            priorityField.SetValue(component, value);
        }

        private static void SetOrthographicLens(Component component, float orthographicSize)
        {
            PropertyInfo lensProperty = component.GetType().GetProperty("Lens", BindingFlags.Instance | BindingFlags.Public);
            if (lensProperty == null)
            {
                return;
            }

            object lens = lensProperty.GetValue(component);
            if (lens == null)
            {
                return;
            }

            SetProperty(lens, "Orthographic", true);
            SetProperty(lens, "OrthographicSize", orthographicSize);
            lensProperty.SetValue(component, lens);
        }

        private static void SetProperty(object target, string name, object value)
        {
            if (target == null)
            {
                return;
            }

            PropertyInfo property = target.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
            property?.SetValue(target, value);
        }

        private static Type FindType(string simpleName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                if (types == null)
                {
                    continue;
                }

                foreach (Type type in types)
                {
                    if (type != null && type.Name == simpleName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }
    }
}
