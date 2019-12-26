using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HoloLab.MixedReality.Toolkit.OculusQuestInput
{
    /// <summary>
    /// Manages Oculus Quest Hand Inputs
    /// </summary>
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.Android | SupportedPlatforms.WindowsEditor | SupportedPlatforms.MacEditor | SupportedPlatforms.LinuxEditor,
        "Oculus Quest Hand Input Manager")]
    public class OculusQuestHandInputManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {
        private Dictionary<Handedness, OculusQuestHand> trackedHands = new Dictionary<Handedness, OculusQuestHand>();

        private OVRHand rightHand;
        private OVRSkeleton rightSkeleton;
        private OVRHand leftHand;
        private OVRSkeleton leftSkeleton;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public OculusQuestHandInputManager(
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(inputSystem, name, priority, profile) {
        }

        public override void Enable()
        {
            base.Enable();

            var ovrCameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
            var ovrHands = ovrCameraRig.GetComponentsInChildren<OVRHand>();

            foreach(var ovrHand in ovrHands)
            {
                var skeltonDataProvider = ovrHand as OVRSkeleton.IOVRSkeletonDataProvider;
                var skeltonType = skeltonDataProvider.GetSkeletonType();

                var ovrSkelton = ovrHand.GetComponent<OVRSkeleton>();
                if (ovrSkelton == null)
                {
                    continue;
                }

                switch (skeltonType)
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                        leftHand = ovrHand;
                        leftSkeleton = ovrSkelton;
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                        rightHand = ovrHand;
                        rightSkeleton = ovrSkelton;
                        break;
                }
            }
        }

        public override void Disable()
        {
            base.Disable();

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            foreach (var hand in trackedHands)
            {
                if (hand.Value != null)
                {
                    inputSystem?.RaiseSourceLost(hand.Value.InputSource, hand.Value);
                }
            }

            trackedHands.Clear();
        }

        public override IMixedRealityController[] GetActiveControllers()
        {
            return trackedHands.Values.ToArray<IMixedRealityController>();
        }

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            return (capability == MixedRealityCapability.ArticulatedHand);
        }

        public override void Update()
        {
            base.Update();
            UpdateHand(rightHand, rightSkeleton, Handedness.Right);
            UpdateHand(leftHand, leftSkeleton, Handedness.Left);
        }

        protected void UpdateHand(OVRHand ovrHand, OVRSkeleton ovrSkeleton, Handedness handedness)
        {
            if (ovrHand.IsTracked)
            {
                var hand = GetOrAddHand(handedness);
                hand.UpdateController(ovrHand, ovrSkeleton);
            }
            else
            {
                RemoveHandDevice(handedness);
            }
        }

        private OculusQuestHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                return trackedHands[handedness];
            }

            // Add new hand
            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;
            var inputSource = InputSystem?.RequestNewGenericInputSource($"Oculus Quest {handedness} Hand", pointers, inputSourceType);

            var controller = new OculusQuestHand(TrackingState.Tracked, handedness, inputSource);
            controller.SetupConfiguration(typeof(OculusQuestHand), inputSourceType);

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            InputSystem?.RaiseSourceDetected(controller.InputSource, controller);

            trackedHands.Add(handedness, controller);

            return controller;
        }

        private void RemoveHandDevice(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                var hand = trackedHands[handedness];
                CoreServices.InputSystem?.RaiseSourceLost(hand.InputSource, hand);
                trackedHands.Remove(handedness);
            }
        }

        private void RemoveAllHandDevices()
        {
            foreach (var controller in trackedHands.Values)
            {
                CoreServices.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
            }
            trackedHands.Clear();
        }
    }
}
