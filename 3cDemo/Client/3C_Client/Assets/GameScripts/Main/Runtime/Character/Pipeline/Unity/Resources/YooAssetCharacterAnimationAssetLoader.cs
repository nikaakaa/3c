using System;
using System.Collections.Generic;
using TEngine;
using ThirdPersonCharacter.Pipeline.Animation;
using YooAsset;

namespace ThirdPersonCharacter.Pipeline.Unity.Resources
{
    internal sealed class YooAssetCharacterAnimationAssetLoader : ICharacterAnimationAssetLoader
    {
        readonly IResourceModule m_ResourceModule;
        readonly string m_PackageName;
        readonly Dictionary<ulong, AssetHandle> m_Handles =
            new Dictionary<ulong, AssetHandle>();
        ulong m_NextTicket = 1;
        bool m_Disposed;

        internal YooAssetCharacterAnimationAssetLoader(
            IResourceModule resourceModule,
            string packageName)
        {
            m_ResourceModule = resourceModule ?? throw new ArgumentNullException(nameof(resourceModule));
            m_PackageName = string.IsNullOrWhiteSpace(packageName)
                ? throw new ArgumentException("Animation resource package name is required.", nameof(packageName))
                : packageName.Trim();
        }

        public CharacterAnimationAssetLoadTicket BeginLoad(
            CharacterAnimationResourceAddress address)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(YooAssetCharacterAnimationAssetLoader));
            if (!address.IsValid)
                throw new ArgumentException("Animation resource address is invalid.", nameof(address));
            AssetHandle handle = m_ResourceModule.LoadAssetAsyncHandle<CharacterAclAnimationResource>(
                address.Value,
                m_PackageName);
            if (handle == null || !handle.IsValid)
                throw new InvalidOperationException($"Animation resource '{address}' could not start loading.");
            ulong value = m_NextTicket++;
            if (value == 0)
                throw new InvalidOperationException("Animation resource load ticket space is exhausted.");
            m_Handles.Add(value, handle);
            return new CharacterAnimationAssetLoadTicket(value);
        }

        public CharacterAnimationAssetLoadResult Poll(
            CharacterAnimationAssetLoadTicket ticket)
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(YooAssetCharacterAnimationAssetLoader));
            if (!ticket.IsValid || !m_Handles.TryGetValue(ticket.Value, out AssetHandle handle))
                return new CharacterAnimationAssetLoadResult(
                    CharacterAnimationAssetLoadState.Invalid,
                    null,
                    "Animation resource load ticket is stale.");
            if (!handle.IsDone)
                return new CharacterAnimationAssetLoadResult(
                    CharacterAnimationAssetLoadState.Pending,
                    null,
                    string.Empty);
            if (handle.Status != EOperationStatus.Succeed ||
                !(handle.AssetObject is CharacterAclAnimationResource resource))
                return new CharacterAnimationAssetLoadResult(
                    CharacterAnimationAssetLoadState.Invalid,
                    null,
                    string.IsNullOrEmpty(handle.LastError)
                        ? "Animation resource load failed."
                        : handle.LastError);
            return new CharacterAnimationAssetLoadResult(
                CharacterAnimationAssetLoadState.Ready,
                resource,
                string.Empty);
        }

        public void Release(CharacterAnimationAssetLoadTicket ticket)
        {
            if (!ticket.IsValid || !m_Handles.TryGetValue(ticket.Value, out AssetHandle handle))
                return;
            m_Handles.Remove(ticket.Value);
            handle.Release();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            foreach (KeyValuePair<ulong, AssetHandle> pair in m_Handles)
                pair.Value.Release();
            m_Handles.Clear();
            m_Disposed = true;
        }
    }
}
