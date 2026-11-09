using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Management;
namespace SafeShelter
{
    [DefaultExecutionOrder(-10000)]
    public sealed class PlatformRigSelector : MonoBehaviour
    {
        [SerializeField] GameObject desktopRig;
        [SerializeField] GameObject xrRig;
        [SerializeField] GameObject[] questOnlyObjects;
        public static bool IsQuest => !Application.isEditor && Application.platform == RuntimePlatform.Android;
        void Awake()
        {
            // The gravity provider casts from the tracked head. On Quest its first Update can
            // run before the headset has supplied a pose, causing the origin to fall below the floor.
            GravityProvider gravity = null;
            bool resumeGravity = false;
            if (IsQuest)
            {
                gravity = xrRig.GetComponentInChildren<GravityProvider>(true);
                if (gravity != null && gravity.enabled)
                {
                    resumeGravity = true;
                    gravity.enabled = false;
                }
            }
            desktopRig.SetActive(!IsQuest);
            xrRig.SetActive(IsQuest);
            foreach (var item in questOnlyObjects) if (item != null) item.SetActive(IsQuest);
            if (resumeGravity)
                StartCoroutine(EnableGravityWhenTracked(gravity));
            if (!IsQuest && XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null)
            {
                var manager = XRGeneralSettings.Instance.Manager;
                if (manager.isInitializationComplete)
                {
                    manager.StopSubsystems();
                    manager.DeinitializeLoader();
                }
            }
        }

        IEnumerator EnableGravityWhenTracked(GravityProvider gravity)
        {
            int trackedFrames = 0;
            while (trackedFrames < 2)
            {
                yield return null;
                var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked)
                    trackedFrames++;
                else
                    trackedFrames = 0;
            }
            gravity.enabled = true;
        }
    }
}
