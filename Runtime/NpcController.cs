using System.Threading;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;
using Xprees.Npc.Animation;
using Xprees.Npc.Extensions;
using Xprees.Npc.Navigation;
using Xprees.Npc.RuntimeAnchors.Reference;
using Xprees.Npc.StateMachine;
using Xprees.Npc.StateMachine.States;
using Xprees.RuntimeAnchors;
using Xprees.Variables.Reference.Primitive;

namespace Xprees.Npc
{
    public class NpcController : MonoBehaviour, INpcStateMachine, INavigable, INpcAnimationControllerSource
    {
        [Header("Player")]
        public TransformAnchor playerTransformAnchor;

        [Header("Navigation")]
        [Required]
        public NavMeshAgent agent;

        [Tooltip("The route the NPC will follow")]
        public RouteAnchorReference routeAnchor = new();

        [Space]
        [Range(0.01f, 1f)]
        [Tooltip("The tolerance used to determine if the agents is moving.")]
        [SerializeField] private float movementTolerance = .3f;

        [Foldout("Behaviour Parameters")]
        [Tooltip("If true, the NPC will follow the route on start, otherwise after the next switch to idle state.")]
        [SerializeField] private bool dontGoOnRouteOnStart;

        [Foldout("Behaviour Parameters")]
        [SerializeField] private float waitForInteractionTimeout = 10;

        [Foldout("Behaviour Parameters")]
        [SerializeField] private float waitAfterMoveToArrived = 15;

        [Header("Variables")]
        [Tooltip("Variable connecting attached interaction to NPC. Used for enabling/disabling the interaction based on NPC state.")]
        [SerializeField] private BoolReference isDisabledInteraction = new();

        [Foldout("Runtime State")]
        [ReadOnly]
        [SerializeField] private string currentStateName;

        /// For saving where was going last time
        [field: Foldout("Runtime State")]
        [field: ReadOnly]
        [field: SerializeField] internal int CurrentWaypointIndex { get; set; }

        public bool IsMoving => !agent.isStopped && agent.IsMoving(movementTolerance);
        public bool IsInteracting => CurrentState is InteractionState;
        public bool IsTalking => CurrentState is InteractionState;

        public Vector3 Velocity => agent.velocity;
        public float Speed => agent.speed;

        #region State Machine Management

        private NpcStateBase currentState;
        private CancellationTokenSource _cts;

        public NpcStateBase CurrentState
        {
            get => currentState;
            set
            {
                currentStateName = value?.GetType().Name;
                currentState = value;
            }
        }

        private async UniTask InitNpcBehaviourStateMachine(CancellationToken cancellationToken = default)
        {
            CurrentState = new IdleState(this)
            {
                DontGoOnRoute = dontGoOnRouteOnStart,
                Route = routeAnchor?.Value,
            };
            await CurrentState.EnterState(cancellationToken)
                .SuppressCancellationThrow();
        }

        public async UniTask SwitchState(NpcStateBase newState, CancellationToken cancellationToken = default)
        {
            ApplySettingsToNewState(newState);
            await CurrentState.SwitchState(newState, cancellationToken)
                .SuppressCancellationThrow();
        }

        private void ApplySettingsToNewState(NpcStateBase state)
        {
            switch (state)
            {
                case MoveToState moveToState:
                    moveToState.WaitTimeAfterArrive = waitAfterMoveToArrived;
                    break;
                case InteractionState interactionState:
                    interactionState.InteractionTimeout = waitForInteractionTimeout;
                    break;
                case IdleState idleState:
                    idleState.Route = routeAnchor?.Value;
                    idleState.CanBeInterrupted = true; // Idle always can be interrupted
                    break;
            }
        }

        #endregion

        #region Interaction

        public bool IsDisabledInteraction
        {
            get => isDisabledInteraction?.Value ?? false;
            set
            {
                if (isDisabledInteraction == null) return;
                isDisabledInteraction.Value = value;
            }
        }

        public bool InteractedWithPlayer { get; private set; }

        public async void PrepareFoInteraction()
        {
            if (isDisabledInteraction.Value) return; // ignore player
            await SwitchState(new InteractionState(this), _cts.Token);
        }

        public async void OnInteraction(GameObject interactable)
        {
            if (interactable == null || IsDisabledInteraction) return;
            if (!IsInteractionOfThisNpc(interactable)) return;

            InteractedWithPlayer = true;
            if (IsInteracting) return;
            await SwitchState(new InteractionState(this), _cts.Token);
        }

        private bool IsInteractionOfThisNpc(GameObject interactable) => interactable == gameObject;

        public async void AfterInteraction()
        {
            InteractedWithPlayer = false;
            if (IsInteracting) await SwitchState(new IdleState(this, CurrentWaypointIndex), _cts.Token);
        }

        #endregion

        public void SetDestination(Transform destination) => SetDestination(destination.position);

        public async void SetDestination(Vector3 destination) =>
            await SwitchState(new MoveToState(this, destination)
            {
                CanBeInterrupted = false,
                WaitUntilCanBeInterrupted = true,
            }, _cts.Token);

        #region Event Functions

        private void OnEnable()
        {
            CurrentWaypointIndex = 0;
            _cts = new CancellationTokenSource();
        }

        private async void OnDisable()
        {
            if (CurrentState == null) return;
            CurrentState.ForceExitState();
            await CurrentState.ExitState(_cts.Token);
            _cts?.Cancel();
            _cts?.Dispose();
        }

        private async void Start() => await InitNpcBehaviourStateMachine(_cts.Token)
            .SuppressCancellationThrow();

        private async void Update() => await CurrentState.UpdateState(_cts.Token)
            .SuppressCancellationThrow();

        #endregion

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            if (agent == null) return;
            DrawAgentDestinationGizmo();
            DrawAgentPathGizmo();
        }

        private void DrawAgentDestinationGizmo() => Gizmos.DrawIcon(agent.destination + Vector3.up, "destination.png");

        private void DrawAgentPathGizmo()
        {
            if (agent.path.corners.Length < 2) return;

            for (var i = 0; i < agent.path.corners.Length - 1; i++)
            {
                Gizmos.color = Color.red;
                var path = agent.path;
                Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
            }
        }
#endif
    }
}