using UnityEngine;

namespace UnderwaterDiving
{
    /// <summary>
    /// Late absolute UI-mask reconcile after shop/map LateUpdates.
    /// SwimController runs at -100 so its own LateUpdate would lose to stalls;
    /// this tail wins and keeps WASD/L/M alive when every panel is closed.
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
