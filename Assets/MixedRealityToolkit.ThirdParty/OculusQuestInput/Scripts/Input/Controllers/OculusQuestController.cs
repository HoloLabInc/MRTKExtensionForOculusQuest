using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using prvncher.MixedReality.Toolkit.Config;
using UnityEngine;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class OculusQuestController : BaseController, IMixedRealityHand
    {
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;

        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        #region AvatarHandReferences
        private GameObject handRoot = null;
        private GameObject handGrip = null;

        private GameObject handIndex1 = null;
        private GameObject handIndex2 = null;
        private GameObject handIndex3 = null;

        private GameObject handMiddle1 = null;
        private GameObject handMiddle2 = null;
        private GameObject handMiddle3 = null;

        private GameObject handRing1 = null;
        private GameObject handRing2 = null;
        private GameObject handRing3 = null;

        private GameObject handPinky0 = null;
        private GameObject handPinky1 = null;
        private GameObject handPinky2 = null;
        private GameObject handPinky3 = null;

        private GameObject handThumb1 = null;
        private GameObject handThumb2 = null;
        private GameObject handThumb3 = null;
        #endregion

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

        public override MixedRealityInteractionMapping[] DefaultLeftHandedInteractions => DefaultInteractions;

        public override MixedRealityInteractionMapping[] DefaultRightHandedInteractions => DefaultInteractions;

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

            UpdateJointPoses();

            if (MRTKOculusConfig.Instance.RenderAvatarHandsInsteadOfController)
            {
                // If rendering avatar hands, we can query the avatar hands for pointer data

            }
            else
            {
                // If not rendering avatar hands, pointer pose is not available, so we approximate it
                if (IsPositionAvailable)
                {
                    currentPointerPose.Position = currentGripPose.Position = worldPosition;
                }

                if (IsRotationAvailable)
                {
                    currentPointerPose.Rotation = currentGripPose.Rotation = worldRotation;
                }
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

        private bool InitializeAvatarHandReferences()
        {
            if (!MRTKOculusConfig.Instance.RenderAvatarHandsInsteadOfController) return false;

            string handSignififer = ControllerHandedness == Handedness.Left ? "l" : "r";
            string handStructure = "hands:b_" + handSignififer;

            string handRootString = handStructure + "_hand";

            handRoot = GameObject.Find(handRootString);

            // With no root, no use in looking up other joints
            if (handRoot == null) return false;

            // If we have a hand root match, we look up all other hand joints

            // Index
            string indexString = handStructure + "_index";

            string indexString1 = indexString + "1";
            handIndex1 = GameObject.Find(indexString1);

            string indexString2 = indexString + "2";
            handIndex2 = GameObject.Find(indexString2);

            string indexString3 = indexString + "3";
            handIndex3 = GameObject.Find(indexString3);

            // Middle
            string middleString = handStructure + "_middle";

            string middleString1 = middleString + "1";
            handMiddle1 = GameObject.Find(middleString1);

            string middleString2 = middleString + "2";
            handMiddle2 = GameObject.Find(middleString2);

            string middleString3 = middleString + "3";
            handMiddle3 = GameObject.Find(middleString3);

            // Pinky
            string pinkyString = handStructure + "_pinky";

            string pinkyString0 = pinkyString + "0";
            handPinky0 = GameObject.Find(pinkyString0);

            string pinkyString1 = pinkyString + "1";
            handPinky1 = GameObject.Find(pinkyString1);

            string pinkyString2 = pinkyString + "2";
            handPinky2 = GameObject.Find(pinkyString2);

            string pinkyString3 = pinkyString + "3";
            handPinky3 = GameObject.Find(pinkyString3);

            // Ring
            string ringString = handStructure + "_ring";

            string ringString1 = ringString + "1";
            handRing1 = GameObject.Find(ringString1);

            string ringString2 = ringString + "2";
            handRing2 = GameObject.Find(ringString2);

            string ringString3 = ringString + "3";
            handRing3 = GameObject.Find(ringString3);

            // Thumb
            string thumbString = handStructure + "_thumb";

            string thumbString1 = thumbString + "1";
            handThumb1 = GameObject.Find(thumbString1);

            string thumbString2 = thumbString + "2";
            handThumb2 = GameObject.Find(thumbString2);

            string thumbString3 = thumbString + "3";
            handThumb3 = GameObject.Find(thumbString3);

            return true;
        }

        private void UpdateJointPoses()
        {
            if (MRTKOculusConfig.Instance.RenderAvatarHandsInsteadOfController)
            {
                // See https://developer.oculus.com/documentation/unity/as-avatars-gsg-unity-mobile/
                if (ControllerHandedness == Handedness.Left)
                {

                }
                else
                {

                }
            }
            else
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
