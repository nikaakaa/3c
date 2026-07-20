using System;
using UnityEngine;

namespace ThirdPerson.ProductStartup
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-10000)]
    public sealed class ProductBootstrapHost : MonoBehaviour
    {
        static ProductBootstrapHost s_Active;

        [SerializeField] ProductStartupProfile m_Profile;
        ProductBootstrapRunner m_Runner;
        ProductBootstrapView m_View;

        public static ProductBootstrapHost Active => s_Active;
        public ProductStartupProfile Profile => m_Profile;

        void Awake()
        {
            if (s_Active != null && s_Active != this)
            {
                enabled = false;
                throw new InvalidOperationException("Bootstrap scene contains more than one ProductBootstrapHost.");
            }

            s_Active = this;
            m_View = GetComponent<ProductBootstrapView>();
            if (m_View == null)
            {
                m_View = gameObject.AddComponent<ProductBootstrapView>();
            }
        }

        public void BindRunner(ProductBootstrapRunner runner)
        {
            if (runner == null) throw new ArgumentNullException(nameof(runner));
            if (m_Runner != null) throw new InvalidOperationException("Bootstrap host is already bound to a runner.");
            if (m_Profile == null)
            {
                throw new ProductStartupException(
                    ProductStartupErrorCode.ProfileMissing,
                    "Bootstrap ProductStartupProfile is missing.",
                    false);
            }

            m_Runner = runner;
            m_View.Bind(runner.Snapshots, runner);
        }

        void OnDestroy()
        {
            if (s_Active == this)
            {
                s_Active = null;
            }

            if (m_Runner != null && !m_Runner.HandoffCommitted)
            {
                m_Runner.Dispose();
            }
        }
    }
}
