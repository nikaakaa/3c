using System;
using System.Threading;
using System.Threading.Tasks;
using TEngine;
using YooAsset;

namespace ThirdPerson.ProductStartup
{
    public static class ProjectSceneResourcePreparation
    {
        public static async Task PrepareAsync(
            IResourceModule resourceModule,
            ProductStartupProfile profile,
            CancellationToken cancellationToken)
        {
            if (resourceModule == null)
                throw new ArgumentNullException(nameof(resourceModule));
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            cancellationToken.ThrowIfCancellationRequested();

            if (!YooAssets.Initialized)
                resourceModule.Initialize();

            ResourcePackage package = YooAssets.GetPackage(resourceModule.DefaultPackageName);
            if (package.InitializeStatus == EOperationStatus.Succeed &&
                !string.IsNullOrWhiteSpace(package.GetPackageVersion()))
            {
                return;
            }

            var adapter = new ProjectResourceInitializationAdapter(resourceModule);
            if (package.InitializeStatus != EOperationStatus.Succeed)
            {
                ProductResourceInitializationResult initialization =
                    await adapter.InitializePackageAndVerifyCacheAsync(
                        profile, null, cancellationToken);
                RequireSuccess(initialization.StageResult);
            }

            var version = await adapter.RequestPackageVersionAsync(
                profile.RequestTimeoutSeconds, cancellationToken);
            RequireSuccess(version.Result);
            RequireSuccess(await adapter.UpdatePackageManifestAsync(
                version.PackageVersion, profile.RequestTimeoutSeconds, cancellationToken));
            adapter.CommitPackageVersion(version.PackageVersion);
        }

        static void RequireSuccess(ProductStartupStageResult result)
        {
            if (!result.Succeeded)
                throw new ProductStartupException(
                    result.ErrorCode, result.SafeError, result.Retryable);
        }
    }
}
