using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPersonMovement
{
    [DisallowMultipleComponent]
    public sealed class UnityInputSystemLocomotionInputSource : MonoBehaviour, IBasicLocomotionInputSource
    {
        [SerializeField] InputActionAsset inputActions;
        [SerializeField] string actionMapName = "Player";
        [SerializeField] string moveActionName = "Move";
        [SerializeField] string lookActionName = "Look";
        [SerializeField] string runActionName = "";
        [SerializeField] bool enableInputOnEnable = true;

        InputAction moveAction;
        InputAction lookAction;
        InputAction runAction;

        public InputActionAsset InputActions { get => inputActions; set { inputActions = value; ClearCachedActions(); } }
        public string ActionMapName { get => actionMapName; set { actionMapName = value; ClearCachedActions(); } }
        public string MoveActionName { get => moveActionName; set { moveActionName = value; ClearCachedActions(); } }
        public string LookActionName { get => lookActionName; set { lookActionName = value; ClearCachedActions(); } }
        public string RunActionName { get => runActionName; set { runActionName = value; ClearCachedActions(); } }
        public InputAction MoveAction => ResolveMoveAction();
        public InputAction LookAction => ResolveLookAction();
        public InputAction RunAction => ResolveRunAction();

        void OnValidate()
        {
            ClearCachedActions();
        }

        void OnEnable()
        {
            if (enableInputOnEnable)
                SetInputEnabled(true);
        }

        void OnDisable()
        {
            if (enableInputOnEnable)
                SetInputEnabled(false);
        }

        public BasicLocomotionInputSnapshot ReadInput(float deltaTime)
        {
            InputAction activeMoveAction = ResolveMoveAction();
            InputAction activeLookAction = ResolveLookAction();
            InputAction activeRunAction = ResolveRunAction();
            Vector2 move = activeMoveAction != null ? activeMoveAction.ReadValue<Vector2>() : Vector2.zero;
            Vector2 look = activeLookAction != null ? activeLookAction.ReadValue<Vector2>() : Vector2.zero;
            bool runHeld = activeRunAction != null && activeRunAction.IsPressed();
            return new BasicLocomotionInputSnapshot(deltaTime, move, look, runHeld);
        }

        public void SetInputEnabled(bool enabled)
        {
            SetActionEnabled(ResolveMoveAction(), enabled);
            SetActionEnabled(ResolveLookAction(), enabled);
            SetActionEnabled(ResolveRunAction(), enabled);
        }

        InputAction ResolveMoveAction()
        {
            return moveAction ?? (moveAction = ResolveAction(moveActionName));
        }

        InputAction ResolveLookAction()
        {
            return lookAction ?? (lookAction = ResolveAction(lookActionName));
        }

        InputAction ResolveRunAction()
        {
            return runAction ?? (runAction = ResolveAction(runActionName));
        }

        InputAction ResolveAction(string actionName)
        {
            if (inputActions == null || string.IsNullOrEmpty(actionName))
                return null;

            if (!string.IsNullOrEmpty(actionMapName))
            {
                InputActionMap actionMap = inputActions.FindActionMap(actionMapName, false);
                if (actionMap != null)
                    return actionMap.FindAction(actionName, false);
            }

            return inputActions.FindAction(actionName, false);
        }

        void ClearCachedActions()
        {
            moveAction = null;
            lookAction = null;
            runAction = null;
        }

        static void SetActionEnabled(InputAction action, bool enabled)
        {
            if (action == null)
                return;

            if (enabled)
                action.Enable();
            else
                action.Disable();
        }
    }
}
