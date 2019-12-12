using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace HoloLab.MixedReality.Toolkit.MagicLeapInput
{
    /// <summary>
    /// Manages Magic Leap Hand Inputs
    /// </summary>
    [MixedRealityDataProvider(
        typeof(IMixedRealityInputSystem),
        SupportedPlatforms.Lumin | SupportedPlatforms.WindowsEditor | SupportedPlatforms.MacEditor | SupportedPlatforms.LinuxEditor,
        "Magic Leap Hand Input Manager")]
    public class MagicLeapHandInputManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {
        private MLKeyPointFilterLevel keyPointFilterLevel = MLKeyPointFilterLevel.ExtraSmoothed;
        private MLPoseFilterLevel poseFilterLevel = MLPoseFilterLevel.ExtraRobust;

        private Dictionary<Handedness, MagicLeapHand> trackedHands = new Dictionary<Handedness, MagicLeapHand>();

        private bool mlInputStarted = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="registrar">The <see cref="IMixedRealityServiceRegistrar"/> instance that loaded the data provider.</param>
        /// <param name="inputSystem">The <see cref="Microsoft.MixedReality.Toolkit.Input.IMixedRealityInputSystem"/> instance that receives data from this provider.</param>
        /// <param name="name">Friendly name of the service.</param>
        /// <param name="priority">Service priority. Used to determine order of instantiation.</param>
        /// <param name="profile">The service's configuration profile.</param>
        public MagicLeapHandInputManager(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(registrar, inputSystem, name, priority, profile) {
        }

        public override void Disable()
        {
            StopMLInput();

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

        private void StartMLInput()
        {
            MLResult result = MLHands.Start();
            if (!result.IsOk)
            {
                Debug.LogError($"[MagicLeapHandInputManager] Failed starting MLHands. {result}");
                return;
            }

            // Enable all KeyPoses
            var keyPoseTypes = Enum.GetValues(typeof(MLHandKeyPose)).Cast<MLHandKeyPose>().ToArray();
            bool status = MLHands.KeyPoseManager.EnableKeyPoses(keyPoseTypes, true, true);
            if (!status)
            {
                Debug.LogError("[MagicLeapHandInputManager] Failed enabling tracked KeyPoses.");
                return;
            }

            MLHands.KeyPoseManager.SetKeyPointsFilterLevel(keyPointFilterLevel);
            MLHands.KeyPoseManager.SetPoseFilterLevel(poseFilterLevel);
        }

        private void StopMLInput()
        {
            if (MLInput.IsStarted)
            {
                // Stop KeyPose detection
                var keyPoseTypes = Enum.GetValues(typeof(MLHandKeyPose)).Cast<MLHandKeyPose>().ToArray();
                MLHands.KeyPoseManager.EnableKeyPoses(keyPoseTypes, false, true);

                MLInput.Stop();
            }
        }

        public override void Update()
        {
            // MLInput.Start() should be called after Start() in MonoBehaviour is called
            if (!mlInputStarted)
            {
                StartMLInput();
                mlInputStarted = true;
            }

            if (MLHands.IsStarted)
            {
                UpdateHand(MLHands.Left, Handedness.Left);
                UpdateHand(MLHands.Right, Handedness.Right);
            }
        }

        protected void UpdateHand(MLHand mlHand, Handedness handedness)
        {
            if (mlHand.IsVisible)
            {
                var hand = GetOrAddHand(handedness);
                hand.UpdateController(mlHand);
            }
            else
            {
                RemoveHandDevice(handedness);
            }
        }

        private MagicLeapHand GetOrAddHand(Handedness handedness)
        {
            if (trackedHands.ContainsKey(handedness))
            {
                return trackedHands[handedness];
            }

            // Add new hand
            var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
            var inputSourceType = InputSourceType.Hand;
            var inputSource = InputSystem?.RequestNewGenericInputSource($"Magic Leap {handedness} Hand", pointers, inputSourceType);

            var controller = new MagicLeapHand(TrackingState.Tracked, handedness, inputSource);
            controller.SetupConfiguration(typeof(MagicLeapHand), inputSourceType);

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
