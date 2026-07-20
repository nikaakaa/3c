using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic.ProductResource
{
    public enum PreloadItemKind
    {
        Asset = 0,
        PrefabAsset = 1,
        SceneLocation = 2
    }

    public sealed class PreloadItem
    {
        private PreloadItem(string location, Type expectedType, PreloadItemKind kind)
        {
            Location = string.IsNullOrWhiteSpace(location) ? throw new ArgumentException("Location is required.", nameof(location)) : location.Trim();
            ExpectedType = expectedType ?? throw new ArgumentNullException(nameof(expectedType));
            Kind = kind;
        }

        public string Location { get; }
        public Type ExpectedType { get; }
        public PreloadItemKind Kind { get; }

        public static PreloadItem Asset<T>(string location) where T : UnityEngine.Object
        {
            return new PreloadItem(location, typeof(T), PreloadItemKind.Asset);
        }

        public static PreloadItem Asset(string location, Type expectedType)
        {
            if (!typeof(UnityEngine.Object).IsAssignableFrom(expectedType))
            {
                throw new ArgumentException("Expected type must be a Unity Object type.", nameof(expectedType));
            }

            return new PreloadItem(location, expectedType, PreloadItemKind.Asset);
        }

        public static PreloadItem Prefab(string location)
        {
            return new PreloadItem(location, typeof(GameObject), PreloadItemKind.PrefabAsset);
        }

        public static PreloadItem Scene(string location)
        {
            return new PreloadItem(location, typeof(UnityEngine.SceneManagement.Scene), PreloadItemKind.SceneLocation);
        }
    }

    public sealed class PreloadBarrier
    {
        private readonly PreloadItem[] _items;

        public PreloadBarrier(string name, IReadOnlyList<PreloadItem> items)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Barrier name is required.", nameof(name)) : name.Trim();
            if (items == null || items.Count == 0)
            {
                throw new ArgumentException("Barrier must contain at least one item.", nameof(items));
            }

            _items = new PreloadItem[items.Count];
            for (int index = 0; index < items.Count; index++)
            {
                _items[index] = items[index] ?? throw new ArgumentException("Barrier contains a null item.", nameof(items));
            }
        }

        public string Name { get; }
        public IReadOnlyList<PreloadItem> Items => _items;
    }

    public sealed class PreloadPlan
    {
        public const string HomeSharedUiBarrier = "Home.SharedUI";
        public const string HomeUiBarrier = "Home.UI";
        public const string HomePresentationBarrier = "Home.Presentation";
        public const string GameplaySharedBarrier = "Gameplay.Shared";
        public const string GameplaySceneBarrier = "Gameplay.Scene";
        public const string GameplayCorinPresentationBarrier = "Gameplay.CorinPresentation";

        private readonly PreloadBarrier[] _barriers;

        public PreloadPlan(string name, IReadOnlyList<PreloadBarrier> barriers)
        {
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Plan name is required.", nameof(name)) : name.Trim();
            if (barriers == null || barriers.Count == 0)
            {
                throw new ArgumentException("Plan must contain at least one barrier.", nameof(barriers));
            }

            _barriers = new PreloadBarrier[barriers.Count];
            var names = new HashSet<string>(StringComparer.Ordinal);
            for (int index = 0; index < barriers.Count; index++)
            {
                PreloadBarrier barrier = barriers[index] ?? throw new ArgumentException("Plan contains a null barrier.", nameof(barriers));
                if (!names.Add(barrier.Name))
                {
                    throw new ArgumentException($"Barrier '{barrier.Name}' is duplicated.", nameof(barriers));
                }
                _barriers[index] = barrier;
            }
        }

        public string Name { get; }
        public IReadOnlyList<PreloadBarrier> Barriers => _barriers;

        public static PreloadPlan Home(IReadOnlyList<PreloadItem> sharedUi, IReadOnlyList<PreloadItem> homeUi, IReadOnlyList<PreloadItem> presentation)
        {
            return new PreloadPlan("Home", new[]
            {
                new PreloadBarrier(HomeSharedUiBarrier, sharedUi),
                new PreloadBarrier(HomeUiBarrier, homeUi),
                new PreloadBarrier(HomePresentationBarrier, presentation)
            });
        }

        public static PreloadPlan Gameplay(IReadOnlyList<PreloadItem> shared, string sceneLocation, IReadOnlyList<PreloadItem> corinPresentation)
        {
            return new PreloadPlan("Gameplay", new[]
            {
                new PreloadBarrier(GameplaySharedBarrier, shared),
                new PreloadBarrier(GameplaySceneBarrier, new[] { PreloadItem.Scene(sceneLocation) }),
                new PreloadBarrier(GameplayCorinPresentationBarrier, corinPresentation)
            });
        }
    }

    public sealed class PreloadPlanResult
    {
        public PreloadPlanResult(string planName, IReadOnlyList<string> committedBarriers, IReadOnlyList<ResourceLease> leases)
        {
            PlanName = planName;
            CommittedBarriers = committedBarriers;
            Leases = leases;
        }

        public string PlanName { get; }
        public IReadOnlyList<string> CommittedBarriers { get; }
        public IReadOnlyList<ResourceLease> Leases { get; }
    }

    public sealed class PreloadPlanExecutor
    {
        private readonly ProductResourceRuntime _resources;

        public PreloadPlanExecutor(ProductResourceRuntime resources)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        }

        public async UniTask<PreloadPlanResult> ExecuteAsync(PreloadPlan plan, ResourceScope scope, CancellationToken cancellationToken = default)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var committed = new List<string>(plan.Barriers.Count);
            var leases = new List<ResourceLease>();
            foreach (PreloadBarrier barrier in plan.Barriers)
            {
                var tasks = new UniTask<ResourceLease>[barrier.Items.Count];
                for (int index = 0; index < barrier.Items.Count; index++)
                {
                    tasks[index] = ExecuteItemAsync(barrier.Items[index], scope, cancellationToken);
                }

                ResourceLease[] barrierLeases = await UniTask.WhenAll(tasks);
                foreach (ResourceLease lease in barrierLeases)
                {
                    if (lease != null)
                    {
                        leases.Add(lease);
                    }
                }
                committed.Add(barrier.Name);
            }

            return new PreloadPlanResult(plan.Name, committed.ToArray(), leases.ToArray());
        }

        private async UniTask<ResourceLease> ExecuteItemAsync(PreloadItem item, ResourceScope scope, CancellationToken cancellationToken)
        {
            if (item.Kind == PreloadItemKind.SceneLocation)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!_resources.ValidateSceneLocation(item.Location))
                {
                    throw new InvalidOperationException($"Scene location '{item.Location}' is not valid in the active package.");
                }

                return null;
            }

            return await _resources.AcquireAsync(scope, item.Location, item.ExpectedType, cancellationToken);
        }
    }
}
