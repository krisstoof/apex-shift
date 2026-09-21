using System;
using UnityEngine;
using Unity.Cinemachine;
using ApexShift.Runtime.Camera;
using CameraComponent = UnityEngine.Camera;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class RuntimeCameraSetup
    {
        public GameObject Create(Transform parent, Transform target, bool useCinemachine)
        {
            return useCinemachine ? CreateCinemachine(parent, target) : CreateBasic(parent, target);
        }

        private static GameObject CreateBasic(Transform parent, Transform target)
        {
            GameObject go = new GameObject("Main Camera");
            go.transform.SetParent(parent, true);
            go.tag = "MainCamera";
            CameraComponent camera = go.AddComponent<CameraComponent>();
            camera.orthographic = true;
            camera.orthographicSize = 14f;
            EnsureAudioListener(go);
            AddUrpCameraData(go);
            IsometricCameraFollow follow = go.AddComponent<IsometricCameraFollow>();
            follow.SetTarget(target);
            follow.SetInitialRotation(Quaternion.Euler(35.264f, 45f, 0f));
            follow.SnapToTarget();
            return go;
        }

        private static GameObject CreateCinemachine(Transform parent, Transform target)
        {
            const float pitch = 35.264f;
            const float yaw = 45f;
            const float orthographicSize = 14f;
            const float followDistance = 20f;
            Vector3 focusOffset = new Vector3(0f, 1.25f, 0f);
            Quaternion rigRotation = Quaternion.Euler(pitch, yaw, 0f);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, true);
            cameraObject.transform.rotation = rigRotation;
            cameraObject.tag = "MainCamera";
            CameraComponent camera = cameraObject.AddComponent<CameraComponent>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            EnsureAudioListener(cameraObject);
            AddUrpCameraData(cameraObject);
            cameraObject.AddComponent<CinemachineBrain>();

            GameObject followObject = new GameObject("PlayerFollowCamera");
            followObject.transform.SetParent(parent, true);
            followObject.transform.rotation = rigRotation;
            Vector3 offset = -(rigRotation * Vector3.forward) * followDistance + focusOffset;
            followObject.transform.position = target != null ? target.position + offset : offset;
            CinemachineCamera cinemachine = followObject.AddComponent<CinemachineCamera>();
            cinemachine.Target.TrackingTarget = target;
            cinemachine.Target.LookAtTarget = target;
            LensSettings lens = LensSettings.FromCamera(camera);
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = orthographicSize;
            cinemachine.Lens = lens;
            cinemachine.Priority.Value = 20;
            CinemachineFollow follow = followObject.AddComponent<CinemachineFollow>();
            follow.FollowOffset = offset;
            followObject.AddComponent<CinemachineOrthographicZoom>();
            return cameraObject;
        }

        private static void AddUrpCameraData(GameObject cameraObject)
        {
            Type type = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (type == null) return;
            Component data = cameraObject.AddComponent(type);
            var property = type.GetProperty("renderType");
            if (property != null) property.SetValue(data, 0);
        }

        private static void EnsureAudioListener(GameObject cameraObject)
        {
            AudioListener listener = cameraObject.GetComponent<AudioListener>();
            if (listener == null) listener = cameraObject.AddComponent<AudioListener>();
            AudioListener[] listeners = UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            bool kept = false;
            foreach (AudioListener item in listeners)
            {
                if (item == null) continue;
                item.enabled = !kept || item == listener;
                if (item.enabled) kept = true;
            }
        }
    }
}
