using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static OVRSkeleton;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class OculusQuestHand : BaseController, IMixedRealityHand
    {
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        /// <summary>
        /// Pose used by hand ray
        /// </summary>
        public MixedRealityPose HandPointerPose => currentPointerPose;

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        // Use Kalman filters to improve palm and index positions, as they drive many interactions
        private KalmanFilterVector3 palmFilter = new KalmanFilterVector3();
        private KalmanFilterVector3 indexTipFilter = new KalmanFilterVector3();

        // TODO: Hand mesh
        // private int[] handMeshTriangleIndices = null;
        // private Vector2[] handMeshUVs;

        public OculusQuestHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {
            palmFilter.Reset();
            indexTipFilter.Reset();
        }

        public override MixedRealityInteractionMapping[] DefaultInteractions => new[]
        {
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer, new MixedRealityInputAction(4, "Pointer Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip, new MixedRealityInputAction(3, "Grip Pose", AxisType.SixDof)),
            new MixedRealityInteractionMapping(2, "Select", AxisType.Digital, DeviceInputType.Select, new MixedRealityInputAction(1, "Select", AxisType.Digital)),
            new MixedRealityInteractionMapping(3, "Grab", AxisType.SingleAxis, DeviceInputType.TriggerPress, new MixedRealityInputAction(7, "Grip Press", AxisType.SingleAxis)),
            new MixedRealityInteractionMapping(4, "Index Finger Pose", AxisType.SixDof, DeviceInputType.IndexFinger,  new MixedRealityInputAction(13, "Index Finger Pose", AxisType.SixDof)),
        };

        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

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
                if (!TryGetJoint(TrackedHandJoint.Palm, out var palmPose)) return false;

                Transform cameraTransform = CameraCache.Main.transform;

                Vector3 projectedPalmUp = Vector3.ProjectOnPlane(-palmPose.Up, cameraTransform.up);
                
                // We check if the palm forward is roughly in line with the camera lookAt
                return Vector3.Dot(cameraTransform.forward, projectedPalmUp) > 0.3f;
            }
        }

        protected bool IsPinching { set; get; }

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(OVRHand hand, OVRSkeleton ovrSkeleton, Transform trackingOrigin)
        {
            if (!Enabled || hand == null || ovrSkeleton == null)
            {
                return;
            }

            UpdateHandData(hand, ovrSkeleton);

            IsPositionAvailable = hand.IsTracked;

            bool isTracked = hand.IsTracked;
            IsPositionAvailable = IsRotationAvailable = isTracked;

            if (isTracked)
            {
                // Leverage Oculus Platform Hand Ray - instead of simulating it in a crummy way
                currentPointerPose.Position = trackingOrigin.TransformPoint(hand.PointerPose.position);

                Vector3 pointerForward = trackingOrigin.TransformDirection(hand.PointerPose.forward);
                Vector3 pointerUp = trackingOrigin.TransformDirection(hand.PointerPose.up);

                currentPointerPose.Rotation = Quaternion.LookRotation(pointerForward, pointerUp);

                currentGripPose = jointPoses[TrackedHandJoint.Palm];

                CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, currentGripPose);
            }

            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = currentPointerPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentPointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = currentGripPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, currentGripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                        Interactions[i].BoolData = IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.TriggerPress:
                        Interactions[i].BoolData = IsPinching;

                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        UpdateIndexFingerData(Interactions[i]);
                        break;
                }
            }
        }

        #region HandJoints
        protected readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        protected readonly Dictionary<BoneId, TrackedHandJoint> boneJointMapping = new Dictionary<BoneId, TrackedHandJoint>()
        {
            { BoneId.Hand_Thumb1, TrackedHandJoint.ThumbMetacarpalJoint },
            { BoneId.Hand_Thumb2, TrackedHandJoint.ThumbProximalJoint },
            { BoneId.Hand_Thumb3, TrackedHandJoint.ThumbDistalJoint },
            { BoneId.Hand_ThumbTip, TrackedHandJoint.ThumbTip },
            { BoneId.Hand_Index1, TrackedHandJoint.IndexKnuckle },
            { BoneId.Hand_Index2, TrackedHandJoint.IndexMiddleJoint },
            { BoneId.Hand_Index3, TrackedHandJoint.IndexDistalJoint },
            { BoneId.Hand_IndexTip, TrackedHandJoint.IndexTip },
            { BoneId.Hand_Middle1, TrackedHandJoint.MiddleKnuckle },
            { BoneId.Hand_Middle2, TrackedHandJoint.MiddleMiddleJoint },
            { BoneId.Hand_Middle3, TrackedHandJoint.MiddleDistalJoint },
            { BoneId.Hand_MiddleTip, TrackedHandJoint.MiddleTip },
            { BoneId.Hand_Ring1, TrackedHandJoint.RingKnuckle },
            { BoneId.Hand_Ring2, TrackedHandJoint.RingMiddleJoint },
            { BoneId.Hand_Ring3, TrackedHandJoint.RingDistalJoint },
            { BoneId.Hand_RingTip, TrackedHandJoint.RingTip },
            { BoneId.Hand_Pinky1, TrackedHandJoint.PinkyKnuckle },
            { BoneId.Hand_Pinky2, TrackedHandJoint.PinkyMiddleJoint },
            { BoneId.Hand_Pinky3, TrackedHandJoint.PinkyDistalJoint },
            { BoneId.Hand_PinkyTip, TrackedHandJoint.PinkyTip },
            { BoneId.Hand_WristRoot, TrackedHandJoint.Wrist },
        };

        protected void UpdateHandData(OVRHand ovrHand, OVRSkeleton ovrSkeleton)
        {
            if (ovrSkeleton != null)
            {
                var bones = ovrSkeleton.Bones;
                foreach (var bone in bones)
                {
                    UpdateBone(bone);
                }

                UpdatePalm();
            }

            CoreServices.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);


            if (IsPinching)
            {
                // If we are already pinching, we make the pinch a bit sticky
                IsPinching = ovrHand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > 0.85f;
            }
            else
            {
                // If not yet pinching, only consider pinching if finger confidence is high
                IsPinching = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index)
                             && ovrHand.GetFingerConfidence(OVRHand.HandFinger.Index) == OVRHand.TrackingConfidence.High;
            }
        }

        protected void UpdateBone(OVRBone bone)
        {
            var boneId = bone.Id;
            var boneTransform = bone.Transform;

            if (boneJointMapping.TryGetValue(boneId, out var joint))
            {
                Quaternion boneRotation = bone.Transform.rotation;

                // WARNING THIS CODE IS SUBJECT TO CHANGE WITH THE OCULUS SDK - This fix is a hack to fix broken and inconsistent rotations for hands
                if (ControllerHandedness == Handedness.Left)
                {
                    // Rotate palm 180 on X to flip up
                    boneRotation *= Quaternion.Euler(180f, 0f, 0f);

                    // Rotate palm 90 degrees on y to align x with right
                    boneRotation *= Quaternion.Euler(0f, -90, 0f);
                }
                else
                {
                    // Right Up direction is correct

                    // Rotate palm 90 degrees on y to align x with right
                    boneRotation *= Quaternion.Euler(0f, 90f, 0f);
                }

                UpdateJointPose(joint, boneTransform.position, boneRotation);
            }
        }

        protected void UpdatePalm()
        {
            bool hasMiddleKnuckle = TryGetJoint(TrackedHandJoint.MiddleKnuckle, out var middleKnucklePose);
            bool hasWrist = TryGetJoint(TrackedHandJoint.Wrist, out var wristPose);

            if (hasMiddleKnuckle && hasWrist)
            {
                Vector3 wristRootPosition = wristPose.Position;
                Vector3 middle3Position = middleKnucklePose.Position;

                Vector3 palmPosition = Vector3.Lerp(wristRootPosition, middle3Position, 0.5f);
                Quaternion palmRotation = wristPose.Rotation;

                UpdateJointPose(TrackedHandJoint.Palm, palmPosition, palmRotation);
            }
        }

        protected void UpdateJointPose(TrackedHandJoint joint, Vector3 position, Quaternion rotation)
        {
            Vector3 jointPosition = position;
            
            // TODO Figure out kalman filter coefficients to get good quality smoothing
            /*
            if (joint == TrackedHandJoint.IndexTip)
            {
                jointPosition = indexTipFilter.Update(position);
            }
            else if (joint == TrackedHandJoint.Palm)
            {
                jointPosition = palmFilter.Update(position);
            }
            */

            MixedRealityPose pose = new MixedRealityPose(jointPosition, rotation);
            if (!jointPoses.ContainsKey(joint))
            {
                jointPoses.Add(joint, pose);
            }
            else
            {
                jointPoses[joint] = pose;
            }
        }

        private void UpdateIndexFingerData(MixedRealityInteractionMapping interactionMapping)
        {
            if (jointPoses.TryGetValue(TrackedHandJoint.IndexTip, out var pose))
            {
                currentIndexPose.Rotation = pose.Rotation;
                currentIndexPose.Position = pose.Position;
            }

            interactionMapping.PoseData = currentIndexPose;

            // If our value changed raise it.
            if (interactionMapping.Changed)
            {
                // Raise input system Event if it enabled
                CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, interactionMapping.MixedRealityInputAction, currentIndexPose);
            }
        }
        #endregion
    }
}
