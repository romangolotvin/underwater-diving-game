using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Late live-panel reconcile after shop/map LateUpdates.
    /// SwimController runs at -100 so its own LateUpdate would lose to stalls;
    /// this tail wins. Movement/look gate on panel activeInHierarchy, not sticky bits.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class UiGateTail : MonoBehaviour
    {
        public static void Attach(GameObject host)
        {
            if (host == null)
                return;
            if (host.GetComponent<UiGateTail>() == null)
                host.AddComponent<UiGateTail>();
        }

        void LateUpdate()
        {
            var swim = SwimController.Instance;
            if (swim != null)
                swim.ReconcileUiGateLate();
            else
                GameUi.ClearBlocking();
        }
    }
}
