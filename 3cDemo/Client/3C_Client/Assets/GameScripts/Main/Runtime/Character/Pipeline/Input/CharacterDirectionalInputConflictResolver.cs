using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace ThirdPersonCharacter.Pipeline.Input
{
    public sealed class CharacterDirectionalInputConflictResolver : IDisposable
    {
        readonly InputAction m_Action;
        readonly List<PartBinding> m_PartBindings = new List<PartBinding>();
        readonly List<PartControl> m_PartControls = new List<PartControl>();
        int m_LastHorizontal;
        int m_LastVertical;
        bool m_PreviousUp;
        bool m_PreviousDown;
        bool m_PreviousLeft;
        bool m_PreviousRight;
        bool m_Active;

        public CharacterDirectionalInputConflictResolver(InputAction action)
        {
            m_Action = action ?? throw new ArgumentNullException(nameof(action));
            if (!TryCollectPartBindings(action, m_PartBindings, out string error))
                throw new ArgumentException(error, nameof(action));
        }

        public static bool TryValidateAction(InputAction action, out string error)
        {
            if (action == null)
            {
                error = "source action is missing.";
                return false;
            }

            return TryCollectPartBindings(action, null, out error);
        }

        public void Activate()
        {
            if (m_Active)
                return;
            if (!m_Action.enabled)
                throw new InvalidOperationException($"Input action '{m_Action}' must be enabled before its direction resolver is activated.");

            ResolveControls();
            m_Action.started += OnActionChanged;
            m_Action.performed += OnActionChanged;
            m_Action.canceled += OnActionChanged;
            m_Active = true;
            CaptureActiveDirections();
        }

        public void Deactivate()
        {
            if (!m_Active)
                return;
            m_Action.started -= OnActionChanged;
            m_Action.performed -= OnActionChanged;
            m_Action.canceled -= OnActionChanged;
            m_PartControls.Clear();
            m_LastHorizontal = 0;
            m_LastVertical = 0;
            m_PreviousUp = false;
            m_PreviousDown = false;
            m_PreviousLeft = false;
            m_PreviousRight = false;
            m_Active = false;
        }

        public Vector2 Resolve(Vector2 value)
        {
            if (!m_Active)
                throw new InvalidOperationException($"Input action '{m_Action}' direction resolver is not active.");

            ReadPressedParts(out bool up, out bool down, out bool left, out bool right);
            bool upActivated = up && !m_PreviousUp;
            bool downActivated = down && !m_PreviousDown;
            bool leftActivated = left && !m_PreviousLeft;
            bool rightActivated = right && !m_PreviousRight;
            if (upActivated != downActivated)
                m_LastVertical = upActivated ? 1 : -1;
            if (leftActivated != rightActivated)
                m_LastHorizontal = leftActivated ? -1 : 1;

            m_PreviousUp = up;
            m_PreviousDown = down;
            m_PreviousLeft = left;
            m_PreviousRight = right;

            if (up && down && m_LastVertical != 0)
                value.y = m_LastVertical;
            if (left && right && m_LastHorizontal != 0)
                value.x = m_LastHorizontal;
            return value;
        }

        public void Dispose()
        {
            Deactivate();
        }

        void ReadPressedParts(out bool up, out bool down, out bool left, out bool right)
        {
            up = false;
            down = false;
            left = false;
            right = false;
            for (int i = 0; i < m_PartControls.Count; i++)
            {
                PartControl part = m_PartControls[i];
                if (!part.Control.IsPressed())
                    continue;
                switch (part.Direction)
                {
                    case CardinalDirection.Up:
                        up = true;
                        break;
                    case CardinalDirection.Down:
                        down = true;
                        break;
                    case CardinalDirection.Left:
                        left = true;
                        break;
                    case CardinalDirection.Right:
                        right = true;
                        break;
                }
            }
        }

        void ResolveControls()
        {
            m_PartControls.Clear();
            ReadOnlyArray<InputControl> controls = m_Action.controls;
            for (int bindingIndex = 0; bindingIndex < m_PartBindings.Count; bindingIndex++)
            {
                PartBinding binding = m_PartBindings[bindingIndex];
                for (int controlIndex = 0; controlIndex < controls.Count; controlIndex++)
                {
                    InputControl control = controls[controlIndex];
                    if (!InputControlPath.Matches(binding.EffectivePath, control))
                        continue;
                    AddControl(control, binding.Direction);
                }
            }
        }

        void AddControl(InputControl control, CardinalDirection direction)
        {
            for (int i = 0; i < m_PartControls.Count; i++)
            {
                if (m_PartControls[i].Control != control)
                    continue;
                if (m_PartControls[i].Direction != direction)
                    throw new InvalidOperationException($"Input action '{m_Action}' binds control '{control.path}' to multiple Dpad directions.");
                return;
            }
            m_PartControls.Add(new PartControl(control, direction));
        }

        void OnActionChanged(InputAction.CallbackContext context)
        {
            InputControl control = context.control;
            if (control == null || !control.IsPressed())
                return;
            for (int i = 0; i < m_PartControls.Count; i++)
            {
                PartControl part = m_PartControls[i];
                if (part.Control != control)
                    continue;
                SetLastDirection(part.Direction);
                return;
            }
        }

        void CaptureActiveDirections()
        {
            InputControl activeControl = m_Action.activeControl;
            if (activeControl != null && activeControl.IsPressed())
            {
                for (int i = 0; i < m_PartControls.Count; i++)
                {
                    if (m_PartControls[i].Control == activeControl)
                    {
                        SetLastDirection(m_PartControls[i].Direction);
                        break;
                    }
                }
            }

            ReadPressedParts(out bool up, out bool down, out bool left, out bool right);
            if (up && !down)
                m_LastVertical = 1;
            else if (down && !up)
                m_LastVertical = -1;
            if (left && !right)
                m_LastHorizontal = -1;
            else if (right && !left)
                m_LastHorizontal = 1;
            m_PreviousUp = up;
            m_PreviousDown = down;
            m_PreviousLeft = left;
            m_PreviousRight = right;
        }

        void SetLastDirection(CardinalDirection direction)
        {
            switch (direction)
            {
                case CardinalDirection.Up:
                    m_LastVertical = 1;
                    break;
                case CardinalDirection.Down:
                    m_LastVertical = -1;
                    break;
                case CardinalDirection.Left:
                    m_LastHorizontal = -1;
                    break;
                case CardinalDirection.Right:
                    m_LastHorizontal = 1;
                    break;
            }
        }

        static bool TryCollectPartBindings(InputAction action, List<PartBinding> destination, out string error)
        {
            destination?.Clear();
            int directionalCompositeCount = 0;
            bool hasUp = false;
            bool hasDown = false;
            bool hasLeft = false;
            bool hasRight = false;
            ReadOnlyArray<InputBinding> bindings = action.bindings;
            for (int i = 0; i < bindings.Count; i++)
            {
                InputBinding binding = bindings[i];
                if (!binding.isComposite || !IsDirectionalComposite(binding.GetNameOfComposite()))
                    continue;

                directionalCompositeCount++;
                for (i++; i < bindings.Count && bindings[i].isPartOfComposite; i++)
                {
                    InputBinding part = bindings[i];
                    if (!TryParseDirection(part.name, out CardinalDirection direction))
                    {
                        error = $"directional composite contains unsupported part '{part.name}'.";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(part.effectivePath))
                    {
                        error = $"directional composite part '{part.name}' has no effective control path.";
                        return false;
                    }

                    destination?.Add(new PartBinding(part.effectivePath, direction));
                    switch (direction)
                    {
                        case CardinalDirection.Up:
                            hasUp = true;
                            break;
                        case CardinalDirection.Down:
                            hasDown = true;
                            break;
                        case CardinalDirection.Left:
                            hasLeft = true;
                            break;
                        case CardinalDirection.Right:
                            hasRight = true;
                            break;
                    }
                }
                i--;
            }

            if (directionalCompositeCount != 1)
            {
                error = $"latest-actuated direction policy requires exactly one Dpad composite, but found {directionalCompositeCount}.";
                return false;
            }
            if (!hasUp || !hasDown || !hasLeft || !hasRight)
            {
                error = "latest-actuated direction policy requires complete up, down, left, and right Dpad parts.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        static bool IsDirectionalComposite(string name)
        {
            return string.Equals(name, "Dpad", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "2DVector", StringComparison.OrdinalIgnoreCase);
        }

        static bool TryParseDirection(string name, out CardinalDirection direction)
        {
            if (string.Equals(name, "up", StringComparison.OrdinalIgnoreCase))
            {
                direction = CardinalDirection.Up;
                return true;
            }
            if (string.Equals(name, "down", StringComparison.OrdinalIgnoreCase))
            {
                direction = CardinalDirection.Down;
                return true;
            }
            if (string.Equals(name, "left", StringComparison.OrdinalIgnoreCase))
            {
                direction = CardinalDirection.Left;
                return true;
            }
            if (string.Equals(name, "right", StringComparison.OrdinalIgnoreCase))
            {
                direction = CardinalDirection.Right;
                return true;
            }
            direction = default;
            return false;
        }

        enum CardinalDirection : byte
        {
            Up,
            Down,
            Left,
            Right
        }

        readonly struct PartBinding
        {
            public PartBinding(string effectivePath, CardinalDirection direction)
            {
                EffectivePath = effectivePath;
                Direction = direction;
            }

            public string EffectivePath { get; }
            public CardinalDirection Direction { get; }
        }

        readonly struct PartControl
        {
            public PartControl(InputControl control, CardinalDirection direction)
            {
                Control = control;
                Direction = direction;
            }

            public InputControl Control { get; }
            public CardinalDirection Direction { get; }
        }
    }
}
