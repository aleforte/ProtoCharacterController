using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProtoCharacterController
{
    /// <summary>
    /// Wrapper used to whole the Camera's Pitch & Yaw (in degrees)
    /// </summary>
    public struct CameraRotation
    {
        public float Pitch;
        public float Yaw;
    }

    public class ProtoPlayerCamera : MonoBehaviour
    {
        /// <summary>
        /// The camera's current transform
        /// </summary>
        public Transform Transform { get; private set; }
        /// <summary>
        /// The transform the camera is following
        /// </summary>
        public Transform FollowTransform { get; private set; }

        /// <summary>
        /// The camera's target look rotation
        /// </summary>
        private CameraRotation _targetCameraRotation;
        public CameraRotation TargetCameraRotation { 
            get { return _targetCameraRotation; }  
            set { _targetCameraRotation = value; } 
        }

        /// <summary>
        /// The camera's target follow distance
        /// </summary>
        public float TargetDistance { get; set; }

        [Header("Framing")]
        [Tooltip("The camera object this script uses")]
        [SerializeField] private Camera Camera;
        [Tooltip("The camera's view offset from it's follow position")]
        [SerializeField] private Vector2 CameraViewOffset = new Vector2(0f, 0f);
        [Tooltip("The speed that the camera interpolates towards it's follow point")]
        [SerializeField] private float FollowingInterpSpeed = 10000f;

        [Header("Rotation")]
        [Tooltip("Should the look controls a long the horizontal-axis be inverted?")]
        [SerializeField] private bool InvertX = false;
        [Tooltip("Should the look controls along the vertical-axis be inverted?")]
        [SerializeField] private bool InvertY = false;
        [Range(-90f, 90f)]
        [Tooltip("The minimum angle that this camera can look downwards")]
        [SerializeField] private float MinVerticalAngle = -90f;
        [Range(-90f, 90f)]
        [Tooltip("The minimum angle that this camera can look downwards")]
        [SerializeField] private float MaxVerticalAngle = 90f;
        [Tooltip("The camera's look sensitivity when applying user input")]
        [SerializeField] private float LookSensitivity = 2f;
        [Tooltip("The speed that the camera interpolates towards it's target look rotation")]
        [SerializeField] private float LookInterpSpeed = 10000f;
        //[Tooltip("Does the camera rotate with the rotation of physics mover the player is standing on? (...not implemented yet)")]
        //[SerializeField] private bool RotateWithPhysicsMover = false;

        [Header("Distance")]
        [Tooltip("The default distance the camera maintains from it's follow point")]
        [SerializeField] private float DefaultDistance = 0f;
        [Tooltip("The minimum distance the camera can maintain from it's follow point")]
        [SerializeField] private float MinDistance = 0f;
        [Tooltip("The maximum distance the camera can maintain from it's follow point")]
        [SerializeField] private float MaxDistance = 10f;
        [Tooltip("The speed that the camera's follow distance moves in/out via user input")]
        [SerializeField] private float DistanceMovementSpeed = 5f;
        [Tooltip("The speed that the camera interpolates towards it's target distance")]
        [SerializeField] private float DistanceInterpSpeed = 10f;

        [Header("Obstruction")]
        [Tooltip("The radius of the sphere used to check for camera obstructions")]
        [SerializeField] private float ObstructionCheckRadius = 0.2f;
        [Tooltip("The layer mask that applies when performing obstruction checks")]
        [SerializeField] private LayerMask ObstructionLayers = -1;
        [Tooltip("The duration of time between obstruction checks (in seconds)")]
        [SerializeField] private float ObstructionCheckRate = 0.04f;
        [Tooltip("The maximum distance the camera can follow before it must begin checking for obstructions (used for optimization)")]
        [SerializeField] private float MaxDistanceBeforeObstructionChecks = 0f;
        [Tooltip("The speed that the camera interpolates towards it's obstruction distance")]
        [SerializeField] private float ObstructionInterpSpeed = 50f;
        [Tooltip("The colliders to be ignored when performing obstruction checks")]
        [SerializeField] private List<Collider> IgnoredColliders = new List<Collider>();

        private Vector3 _currentFollowPosition;

        private bool _distanceIsObstructed;
        private float _currentDistance;
        private int _obstructionCount;
        private RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];
        private float _lastObstructionCheckTime;
        private const int MaxObstructions = 32; // max number of obstructions that an obstruction check can detect

        void OnValidate()
        {
            MaxVerticalAngle = Mathf.Max(MinVerticalAngle, MaxVerticalAngle);
            MinVerticalAngle = Mathf.Min(MinVerticalAngle, MaxVerticalAngle);
            DefaultDistance = Mathf.Clamp(DefaultDistance, MinDistance, MaxDistance);
        }

        void Awake()
        {
            Transform = this.transform;

            _targetCameraRotation.Yaw = Transform.eulerAngles.y;
            _targetCameraRotation.Pitch = 0f;

            TargetDistance = DefaultDistance;
            _currentDistance = TargetDistance;
            _distanceIsObstructed = false;
            _obstructionCount = 0;
            _lastObstructionCheckTime = 0f;
        }

        /// <summary>
        /// Sets the transform that the camera will orbit around
        /// </summary>
        public void SetFollowTransform(Transform t)
        {
            FollowTransform = t;
            Transform.parent = FollowTransform;
            Transform.localPosition = Vector3.zero;
            _targetCameraRotation.Yaw = FollowTransform.eulerAngles.y;
                        
            //_currentFollowPosition = FollowTransform.position;

            // Start camera at follow transform 
            //Transform.position = _currentFollowPosition;
            //Transform.rotation = Quaternion.Euler(0f, _targetCameraRotation.Yaw, 0f);
        }

        /// <summary>
        /// Adds a collider to be ignored from obstruction checks
        /// </summary>
        public void AddIgnoredCollider(Collider collider)
        {
            IgnoredColliders.Add(collider);
        }

        /// <summary>
        /// Adds multiple colliders to be ignored from obstruction checks
        /// </summary>
        public void AddIgnoredColliders(Collider[] colliders)
        {
            IgnoredColliders.AddRange(colliders);
        }

        /// <summary>
        /// Applies rotation and zoom inputs to update the camera
        /// </summary>
        public void UpdateWithInput(Vector2 rotationInput, float zoomInput, float deltaTime)
        {
            if (FollowTransform)
            {
                if (InvertX)
                {
                    rotationInput.x *= -1f;
                }
                if (InvertY)
                {
                    rotationInput.y *= -1f;
                }

                UpdateRotationWithInput(rotationInput, deltaTime);
                //UpdatePositionWithInput(zoomInput, deltaTime);
            }
        }

        /// <summary>
        /// Updates the camera's rotation based on the player's rotation input
        /// </summary>
        /// <param name="rotationInput">The player's rotation input</param>
        /// <param name="deltaTime">The current deltaTime</param>
        private void UpdateRotationWithInput(Vector2 rotationInput, float deltaTime)
        {
            _targetCameraRotation.Yaw = (_targetCameraRotation.Yaw + (rotationInput.x * LookSensitivity)) % 360;
            _targetCameraRotation.Yaw += _targetCameraRotation.Yaw < 0 ? 360 : 0; // ensures rotation is always between 0 and 360
            Quaternion yawRot = Quaternion.Euler(0f, _targetCameraRotation.Yaw, 0f);

            _targetCameraRotation.Pitch -= (rotationInput.y * LookSensitivity);
            _targetCameraRotation.Pitch = Mathf.Clamp(_targetCameraRotation.Pitch, MinVerticalAngle, MaxVerticalAngle);
            Quaternion pitchRot = Quaternion.Euler(_targetCameraRotation.Pitch, 0f, 0f);

            // Derive rotation from planar & vertical rotations
            Transform.rotation = Quaternion.Slerp(Transform.rotation, yawRot * pitchRot, 1f - Mathf.Exp(-LookInterpSpeed * deltaTime));
        }

        /// <summary>
        /// Updates the camera's position based on it's tracked follow position and player zoom input
        /// </summary>
        /// <param name="zoomInput">The player's zoom input</param>
        /// <param name="deltaTime">The current deltaTime</param>
        private void UpdatePositionWithInput(float zoomInput, float deltaTime)
        {
            // Apply smoothing to camera movement
            _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position, 1 - Mathf.Exp(-FollowingInterpSpeed * deltaTime));

            if (!Mathf.Approximately(zoomInput, 0f))
            {
                if (_distanceIsObstructed)
                {
                    TargetDistance = _currentDistance;
                }
                TargetDistance += zoomInput * DistanceMovementSpeed;
                TargetDistance = Mathf.Clamp(TargetDistance, MinDistance, MaxDistance);
            }

            // Handle obstructions
            RaycastHit closestHit = new RaycastHit();
            closestHit.distance = Mathf.Infinity;
            if (_distanceIsObstructed || (TargetDistance > MaxDistanceBeforeObstructionChecks && HasObstructionCheckTimeElapsed()))
            {
                _distanceIsObstructed = CheckForObstructions(ref closestHit);
                _lastObstructionCheckTime = Time.time;
            }
            _currentDistance = _distanceIsObstructed
                ? Mathf.Lerp(_currentDistance, closestHit.distance, 1 - Mathf.Exp(-ObstructionInterpSpeed * deltaTime))
                : Mathf.Lerp(_currentDistance, TargetDistance, 1 - Mathf.Exp(-DistanceInterpSpeed * deltaTime));

            // Find smoothed camera target position
            Vector3 targetPosition = _currentFollowPosition - ((Transform.rotation * Vector3.forward) * _currentDistance);

            // Handle offset
            targetPosition += Transform.right * CameraViewOffset.x;
            targetPosition += Transform.up * CameraViewOffset.y;

            // Apply position
            Transform.position = targetPosition;
        }

        /// <summary>
        /// Performs an obstruction check to see if any colliders exist between the camera and it's target follow distance
        /// (NOTE: notice side-effect -- hit is passed and updated by reference!)
        /// </summary>
        private bool CheckForObstructions(ref RaycastHit hit)
        {
            _obstructionCount = Physics.SphereCastNonAlloc(_currentFollowPosition, ObstructionCheckRadius, -Transform.forward, _obstructions, TargetDistance, ObstructionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < _obstructionCount; i++)
            {
                bool isIgnored = false;
                for (int j = 0; j < IgnoredColliders.Count; j++)
                {
                    if (IgnoredColliders[j] == _obstructions[i].collider)
                    {
                        isIgnored = true;
                        break;
                    }
                }
                if (!isIgnored && _obstructions[i].distance < hit.distance && _obstructions[i].distance > 0)
                {
                    hit = _obstructions[i];
                }
            }
            return hit.distance < Mathf.Infinity;
        }

        /// <summary>
        /// Returns true if enough time has passed since the last obstruction check, else false
        /// </summary>
        private bool HasObstructionCheckTimeElapsed()
        {
            return Time.time - _lastObstructionCheckTime > ObstructionCheckRate;
        }

    }
}
