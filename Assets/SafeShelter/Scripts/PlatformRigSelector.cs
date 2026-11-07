using UnityEngine;
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
            desktopRig.SetActive(!IsQuest);
            xrRig.SetActive(IsQuest);
            foreach (var item in questOnlyObjects) if (item != null) item.SetActive(IsQuest);
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
    }
}