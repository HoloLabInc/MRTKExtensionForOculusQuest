using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static OVRSkeleton;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    [MixedRealityController(
        SupportedControllerType.ArticulatedHand,
        new[] { Handedness.Left, Handedness.Right })]
    public class OculusQuestHand : BaseController, IMixedRealityHand
    {
        protected Vector3 CurrentControllerPosition = Vector3.zero;
        protected Quaternion CurrentControllerRotation = Quaternion.identity;
        protected MixedRealityPose CurrentControllerPose = MixedRealityPose.ZeroIdentity;

        private Vector3 currentPointerPosition = Vector3.zero;
        private Quaternion currentPointerRotation = Quaternion.identity;
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        /// <summary>
        /// Pose used by hand ray
        /// </summary>
        public MixedRealityPose HandPointerPose => currentPointerPose;

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        // TODO: Hand mesh
        // private int[] handMeshTriangleIndices = null;
        // private Vector2[] handMeshUVs;

        public OculusQuestHand(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
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
        public void UpdateController(OVRHand hand, OVRSkeleton ovrSkeleton)
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
                currentPointerPose.Position = hand.PointerPose.position;
                currentPointerPose.Rotation = hand.PointerPose.rotation;

                currentGripPose = jointPoses[TrackedHandJoint.Wrist];

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

                UpdatePalm(bones);
            }

            CoreServices.InputSystem?.RaiseHandJointsUpdated(InputSource, ControllerHandedness, jointPoses);

            IsPinching = ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        }

        protected void UpdateBone(OVRBone bone)
        {
            var boneId = bone.Id;
            var boneTransform = bone.Transform;

            if (boneJointMapping.TryGetValue(boneId, out var joint))
            {
                UpdateJointPose(joint, boneTransform.position, boneTransform.rotation);
            }
        }

        protected void UpdatePalm(IList<OVRBone> bones)
        {
            var wristRoot = bones.FirstOrDefault(x => x.Id == BoneId.Hand_WristRoot);
            var middle3 = bones.FirstOrDefault(x => x.Id == BoneId.Hand_Middle3);
            if (wristRoot != null && middle3 != null)
            {
                var wristRootPosition = wristRoot.Transform.position;
                var middle3Position = middle3.Transform.position;
                var palmPosition = Vector3.Lerp(wristRootPosition, middle3Position, 0.5f);
                var palmRotation = wristRoot.Transform.rotation;


                // WARNING THIS CODE IS SUBJECT TO CHANGE WITH THE OCULUS SDK - This fix is a hack to fix broken rotations for palms

                if (ControllerHandedness == Handedness.Left)
                {
                    // Rotate palm 180 on X to flip up
                    palmRotation *= Quaternion.Euler(180f, 0f, 0f);

                    // Rotate palm 90 degrees on y to align z with forward
                    //palmRotation *= Quaternion.Euler(0f, 90f, 0f);
                }
                // Right Up direction is correct
                else
                {
                    // Rotate palm 90 degrees on y to align z with forward
                    //palmRotation *= Quaternion.Euler(0f, 90f, 0f);
                }
                UpdateJointPose(TrackedHandJoint.Palm, palmPosition, palmRotation);
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
