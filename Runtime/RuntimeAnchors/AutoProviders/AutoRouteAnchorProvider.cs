using Xprees.Npc.Navigation;
using Xprees.RuntimeAnchors.Base;

namespace Xprees.Npc.RuntimeAnchors.AutoProviders
{
    public class AutoRouteAnchorProvider : AutoAnchorProviderBase<Route>
    {
        protected override Route GetAutomaticallyAnchorComponent() => GetComponent<Route>();
    }
}