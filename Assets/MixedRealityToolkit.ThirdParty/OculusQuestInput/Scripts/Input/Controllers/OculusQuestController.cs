using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace prvncher.MixedReality.Toolkit.OculusQuestInput
{
    [MixedRealityController(SupportedControllerType.OculusTouch, new[] { Handedness.Left, Handedness.Right })]
    public class OculusQuestController : BaseController
    {
        private MixedRealityPose currentPointerPose = MixedRealityPose.ZeroIdentity;


        private MixedRealityPose currentIndexPose = MixedRealityPose.ZeroIdentity;
        private MixedRealityPose currentGripPose = MixedRealityPose.ZeroIdentity;

        private const float cTriggerDeadZone = 0.1f;

        // TODO: Hand mesh
        // private int[] handMeshTriangleIndices = null;
        // private Vector2[] handMeshUVs;

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
            new MixedRealityInteractionMapping(0, "Spatial Pointer", AxisType.SixDof, DeviceInputType.SpatialPointer),
            new MixedRealityInteractionMapping(1, "Spatial Grip", AxisType.SixDof, DeviceInputType.SpatialGrip),
            new MixedRealityInteractionMapping(2, "Grip Press", AxisType.SingleAxis, DeviceInputType.TriggerPress),
            new MixedRealityInteractionMapping(3, "Trigger Position", AxisType.SingleAxis, DeviceInputType.Trigger),
            new MixedRealityInteractionMapping(4, "Trigger Touch", AxisType.Digital, DeviceInputType.TriggerTouch),
            new MixedRealityInteractionMapping(5, "Trigger Press (Select)", AxisType.Digital, DeviceInputType.Select),
            new MixedRealityInteractionMapping(6, "Touchpad Position", AxisType.DualAxis, DeviceInputType.Touchpad),
            new MixedRealityInteractionMapping(7, "Touchpad Touch", AxisType.Digital, DeviceInputType.TouchpadTouch),
            new MixedRealityInteractionMapping(8, "Touchpad Press", AxisType.Digital, DeviceInputType.TouchpadPress),
            new MixedRealityInteractionMapping(9, "Menu Press", AxisType.Digital, DeviceInputType.Menu),
            new MixedRealityInteractionMapping(10, "Thumbstick Position", AxisType.DualAxis, DeviceInputType.ThumbStick),
            new MixedRealityInteractionMapping(11, "Thumbstick Press", AxisType.Digital, DeviceInputType.ThumbStickPress),
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

            IsPositionAvailable = OVRInput.GetControllerPositionTracked(ovrController);
            IsRotationAvailable = OVRInput.GetControllerPositionTracked(ovrController);

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
                }
            }
        }
    }
}
