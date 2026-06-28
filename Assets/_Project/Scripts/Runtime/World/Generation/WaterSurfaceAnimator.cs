using UnityEngine;

namespace ApexShift.Runtime.World.Generation
{
    public sealed class WaterSurfaceAnimator : MonoBehaviour
    {
        [SerializeField] private float shoreWaveAmplitude = 0.012f;
        [SerializeField] private float deepWaveAmplitude = 0.02f;
        [SerializeField] private float waveSpeed = 0.65f;
        private Vector3 _basePosition;
        private Vector3 _baseScale;
        private bool _isShoreline;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float amplitude = _isShoreline ? shoreWaveAmplitude : deepWaveAmplitude;
            float offset = Mathf.Sin((Time.time * waveSpeed) + transform.position.x * 0.08f + transform.position.z * 0.06f) * amplitude;
            transform.localPosition = _basePosition + new Vector3(0f, offset, 0f);

            float tilt = Mathf.Sin(Time.time * waveSpeed * 0.7f + transform.position.x * 0.04f) * (amplitude * 0.35f);
            transform.localRotation = Quaternion.Euler(tilt * 12f, 0f, -tilt * 12f);

            transform.localScale = _baseScale;
        }

        public void Configure(bool shoreline)
        {
            _isShoreline = shoreline;
        }
    }
}
