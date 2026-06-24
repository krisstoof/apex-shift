using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ithappy.Animals_FREE
{
    [RequireComponent(typeof(CreatureMover))]
    public class MovePlayerInput : MonoBehaviour
    {
        [Header("Character")]
#if !ENABLE_INPUT_SYSTEM
        [SerializeField]
        private string m_HorizontalAxis = "Horizontal";
        [SerializeField]
        private string m_VerticalAxis = "Vertical";
        [SerializeField]
        private string m_JumpButton = "Jump";
        [SerializeField]
        private KeyCode m_RunKey = KeyCode.LeftShift;
#endif

        [Header("Camera")]
        [SerializeField]
        private PlayerCamera m_Camera;
#if !ENABLE_INPUT_SYSTEM
        [SerializeField]
        private string m_MouseX = "Mouse X";
        [SerializeField]
        private string m_MouseY = "Mouse Y";
        [SerializeField]
        private string m_MouseScroll = "Mouse ScrollWheel";
#endif

        private CreatureMover m_Mover;

        private Vector2 m_Axis;
        private bool m_IsRun;
        private bool m_IsJump;

        private Vector3 m_Target;
        private Vector2 m_MouseDelta;
        private float m_Scroll;

        private void Awake()
        {
            m_Mover = GetComponent<CreatureMover>();
        }

        private void Update()
        {
            GatherInput();
            SetInput();
        }

        public void GatherInput()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            float x = 0;
            float y = 0;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1;

                m_IsRun = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
                m_IsJump = keyboard.spaceKey.wasPressedThisFrame;
            }

            m_Axis = new Vector2(x, y);

            if (mouse != null)
            {
                m_MouseDelta = mouse.delta.ReadValue() * 0.05f; // Scale down to match legacy sensitivity better
                m_Scroll = mouse.scroll.ReadValue().y * 0.01f;
            }
#else
            m_Axis = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
            m_IsRun = Input.GetKey(m_RunKey);
            m_IsJump = Input.GetButton(m_JumpButton);

            m_MouseDelta = new Vector2(Input.GetAxis(m_MouseX), Input.GetAxis(m_MouseY));
            m_Scroll = Input.GetAxis(m_MouseScroll);
#endif
            m_Target = (m_Camera == null) ? Vector3.zero : m_Camera.Target;
        }

        public void BindMover(CreatureMover mover)
        {
            m_Mover = mover;
        }

        public void SetInput()
        {
            if (m_Mover != null)
            {
                m_Mover.SetInput(in m_Axis, in m_Target, in m_IsRun, m_IsJump);
            }

            if (m_Camera != null)
            {
                m_Camera.SetInput(in m_MouseDelta, m_Scroll);
            }
        }
    }
}