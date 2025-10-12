using System.Threading;
using Cysharp.Threading.Tasks;
using Xprees.Npc.StateMachine.States;

namespace Xprees.Npc.StateMachine
{
    public interface INpcStateMachine
    {
        NpcStateBase CurrentState { get; set; }
        UniTask SwitchState(NpcStateBase newState, CancellationToken cancellationToken = default);
    }
}