using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;

namespace prvncher.MixedReality.Toolkit.Debug
{

    public class HandJointFollow : MonoBehaviour
    {
        [SerializeField]
        private Handedness trackedHandedness = Handedness.Left;

        [SerializeField]
        private TrackedHandJoint trackedJoint = TrackedHandJoint.Palm;

        void LateUpdate()
        {
            IMixedRealityHand hand = GetController(trackedHandedness) as IMixedRealityHand;
            if (hand == null || !hand.TryGetJoint(trackedJoint, out MixedRealityPose pose))
            {
                SetChildrenActive(false);
                return;
            }
            SetChildrenActive(true);
            transform.position = pose.Position;
            transform.rotation = pose.Rotation;
        }

        private void SetChildrenActive(bool isActive)
        {
            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(isActive);
            }
        }

        private static IMixedRealityController GetController(Handedness handedness)
        {
            foreach (IMixedRealityController c in CoreServices.InputSystem.DetectedControllers)
            {
                if (c.ControllerHandedness.IsMatch(handedness))
                {
                    return c;
                }
            }
            return null;
        }
    }

}