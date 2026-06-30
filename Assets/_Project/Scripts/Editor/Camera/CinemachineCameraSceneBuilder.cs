using UnityEditor;
using UnityEngine;
using Unity.Cinemachine;
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
            if (player == null)
            {
                Debug.LogWarning("Cinemachine isometric camera rig was created without a player target.");
            }

            Vector3 focusOffset = new Vector3(0f, 1.25f, 0f);
            Quaternion rigRotation = Quaternion.Euler(pitch, yaw, roll);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.rotation = rigRotation;

            CameraComponent camera = cameraObject.AddComponent<CameraComponent>();
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            EnsureSingleAudioListener(cameraObject);
            cameraObject.AddComponent<CinemachineBrain>();

            GameObject followCamera = new GameObject("PlayerFollowCamera");
            followCamera.transform.SetParent(parent, false);
            followCamera.transform.rotation = rigRotation;

            Vector3 cameraOffset = -(rigRotation * Vector3.forward) * followDistance + focusOffset;
            followCamera.transform.position = player != null ? player.position + cameraOffset : cameraOffset;

            CinemachineCamera cinemachineCamera = followCamera.AddComponent<CinemachineCamera>();
            cinemachineCamera.Target.TrackingTarget = player;
            cinemachineCamera.Target.LookAtTarget = player;
            LensSettings lens = LensSettings.FromCamera(camera);
            lens.ModeOverride = LensSettings.OverrideModes.Orthographic;
            lens.OrthographicSize = orthographicSize;
            cinemachineCamera.Lens = lens;
            cinemachineCamera.Priority.Value = 20;

            CinemachineFollow follow = followCamera.AddComponent<CinemachineFollow>();
            follow.FollowOffset = cameraOffset;
            followCamera.AddComponent<ApexShift.Runtime.Camera.CinemachineOrthographicZoom>();

            Debug.Log(
                $"Cinemachine isometric camera created. MainCamera={cameraObject.name}, " +
                $"FollowCamera={followCamera.name}, Target={(player != null ? player.name : "<null>")}, Offset={cameraOffset}");

            return cameraObject;
        }

        private static void EnsureSingleAudioListener(GameObject cameraObject)
        {
            if (cameraObject == null)
            {
                return;
            }

            AudioListener listener = cameraObject.GetComponent<AudioListener>();
            if (listener == null)
            {
                cameraObject.AddComponent<AudioListener>();
            }

            AudioListener[] allListeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Include);
            bool keptFirst = false;
            foreach (AudioListener item in allListeners)
            {
                if (item == null)
                {
                    continue;
                }

                if (!keptFirst)
                {
                    keptFirst = true;
                    item.enabled = true;
                    continue;
                }

                if (item.gameObject != cameraObject)
                {
                    item.enabled = false;
                }
            }
        }
    }
}
