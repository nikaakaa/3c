using System;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using TEngine;
using ThirdPerson.ProductStartup;

namespace Procedure
{
    internal sealed class ProcedureProductStartupGateway : IProductStartupHandoffStage
    {
        readonly IResourceModule m_ResourceModule;
        readonly UpdateSetting m_UpdateSetting;
        readonly ProductStartupProfile m_Profile;
        readonly IProductStartupSnapshotSource m_StartupSnapshots;
        readonly IProductDiskSpaceProbe m_DiskSpaceProbe;
        readonly Action m_EnterProductRuntimeState;
        MethodInfo m_EntryMethod;
        int m_EntryCommitted;

        public ProcedureProductStartupGateway(
            IResourceModule resourceModule,
            UpdateSetting updateSetting,
            ProductStartupProfile profile,
            IProductStartupSnapshotSource startupSnapshots,
            IProductDiskSpaceProbe diskSpaceProbe,
            Action enterProductRuntimeState)
        {
            m_ResourceModule = resourceModule ?? throw new ArgumentNullException(nameof(resourceModule));
            m_UpdateSetting = updateSetting ?? throw new ArgumentNullException(nameof(updateSetting));
            m_Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            m_StartupSnapshots = startupSnapshots ?? throw new ArgumentNullException(nameof(startupSnapshots));
            m_DiskSpaceProbe = diskSpaceProbe ?? throw new ArgumentNullException(nameof(diskSpaceProbe));
            m_EnterProductRuntimeState = enterProductRuntimeState ?? throw new ArgumentNullException(nameof(enterProductRuntimeState));
        }

        public async UniTask<ProductStartupHandoff> LoadHotUpdateAssembliesAsync(
            string packageName,
            string resourcePackageVersion,
            CancellationToken cancellationToken)
        {
            HotUpdateAssemblyLoadResult result;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                result = await HotUpdateAssemblyLoader.LoadAsync(
                    m_ResourceModule,
                    m_UpdateSetting,
                    cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.HotUpdateAssemblyLoadFailed,
                    "Hot-update assemblies could not be loaded.",
                    false);
            }

            if (result.MainAssembly == null)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.HotUpdateAssemblyMissing,
                    "Main hot-update assembly is missing.",
                    false);
            }

            var appType = result.MainAssembly.GetType("GameApp", false);
            m_EntryMethod = appType?.GetMethod(
                "Entrance",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(object[]) },
                null);
            if (m_EntryMethod == null)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.ProductEntryMissing,
                    "GameApp.Entrance(object[]) is missing.",
                    false);
            }

            return new ProductStartupHandoff(
                packageName,
                resourcePackageVersion,
                result.MainAssembly,
                result.HotUpdateAssemblies);
        }

        public UniTask EnterProductRuntimeAsync(
            ProductStartupHandoff handoff,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (handoff == null || m_EntryMethod == null)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.ProductEntryMissing,
                    "Product runtime handoff is incomplete.",
                    false);
            }

            if (Interlocked.Exchange(ref m_EntryCommitted, 1) != 0)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.ProductEntryInvocationFailed,
                    "GameApp.Entrance was already invoked.",
                    false);
            }

            m_EnterProductRuntimeState();
            try
            {
                m_EntryMethod.Invoke(
                    null,
                    new object[]
                    {
                        new object[]
                        {
                            handoff,
                            m_Profile,
                            m_StartupSnapshots,
                            m_DiskSpaceProbe
                        }
                    });
            }
            catch (TargetInvocationException)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.ProductEntryInvocationFailed,
                    "GameApp.Entrance failed.",
                    false);
            }

            return UniTask.CompletedTask;
        }
    }
}
