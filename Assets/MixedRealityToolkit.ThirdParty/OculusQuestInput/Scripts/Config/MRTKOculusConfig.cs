using UnityEngine;

namespace prvncher.MixedReality.Toolkit.Config
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [CreateAssetMenu(menuName = "MRTK-Quest/MRTK-OculusConfig")]
    public class MRTKOculusConfig : ScriptableObject
    {
        private static MRTKOculusConfig instance;
        public static MRTKOculusConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<MRTKOculusConfig>("MRTK-OculusConfig");

                    if (instance == null)
                    {
                        UnityEngine.Debug.LogError("Failure to detect MRTK-OculusConfig. Please create an instance using the asset context menu, and place it in any Resources folder.");
                    }
                }
                return instance;
            }
        }
        [Header("Config")]
        [SerializeField]
        [Tooltip("Using avatar hands requires a local avatar prefab. Failure to provide one will result in nothing being displayed. \n\n" +
                 "Note: In order to render avatar hands, you will need to set an app id in OvrAvatarSettings. Any number will do, but it needs to be set.")]
        private bool renderAvatarHandsInsteadOfControllers = true;

        /// <summary>
        /// Using avatar hands requires a local avatar prefab. Failure to provide one will result in nothing being displayed.
        /// </summary>
        public bool RenderAvatarHandsInsteadOfController => renderAvatarHandsInsteadOfControllers;

        [Header("Prefab references")]
        [SerializeField]
        [Tooltip("Prefab reference for OVRCameraRig to load, if none are found in scene.")]
        private OVRCameraRig ovrCameraRigPrefab = null;

        /// <summary>
        /// Prefab reference for OVRCameraRig to load, if none are found in scene.
        /// </summary>
        public OVRCameraRig OVRCameraRigPrefab => ovrCameraRigPrefab;

        [SerializeField]
        [Tooltip("Prefab reference for LocalAvatar to load, if none are found in scene.")]
        private GameObject localAvatarPrefab = null;

        /// <summary>
        /// Prefab reference for LocalAvatar to load, if none are found in scene.
        /// </summary>
        public GameObject LocalAvatarPrefab => localAvatarPrefab;
    }
}
