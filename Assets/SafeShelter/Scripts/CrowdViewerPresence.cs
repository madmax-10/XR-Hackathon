using UnityEngine;
using UnityEngine.XR;
namespace SafeShelter
{
    public sealed class CrowdViewerPresence : MonoBehaviour
    {
        [SerializeField] CrowdDirector director;
        [SerializeField] Transform viewHead;
        float next;
        void Update()
        {
            if (!PlatformRigSelector.IsQuest || director == null || viewHead == null || Time.unscaledTime < next) return;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!head.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) || !tracked) return;
            next = Time.unscaledTime + .125f;
            director.ReportViewer(viewHead.position);
        }
    }
}
