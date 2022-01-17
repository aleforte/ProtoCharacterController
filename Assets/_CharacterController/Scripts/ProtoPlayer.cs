using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace ProtoCharacterController
{
    [RequireComponent(typeof(ProtoInputManager))]
    public class ProtoPlayer : MonoBehaviour
    {
        public ProtoCharacterController Character;
        public ProtoPlayerCamera Camera;

        private Transform _cameraFollowPoint;
        private ProtoInputManager _inputManager;
        private PlayerInputs _playerInputs;
        private Vector2 _lookInputVector;

        // input labels
        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string VerticalInput = "Vertical";
        private const string HorizontalInput = "Horizontal";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string JumpInput = "Jump";

        void Awake()
        {
            _inputManager = GetComponent<ProtoInputManager>();
            _playerInputs = new PlayerInputs();
            _lookInputVector = new Vector2();
        }

        private void OnDisable()
        {
            _inputManager.UnbindAll();
        }

        void Start()
        {
            // Tell camera to follow Character
            if (Character)
            {
                Cursor.lockState = CursorLockMode.Locked;
                _cameraFollowPoint = Character.CameraFollowPoint;
                Camera.SetFollowTransform(_cameraFollowPoint);

                // Ignore the character's collider(s) camera from obstruction checks
                Camera.AddIgnoredColliders(Character.GetComponentsInChildren<Collider>());

                // input handling
                _inputManager.BindAxis2D(MouseXInput, MouseYInput, AddLookInput);
                _inputManager.BindAxis2D(VerticalInput, HorizontalInput, AddMoveInput);

                _inputManager.BindAction(JumpInput, Jump, KeyContext.Pressed);
            }
        }

        private void AddLookInput(float mouseX, float mouseY)
        {
            _lookInputVector.x = mouseX;
            _lookInputVector.y = mouseY;
        }

        private void AddMoveInput(float forward, float right)
        {
            _playerInputs.MoveAxisForward = forward;
            _playerInputs.MoveAxisRight = right;
        }

        private void Jump()
        {
            Character.Jump();
        }

        void Update()
        {
            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            if (Camera)
            {
                float zoomInput = -Input.GetAxis(MouseScrollInput);
                Camera.UpdateWithInput(_lookInputVector, zoomInput, Time.deltaTime);
            }
        }

        private void HandleCharacterInput()
        {
            if (Character)
            {
                _playerInputs.CameraLookRotation = Camera.TargetCameraRotation;
                Character.SetInputs(ref _playerInputs);
            }
        }
    }
}

