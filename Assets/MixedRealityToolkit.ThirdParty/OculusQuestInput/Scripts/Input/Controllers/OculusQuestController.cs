using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class OculusQuestController : BaseController, IMixedRealityHand
    {
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        protected readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        private const float cTriggerDeadZone = 0.1f;

        public OculusQuestController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions)
        {
        }

        /// <summary>
        /// The Windows Mixed Reality Controller default interactions.
        /// </summary>
        /// <remarks>A single interaction mapping works for both left and right controllers.</remarks>
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

        /// <summary>
        /// Update the controller data from the provided platform state
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform</param>
        public void UpdateController(OVRCameraRig ovrRigRoot, OVRInput.Controller ovrController)
        {
            if (!Enabled || ovrRigRoot == null)
            {
                return;
            }

            IsPositionAvailable = OVRInput.GetControllerPositionValid(ovrController);
            IsRotationAvailable = OVRInput.GetControllerOrientationValid(ovrController);

            Transform playSpaceTransform = ovrRigRoot.transform;

            // Update transform
            Vector3 localPosition = OVRInput.GetLocalControllerPosition(ovrController);
            Vector3 worldPosition = playSpaceTransform.TransformPoint(localPosition);
            // Debug.Log("Controller " + Handedness + " - local: " + localPosition + " - world: " + worldPosition);

            Quaternion localRotation = OVRInput.GetLocalControllerRotation(ovrController);
            Quaternion worldRotation = playSpaceTransform.rotation * localRotation;

            // Update velocity
            Vector3 localVelocity = OVRInput.GetLocalControllerVelocity(ovrController);
            Velocity = playSpaceTransform.TransformDirection(localVelocity);

            Vector3 localAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(ovrController);
            AngularVelocity = playSpaceTransform.TransformDirection(localAngularVelocity);

            if (IsPositionAvailable)
            {
                currentPointerPose.Position = currentGripPose.Position = worldPosition;
            }

            if (IsRotationAvailable)
            {
                currentPointerPose.Rotation = currentGripPose.Rotation = worldRotation;
            }

            // Todo: Complete touch controller mapping

            bool isTriggerPressed = false;
            if (ControllerHandedness == Handedness.Left)
            {
                isTriggerPressed = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > cTriggerDeadZone;
            }
            else
            {
                isTriggerPressed = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger) > cTriggerDeadZone;
            }

            UpdateJointPoses();

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
                        Interactions[i].BoolData = isTriggerPressed;

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
                        Interactions[i].BoolData = isTriggerPressed;

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

        private void UpdateJointPoses()
        {
            // While we can get pretty much everything done with just the grip pose, we simulate hand sizes for bounds calculations

            // Index
            Vector3 fingerTipPos = currentGripPose.Position + currentGripPose.Rotation * Vector3.forward * 0.1f;
            UpdateJointPose(TrackedHandJoint.IndexTip, fingerTipPos, currentGripPose.Rotation);

            // Handed directional offsets
            Vector3 inWardVector;
            if (ControllerHandedness == Handedness.Left)
            {
                inWardVector = currentGripPose.Rotation * Vector3.right;
            }
            else
            {
                inWardVector = currentGripPose.Rotation * -Vector3.right;
            }

            // Thumb
            Vector3 thumbPose = currentGripPose.Position + inWardVector * 0.04f;
            UpdateJointPose(TrackedHandJoint.ThumbTip, thumbPose, currentGripPose.Rotation);
            UpdateJointPose(TrackedHandJoint.ThumbMetacarpalJoint, thumbPose, currentGripPose.Rotation);
            UpdateJointPose(TrackedHandJoint.ThumbDistalJoint, thumbPose, currentGripPose.Rotation);

            // Pinky
            Vector3 pinkyPose = currentGripPose.Position - inWardVector * 0.03f;
            UpdateJointPose(TrackedHandJoint.PinkyKnuckle, pinkyPose, currentGripPose.Rotation);

            // Palm
            UpdateJointPose(TrackedHandJoint.Palm, currentGripPose.Position, currentGripPose.Rotation);

            // Wrist
            Vector3 wristPose = currentGripPose.Position - currentGripPose.Rotation * Vector3.forward * 0.05f;
            UpdateJointPose(TrackedHandJoint.Palm, wristPose, currentGripPose.Rotation);
        }

        protected void UpdateJointPose(TrackedHandJoint joint, Vector3 position, Quaternion rotation)
        {
            MixedRealityPose pose = new MixedRealityPose(position, rotation);
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

        public bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose)
        {
            if (jointPoses.TryGetValue(joint, out pose))
            {
                return true;
            }
            pose = currentGripPose;
            return true;
        }
    }
}
