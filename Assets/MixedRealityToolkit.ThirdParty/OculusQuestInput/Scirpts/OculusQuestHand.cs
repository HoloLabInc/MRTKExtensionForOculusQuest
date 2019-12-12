using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace HoloLab.MixedReality.Toolkit.MagicLeapInput
{
    [MixedRealityController(
        SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class MagicLeapHand : BaseController, IMixedRealityHand
    {
        protected Vector3 CurrentControllerPosition = Vector3.zero;
        protected Quaternion CurrentControllerRotation = Quaternion.identity;
        protected MixedRealityPose CurrentControllerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose lastPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose lastGripPose = MixedRealityPose.ZeroIdentity;

        private readonly HandRay handRay = new HandRay();

        private static readonly float KeyPoseConfidenceThreshold = 0.3f;

        // TODO: Hand mesh
        // private int[] handMeshTriangleIndices = null;
        // private Vector2[] handMeshUVs;

        public MagicLeapHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, new MixedRealityInputAction(4, "Pointer Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, new MixedRealityInputAction(3, "Grip Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(2, "Select", AxisType.Digital, DeviceInputType.Select, new MixedRealityInputAction(1, "Select", AxisType.Digital)),
            new MixedRealityInteractionMapping(3, "Grab", AxisType.SingleAxis, DeviceInputType.TriggerPress, new MixedRealityInputAction(7, "Grip Press", AxisType.SingleAxis)),
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger,  new MixedRealityInputAction(13, "Index Finger Pose", AxisType.SixDof)),
        };

        public override void SetupDefaultInteractions(Handedness controllerHandedness)
        {
            AssignControllerMappings(DefaultInteractions);
        }

        #region IMixedRealityHand Implementation

        /// <inheritdoc/>
        public bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose)
        {
            return jointPoses.TryGetValue(joint, out pose);
        }

        #endregion IMixedRealityHand Implementation

        public override bool IsInPointingPose
        {
            get
            {
                return true;
            }
        }

        protected bool IsPinching { set; get; }

        protected Vector3 GetPalmNormal()
        {
            return -Vector3.up;
        }

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(MLHand hand)
        {
            if (!Enabled) { return; }

            UpdateHandData(hand);

            lastPointerPose = currentPointerPose;
            lastGripPose = currentGripPose;

            Vector3 pointerPosition = jointPoses[TrackedHandJoint.Palm].Position;
            IsPositionAvailable = IsRotationAvailable = pointerPosition != Vector3.zero;

            if (IsPositionAvailable)
            {
                handRay.Update(pointerPosition, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);

                Ray ray = handRay.Ray;

                currentPointerPose.Position = ray.origin;
                currentPointerPose.Rotation = Quaternion.LookRotation(ray.direction);

                currentGripPose = jointPoses[TrackedHandJoint.Palm];
            }

            if (lastGripPose != currentGripPose)
            {
                if (IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentGripPose);
                }
                else if (IsPositionAvailable && !IsRotationAvailable)
                {
                    InputSystem?.RaiseSourcePositionChanged(InputSource, this, currentPointerPosition);
                }
                else if (!IsPositionAvailable && IsRotationAvailable)
                {
                    InputSystem?.RaiseSourceRotationChanged(InputSource, this, currentPointerRotation);
                }
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = currentPointerPose;
                        if (Interactions[i].Changed)
                        {
                            InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentPointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = currentGripPose;
                        if (Interactions[i].Changed)
                        {
                            InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentGripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                        Interactions[i].BoolData = IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.TriggerPress:
                        Interactions[i].BoolData = IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        UpdateIndexFingerData(hand, Interactions[i]);
                        break;
                }
            }
        }

        protected readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        protected void UpdateHandData(MLHand hand)
        {
            // Update joint positions
            var pinky = hand.Pinky;
            ConvertMagicLeapKeyPoint(pinky.Tip, TrackedHandJoint.PinkyTip);
            ConvertMagicLeapKeyPoint(pinky.MCP, TrackedHandJoint.PinkyKnuckle);

            var ring = hand.Ring;
            ConvertMagicLeapKeyPoint(ring.Tip, TrackedHandJoint.RingTip);
            ConvertMagicLeapKeyPoint(ring.MCP, TrackedHandJoint.RingKnuckle);

            var middle = hand.Middle;
            ConvertMagicLeapKeyPoint(middle.Tip, TrackedHandJoint.MiddleTip);
            ConvertMagicLeapKeyPoint(middle.PIP, TrackedHandJoint.MiddleMiddleJoint);
            ConvertMagicLeapKeyPoint(middle.MCP, TrackedHandJoint.MiddleKnuckle);

            var index = hand.Index;
            ConvertMagicLeapKeyPoint(index.Tip, TrackedHandJoint.IndexTip);
            ConvertMagicLeapKeyPoint(index.PIP, TrackedHandJoint.IndexMiddleJoint);
            ConvertMagicLeapKeyPoint(index.MCP, TrackedHandJoint.IndexKnuckle);

            var thumb = hand.Thumb;
            ConvertMagicLeapKeyPoint(thumb.Tip, TrackedHandJoint.ThumbTip);
            ConvertMagicLeapKeyPoint(thumb.IP, TrackedHandJoint.ThumbDistalJoint);
            ConvertMagicLeapKeyPoint(thumb.MCP, TrackedHandJoint.ThumbProximalJoint);

            var wrist = hand.Wrist;
            ConvertMagicLeapKeyPoint(wrist.Center, TrackedHandJoint.Wrist);

            UpdateJointPose(TrackedHandJoint.Palm, hand.Center, Quaternion.identity);

            CoreServices.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);

            // Check pinching action
            var keyPose = hand.KeyPose;
            var confidence = hand.KeyPoseConfidence;

            if (confidence > KeyPoseConfidenceThreshold && (keyPose == MLHandKeyPose.Pinch || keyPose == MLHandKeyPose.Fist))
            {
                IsPinching = true;
            }
            else
            {
                IsPinching = false;
            }
        }

        protected void ConvertMagicLeapKeyPoint(MLKeyPoint keyPoint, TrackedHandJoint joint)
        {
            if (keyPoint.IsValid) {
                var position = keyPoint.Position;
                UpdateJointPose(joint, position, Quaternion.identity);
            }
        }

        protected void UpdateJointPose(TrackedHandJoint joint, Vector3 position, Quaternion rotation)
        {
            var pose = new MixedRealityPose(position, rotation);

            if (!jointPoses.ContainsKey(joint))
            {
                jointPoses.Add(joint, pose);
            }
            else
            {
                jointPoses[joint] = pose;
            }
        }

        private void UpdateIndexFingerData(MLHand hand, MixedRealityInteractionMapping interactionMapping)
        {
            if(jointPoses.TryGetValue(TrackedHandJoint.IndexTip, out var pose)){
                currentIndexPose.Rotation = pose.Rotation;
                currentIndexPose.Position = pose.Position;
            }

            interactionMapping.PoseData = currentIndexPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentIndexPose);
            }
        }
    }
}
