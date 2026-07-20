using TEngine;
using ThirdPerson.ProductStartup;
using ProcedureOwner = TEngine.IFsm<TEngine.IProcedureModule>;

namespace Procedure
{
    public sealed class ProcedureLaunch : ProcedureBase
    {
        ProductBootstrapRunner m_Runner;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            var host = ProductBootstrapHost.Active;
            if (host == null || host.Profile == null)
            {
                Log.Fatal("Bootstrap ProductBootstrapHost or ProductStartupProfile is missing.");
                return;
            }

            if (!TryGetResourceModule(out var resourceModule))
            {
                return;
            }

            var snapshotStore = new ProductStartupSnapshotStore();
            var diskSpaceProbe = new ProductDiskSpaceProbe();
            var gateway = new ProcedureProductStartupGateway(
                resourceModule,
                Settings.UpdateSetting,
                host.Profile,
                snapshotStore,
                diskSpaceProbe,
                () => ChangeState<ProcedureProductRuntime>(procedureOwner));
            m_Runner = new ProductBootstrapRunner(
                host.Profile,
                snapshotStore,
                new StartupPolicyClient(),
                new ProjectResourceInitializationAdapter(resourceModule),
                diskSpaceProbe,
                gateway);
            host.BindRunner(m_Runner);
            m_Runner.Start();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            if (isShutdown && m_Runner != null && !m_Runner.HandoffCommitted)
            {
                m_Runner.Dispose();
            }

            base.OnLeave(procedureOwner, isShutdown);
        }
    }
}
