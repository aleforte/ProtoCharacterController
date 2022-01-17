using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;

namespace ProtoCharacterController
{
    /// <summary>
    /// 
    /// </summary>
    public struct PlayerInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public bool JumpDown;
        public CameraRotation CameraLookRotation;
    }

    public class ProtoCharacterController : MonoBehaviour, ICharacterController
    {
        [Header("General")]
        [Tooltip("The movement/collision engine for this character (required)")]
        public Transform CameraFollowPoint;
        private Transform MeshRoot;
        [SerializeField] private KinematicCharacterMotor Motor;
        [SerializeField] private float OrientationTurnSpeed = 50;

        [Header("Ground Movement")]
        [Tooltip("The character's movement acceleration while on ground")]
        [SerializeField] private float GroundAcceleration = 10.0f;
        [Tooltip("The friction applied to the character while on ground")]
        [SerializeField] private float GroundFriction = 6.0f;
        [Tooltip("The maximum ground velocity the character can achieve")]
        [SerializeField] private float MaxVelocityGround = 12.0f;
        //[Tooltip("Acceleration scaling while crouched/ducking.")]
        //[SerializeField] private float DuckScale = 0.50f;

        [Header("Air Movement")]
        [Tooltip("The character's movement acceleration while in air")]
        [SerializeField] private float AirAcceleration = 1.0f;
        [Tooltip("The friction applied to the character while in air")]
        [SerializeField] private float AirFriction = 2.0f;
        [Tooltip("The maximum air velocity the character can achieve")]
        [SerializeField] private float MaxVelocityAir = 10.0f;

        /* // TODO implement swimming
        [Header("Swim Movement")]
        [Tooltip("The character's movement acceleration while swimming")]
        [SerializeField] private float WaterAcceleration = 4.0f;
        [Tooltip("The friction applied to the character while swimming")]
        [SerializeField] private float WaterFriction = 1.0f;
        [Tooltip("The maximum water velocity the character can achieve")]
        [SerializeField] private float MaxVelocityWater = 1000.0f;
        */

        [Header("Jumping")]
        [Tooltip("The upward velocity applied when jumping")]
        [SerializeField] private float JumpUpSpeed = 4f;
        [Tooltip("Allows for double/multi jumping")]
        [SerializeField] private int JumpMaxCount = 1;
        [Tooltip("If true, the character does not need to be on stable ground to perform jump")]
        [SerializeField] public bool AllowJumpingWhenSliding = false;
        [Tooltip("The amount of time BEFORE grounding when a player press 'Jump' and the input be accepted.")]
        [SerializeField] private float JumpPreGroundingGraceTime = 0.1f;
        [Tooltip("The amount of time AFTER grounding when a player press 'Jump' and the input be accepted.")]
        [SerializeField] private float JumpPostGroundingGraceTime = 0.1f;

        // input vectors
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;

        // jump state
        private bool _wasJumpPressed = false;
        private bool _wasJumpExecuted = false;
        private int _jumpCurrentCount = 0;
        private float _timeSinceJumpRequested = 0f;
        private float _timeSinceAllowedToJump = 0f;

        void Start()
        {
            // Assign this character controller to the motor
            Motor.CharacterController = this;
        }

        /// <summary>
        /// This is called every frame by MyPlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerInputs inputs)
        {
            // Clamp move input (using ClampMagnitude(), better than normalized for analog sticks)
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);
            Quaternion cameraPlanarRotation = Quaternion.Euler(inputs.CameraLookRotation.Yaw * Motor.CharacterUp);

            // Set input buffers
            _moveInputVector = cameraPlanarRotation * moveInputVector;
            _lookInputVector = cameraPlanarRotation * Vector3.forward;

            if (inputs.JumpDown)
            {
                _wasJumpPressed = true;
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
            //Debug.Log("TestCharacterController::BeforeCharacterUpdate() called!");
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            //Debug.Log("TestCharacterController::UpdateRotation() called!");

            if (_lookInputVector != Vector3.zero && OrientationTurnSpeed > 0f)
            {
                Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationTurnSpeed * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp); // passed by ref, used by KinematicCharacterMotor
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                MoveGround(ref currentVelocity, _moveInputVector, deltaTime);
            }
            else // is falling
            {
                MoveAir(ref currentVelocity, _moveInputVector, deltaTime);
            }

            CheckJumpInput(ref currentVelocity, deltaTime);
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            UpdateJumpState(deltaTime);
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            //Debug.Log("TestCharacterController::PostGroundingUpdate() called!");
        }


        public bool IsColliderValidForCollisions(Collider coll)
        {
            //Debug.Log("TestCharacterController::IsColliderValidForCollisions() called!");
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            //Debug.Log("TestCharacterController::OnGroundHit() called!");
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            //Debug.Log("TestCharacterController::OnMovementHit() called!");
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            //Debug.Log("TestCharacterController::ProcessHitStabilityReport() called!");
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            //Debug.Log("TestCharacterController::PostGroundingUpdate() called!");
        }

        private void MoveGround(ref Vector3 currentVelocity, Vector3 accelDir, float deltaTime)
        {
            // Reorient source velocity & input on current ground slope (prevents velocity losses when slope changes)
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, Motor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;
            Vector3 reorientedAccelDir = Motor.GetDirectionTangentToSurface(accelDir, Motor.GroundingStatus.GroundNormal) * _moveInputVector.magnitude;

            ApplyFriction(ref currentVelocity, GroundFriction, deltaTime);
            ApplyAcceleration(ref currentVelocity , reorientedAccelDir, GroundAcceleration, MaxVelocityGround, deltaTime);
        }

        private void MoveAir(ref Vector3 currentVelocity, Vector3 accelDir, float deltaTime)
        {
            // only apply acceleration to movement on XZ plane
            Vector3 xzVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
            ApplyFriction(ref xzVelocity, AirFriction, deltaTime);
            ApplyAcceleration(ref xzVelocity, accelDir, AirAcceleration, MaxVelocityAir, deltaTime);

            // add gravity 
            currentVelocity = xzVelocity + new Vector3(0f, currentVelocity.y, 0f);
            currentVelocity += Physics.gravity * deltaTime;
        }

        private void ApplyFriction(ref Vector3 currentVelocity, float friction, float deltaTime)
        {
            float speed = currentVelocity.magnitude;
            if (speed != 0) // To avoid divide by zero errors
            {
                float drop = speed * friction * deltaTime;
                currentVelocity *= Mathf.Max(speed - drop, 0) / speed;
            }
        }

        private void ApplyAcceleration(ref Vector3 currentVelocity, Vector3 accelDir, float accelerate, float maxVelocity, float deltaTime)
        {
            float projVel = Vector3.Dot(currentVelocity, accelDir); // Vector projection of Current velocity onto accelDir.
            float accelVel = accelerate * deltaTime; // Accelerated velocity in direction of movement

            // If necessary, truncate the accelerated velocity so the vector projection does not exceed max_velocity
            if (projVel + accelVel > maxVelocity)
                accelVel = maxVelocity - projVel;

            currentVelocity += accelDir * accelVel;
        }

        public void Jump()
        {
            _wasJumpPressed = true;
            _timeSinceJumpRequested = 0f;
        }

        public void StopJumping()
        {
            ResetJumpState();
        }

        private void ResetJumpState()
        {
            _wasJumpPressed = false;
            _wasJumpExecuted = false;

            if (CanJumpFromCurrentGround())
            {
                _jumpCurrentCount = 0;
                _timeSinceAllowedToJump = 0f;
            }
        }

        private void CheckJumpInput(ref Vector3 currentVelocity, float deltaTime)
        {
            if (_wasJumpPressed)
            {
                bool isFirstJump = _jumpCurrentCount == 0;
                bool isInAir = !Motor.GroundingStatus.FoundAnyGround && (_timeSinceAllowedToJump > JumpPostGroundingGraceTime);
                if (isFirstJump && isInAir) 
                {
                    // prevents player from jumping while in air unless multi-jump enabled
                    _jumpCurrentCount++; 
                }

                if (!_wasJumpExecuted && CanJump())
                {
                    ApplyJumpImpulse(ref currentVelocity, deltaTime);
                    _jumpCurrentCount++;
                    _wasJumpExecuted = true;
                }
            }
        }

        private bool CanJump()
        {
            return _jumpCurrentCount < JumpMaxCount;
        }

        private void ApplyJumpImpulse(ref Vector3 currentVelocity, float deltaTime)
        {
            // Makes the character skip ground probing/snapping on its next update. 
            // If this line weren't here, the character would remain snapped to the ground when trying to jump.
            Motor.ForceUnground();

            // Calculate jump direction before ungrounding
            Vector3 jumpDirection = Motor.CharacterUp;
            if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
            {
                jumpDirection = Motor.GroundingStatus.GroundNormal;
            }

            currentVelocity.y = 0f; // offset downward momentum if falling
            currentVelocity += (jumpDirection * JumpUpSpeed);
        }

        /// <summary>
        /// Update's the jump state variables after the character's movement is executed
        /// </summary>
        public void UpdateJumpState(float deltaTime)
        {
            _timeSinceJumpRequested += deltaTime; // reset to zero on jump call

            if (_wasJumpExecuted) // if we've jumped this frame
            {
                _wasJumpExecuted = false;
                _wasJumpPressed = false;
            }
            else if (CanJumpFromCurrentGround())
            {
                _jumpCurrentCount = 0;
                _timeSinceAllowedToJump = 0f; 

                bool isWithinPreGroundGracePeriod =  _timeSinceJumpRequested < JumpPreGroundingGraceTime;
                if (_wasJumpPressed && !isWithinPreGroundGracePeriod)
                {
                    _wasJumpPressed = false;
                }
            }
            else
            {
                _timeSinceAllowedToJump += deltaTime;
            }
        }

        /// <summary>
        /// Returns true if the character can jump from the current ground it's on, else false
        /// </summary>
        private bool CanJumpFromCurrentGround()
        {
            return AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround;
        }
    }
}
