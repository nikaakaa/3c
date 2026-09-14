using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal delegate ICharacterPoseNativeNodeHandler
        CharacterPoseNativeNodeHandlerCreator(
            CharacterPoseCanvasNode node,
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context);

    internal sealed class CharacterPoseNativeNodeHandlerRegistry :
        ICharacterPoseNativeNodeHandlerFactory
    {
        readonly Dictionary<
            CharacterPoseNodeKind,
            CharacterPoseNativeNodeHandlerCreator> m_Creators =
                new Dictionary<
                    CharacterPoseNodeKind,
                    CharacterPoseNativeNodeHandlerCreator>();
        bool m_Sealed;

        internal void Register(
            CharacterPoseNodeKind kind,
            CharacterPoseNativeNodeHandlerCreator creator)
        {
            if (m_Sealed ||
                !Enum.IsDefined(typeof(CharacterPoseNodeKind), kind) ||
                creator == null ||
                IsBuiltin(kind) ||
                !m_Creators.TryAdd(kind, creator))
            {
                throw new ArgumentException(
                    $"Pose native handler registration for '{kind}' is invalid.",
                    nameof(kind));
            }
        }

        public IReadOnlyList<ICharacterPoseNativeNodeHandler> Create(
            in CharacterPoseNativePreparedBinding preparedBinding,
            in CharacterPoseNativeInstanceContext context)
        {
            if (!preparedBinding.IsValid || !context.IsValid)
                throw new ArgumentException(
                    "Pose native handler registry binding is invalid.");
            m_Sealed = true;
            var handlers = new List<ICharacterPoseNativeNodeHandler>();
            try
            {
                foreach (CharacterPoseCanvasNode node in preparedBinding.Graph.Nodes)
                {
                    if (IsBuiltin(node.Kind))
                        continue;
                    if (!m_Creators.TryGetValue(
                            node.Kind,
                            out CharacterPoseNativeNodeHandlerCreator creator))
                    {
                        throw new InvalidOperationException(
                            $"Pose native node kind '{node.Kind}' has no registered handler creator.");
                    }
                    ICharacterPoseNativeNodeHandler handler = creator(
                        node,
                        in preparedBinding,
                        in context);
                    if (handler == null)
                        throw new InvalidOperationException(
                            $"Pose native handler creator for '{node.NodeId}' returned no handler.");
                    if (handler.NodeId != node.NodeId || handler.Kind != node.Kind)
                    {
                        handler.Dispose();
                        throw new InvalidOperationException(
                            $"Pose native handler for '{node.NodeId}' does not match the registered node identity or kind.");
                    }
                    handlers.Add(handler);
                }
                return handlers.ToArray();
            }
            catch
            {
                for (int i = handlers.Count - 1; i >= 0; i--)
                    handlers[i].Dispose();
                throw;
            }
        }

        static bool IsBuiltin(CharacterPoseNodeKind kind) =>
            kind == CharacterPoseNodeKind.OutputPose ||
            kind == CharacterPoseNodeKind.GraphOutput ||
            kind == CharacterPoseNodeKind.GraphInput ||
            kind == CharacterPoseNodeKind.ProgramParameterInput ||
            kind == CharacterPoseNodeKind.ActionPlaybackInput;
    }
}
