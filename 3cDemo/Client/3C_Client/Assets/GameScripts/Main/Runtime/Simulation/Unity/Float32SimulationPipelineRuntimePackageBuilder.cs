using System;
using System.Collections.Generic;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation
{
    public interface IFloat32SimulationPipelinePassRuntimeProvider
    {
        IFloat32PipelinePassRuntimeFactory CreateRuntimeFactory();
        IReadOnlyList<IFloat32PipelineProductSlotFactory> CreateAdditionalProductSlotFactories();
    }

    public interface IFloat32SimulationPipelineRuntimePackageProvider
    {
        Float32SimulationPipelineRuntimePackage BuildRuntimePackage();
    }

    public static class Float32SimulationPipelineRuntimePackageBuilder
    {
        public static Float32SimulationPipelineRuntimePackage BuildPassAuthored(
            SimulationPipelineDefinition pipeline)
        {
            if (!pipeline)
                throw new ArgumentNullException(nameof(pipeline));
            var runtimeFactories = new List<IFloat32PipelinePassRuntimeFactory>();
            var extensionSlots = new Dictionary<SimulationPipelineProductId, IFloat32PipelineProductSlotFactory>();
            AddPhase(pipeline.IngressPasses, runtimeFactories, extensionSlots);
            AddPhase(pipeline.SchedulePasses, runtimeFactories, extensionSlots);
            AddPhase(pipeline.StepPasses, runtimeFactories, extensionSlots);
            AddPhase(pipeline.EgressPasses, runtimeFactories, extensionSlots);
            var runtime = new Float32PipelinePassRuntimeFactoryCatalog(runtimeFactories);
            var products = new List<SimulationPipelineProductContract>(SimulationPipelineProducts.All);
            var slots = new List<IFloat32PipelineProductSlotFactory>(extensionSlots.Values);
            slots.Sort((left, right) => left.Contract.ProductId.CompareTo(right.Contract.ProductId));
            for (int i = 0; i < slots.Count; i++)
                products.Add(slots[i].Contract);
            var descriptors = new SimulationPipelinePassFactoryDescriptor[runtime.Factories.Count];
            for (int i = 0; i < descriptors.Length; i++)
                descriptors[i] = runtime.Factories[i].Descriptor;
            return new Float32SimulationPipelineRuntimePackage(
                pipeline.BuildPortableDescriptor(),
                new SimulationPipelinePassFactoryCatalog(
                    Float32PassExecutionBackend.Descriptor.Identity,
                    descriptors,
                    products),
                runtime,
                Float32PassExecutionBackend.CreateProductRuntimeCatalog(slots));
        }

        static void AddPhase(
            IReadOnlyList<SimulationPipelinePassDefinition> definitions,
            List<IFloat32PipelinePassRuntimeFactory> runtimeFactories,
            Dictionary<SimulationPipelineProductId, IFloat32PipelineProductSlotFactory> extensionSlots)
        {
            for (int i = 0; i < definitions.Count; i++)
            {
                SimulationPipelinePassDefinition definition = definitions[i];
                if (!definition || definition is not IFloat32SimulationPipelinePassRuntimeProvider provider)
                    throw new InvalidOperationException($"Pipeline Pass at index {i} has no Float32 runtime provider.");
                IFloat32PipelinePassRuntimeFactory factory = provider.CreateRuntimeFactory() ??
                    throw new InvalidOperationException($"Pipeline Pass '{definition.PassId}' returned no Float32 runtime factory.");
                SimulationPipelinePassDescriptor pass = definition.BuildPortableDescriptor();
                if (!factory.Descriptor.Identity.PassId.Equals(pass.PassId) ||
                    !factory.Descriptor.Identity.ImplementationVersion.Equals(pass.ImplementationVersion) ||
                    factory.Descriptor.Phase != pass.Phase ||
                    !factory.Descriptor.SupportedConfigurationHash.Equals(pass.ConfigurationHash))
                {
                    throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}' runtime factory does not match its Definition.");
                }
                runtimeFactories.Add(factory);
                IReadOnlyList<IFloat32PipelineProductSlotFactory> additional =
                    provider.CreateAdditionalProductSlotFactories() ??
                    throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}' returned no Product slot collection.");
                for (int slotIndex = 0; slotIndex < additional.Count; slotIndex++)
                {
                    IFloat32PipelineProductSlotFactory slot = additional[slotIndex] ??
                        throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}' contains a missing Product slot factory.");
                    if (IsStandardProduct(slot.Contract.ProductId))
                        throw new InvalidOperationException($"Pipeline Pass '{pass.PassId}' attempted to replace a standard Product runtime.");
                    if (extensionSlots.TryGetValue(slot.Contract.ProductId, out IFloat32PipelineProductSlotFactory existing))
                    {
                        if (!existing.Contract.Equals(slot.Contract) || existing.Lifetime != slot.Lifetime || existing.GetType() != slot.GetType())
                            throw new InvalidOperationException($"Pipeline Product '{slot.Contract.ProductId}' has conflicting runtime factories.");
                    }
                    else
                    {
                        extensionSlots.Add(slot.Contract.ProductId, slot);
                    }
                }
            }
        }

        static bool IsStandardProduct(SimulationPipelineProductId productId)
        {
            for (int i = 0; i < SimulationPipelineProducts.All.Count; i++)
            {
                if (SimulationPipelineProducts.All[i].ProductId.Equals(productId))
                    return true;
            }
            return false;
        }
    }
}
