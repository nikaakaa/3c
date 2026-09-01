using System;
using ThirdPerson.NetworkTest.Contracts;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    [Serializable]
    internal sealed class NetworkTestSessionSlotProfileEndpoint
    {
        [SerializeField] string m_Key = string.Empty;
        [SerializeField] string m_Address = "127.0.0.1";
        [SerializeField] int m_Port;

        public NetworkTestSessionEndpointDocument Build() => new NetworkTestSessionEndpointDocument
        {
            key = m_Key,
            address = m_Address,
            port = m_Port
        };
    }

    [Serializable]
    internal sealed class NetworkTestSessionSlotProfileWindow
    {
        [SerializeField] string m_RoleId = string.Empty;
        [SerializeField] int m_X;
        [SerializeField] int m_Y;
        [SerializeField] int m_Width = 900;
        [SerializeField] int m_Height = 600;

        public NetworkTestSessionWindowDocument Build() => new NetworkTestSessionWindowDocument
        {
            roleId = m_RoleId,
            x = m_X,
            y = m_Y,
            width = m_Width,
            height = m_Height
        };
    }

    [Serializable]
    internal sealed class NetworkTestSessionSlotProfileEntry
    {
        [SerializeField] string m_SlotId = string.Empty;
        [SerializeField] NetworkTestSessionSlotProfileEndpoint[] m_Endpoints =
            Array.Empty<NetworkTestSessionSlotProfileEndpoint>();
        [SerializeField] NetworkTestSessionSlotProfileWindow[] m_Windows =
            Array.Empty<NetworkTestSessionSlotProfileWindow>();

        public NetworkTestSessionSlotDocument Build()
        {
            var endpoints = new NetworkTestSessionEndpointDocument[m_Endpoints.Length];
            for (int i = 0; i < endpoints.Length; i++)
                endpoints[i] = m_Endpoints[i]?.Build();
            var windows = new NetworkTestSessionWindowDocument[m_Windows.Length];
            for (int i = 0; i < windows.Length; i++)
                windows[i] = m_Windows[i]?.Build();
            return new NetworkTestSessionSlotDocument
            {
                slotId = m_SlotId,
                endpoints = endpoints,
                windows = windows
            };
        }
    }

    [CreateAssetMenu(menuName = "3C/Development/Network Test Session Slot Profile")]
    public sealed class NetworkTestSessionSlotProfile : ScriptableObject
    {
        [SerializeField] NetworkTestSessionSlotProfileEntry[] m_Slots =
            Array.Empty<NetworkTestSessionSlotProfileEntry>();

        internal NetworkTestSessionSlotCatalogDocument BuildCatalog()
        {
            var slots = new NetworkTestSessionSlotDocument[m_Slots.Length];
            var slotIds = new HashSet<string>(StringComparer.Ordinal);
            var globalPorts = new HashSet<int>();
            for (int i = 0; i < slots.Length; i++)
            {
                NetworkTestSessionSlotDocument slot = m_Slots[i]?.Build() ??
                    throw new InvalidOperationException($"Network Test Slot Profile '{name}' contains an empty Slot.");
                if (string.IsNullOrWhiteSpace(slot.slotId) || !string.Equals(slot.slotId, slot.slotId.Trim(), StringComparison.Ordinal) ||
                    !slotIds.Add(slot.slotId))
                    throw new InvalidOperationException($"Network Test Slot Profile '{name}' contains an invalid SlotId.");
                var endpointKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (NetworkTestSessionEndpointDocument endpoint in slot.endpoints)
                {
                    if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.key) ||
                        !endpointKeys.Add(endpoint.key) || endpoint.address != "127.0.0.1" ||
                        endpoint.port is <= 0 or > 65535 || !globalPorts.Add(endpoint.port))
                        throw new InvalidOperationException($"Network Test Slot '{slot.slotId}' contains an invalid endpoint.");
                }
                var windowRoles = new HashSet<string>(StringComparer.Ordinal);
                foreach (NetworkTestSessionWindowDocument window in slot.windows)
                {
                    if (window == null || string.IsNullOrWhiteSpace(window.roleId) ||
                        !windowRoles.Add(window.roleId) || window.width <= 0 || window.height <= 0)
                        throw new InvalidOperationException($"Network Test Slot '{slot.slotId}' contains an invalid window.");
                }
                slots[i] = slot;
            }
            if (slots.Length == 0)
                throw new InvalidOperationException($"Network Test Slot Profile '{name}' contains no Slots.");
            return new NetworkTestSessionSlotCatalogDocument
            {
                schemaVersion = 1,
                slots = slots
            };
        }
    }
}
