using Cysharp.Threading.Tasks;
using TEngine;
using YooAsset;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureInitResources : ProcedureBase
    {
        private string _downloadError;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            CheckResourcesAsync(procedureOwner).Forget();
        }

        private async UniTaskVoid CheckResourcesAsync(ProcedureOwner procedureOwner)
        {
            if (!TryGetResourceModule(out var resourceModule))
            {
                return;
            }

            if (resourceModule.PlayMode != EPlayMode.HostPlayMode &&
                resourceModule.PlayMode != EPlayMode.WebPlayMode)
            {
                ChangeState<ProcedureLoadAssembly>(procedureOwner);
                return;
            }

            var versionOperation = resourceModule.RequestPackageVersionAsync();
            await versionOperation.ToUniTask();
            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                Log.Fatal($"Request resource package version failed: {versionOperation.Error}");
                return;
            }

            resourceModule.PackageVersion = versionOperation.PackageVersion;

            var manifestOperation = resourceModule.UpdatePackageManifestAsync(versionOperation.PackageVersion);
            await manifestOperation.ToUniTask();
            if (manifestOperation.Status != EOperationStatus.Succeed)
            {
                Log.Fatal($"Update resource package manifest failed: {manifestOperation.Error}");
                return;
            }

            var downloader = resourceModule.CreateResourceDownloader();
            if (downloader.TotalDownloadCount > 0)
            {
                _downloadError = string.Empty;
                downloader.DownloadErrorCallback = OnDownloadError;
                downloader.BeginDownload();
                await downloader.ToUniTask();

                if (downloader.Status != EOperationStatus.Succeed)
                {
                    Log.Fatal($"Download resource package files failed: {_downloadError}");
                    return;
                }
            }

            Log.Info($"TEngine resource package checked: {resourceModule.PackageVersion}");
            ChangeState<ProcedureLoadAssembly>(procedureOwner);
        }

        private void OnDownloadError(DownloadErrorData data)
        {
            _downloadError = $"{data.FileName}: {data.ErrorInfo}";
        }
    }
}
