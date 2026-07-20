## Context

当前组合链为：

```text
SimulationSessionCompositionDefinition
  -> ProgramRuntimeDefinition.CreateComposer()
  -> SessionSource preparation
  -> UnityFloat32SimulationSessionComposer
  -> Float32SimulationPipelineFactorySet
  -> Float32SimulationSessionCompositionRequest
  -> portable Float32SimulationSessionComposer
```

普通 Local、Preview与ServerAuthoritative Prediction都直接进入 portable Composer。ServerAuthoritative Authority需要额外验证 portable Authority Pipeline catalog、Source policy、locked roster与四个Source port，因此公共 Unity Composer增加了两个具体分支：

```text
pipeline is ServerAuthoritativeAuthorityPipelineDefinition
source as ServerAuthoritativeAuthorityPreparedSource
```

额外验证本身合理，错误在于由公共 Float32 Unity基座识别并调用具体模型实现。新增其它需要启动前约束的 Float32模型时，公共 Composer必须继续扩张。

## Goals

- 公共 Unity Float32组合代码只依赖 portable Float32合同和 Unity通用 Definition合同。
- Pipeline runtime factories形成一个不可拆分的neutral package，不向公共代码暴露模型专属 catalog类型。
- Prepared Source显式交付匹配的 Runtime Launcher，不增加额外作者配置。
- Launcher只能执行模型专属启动约束并委托唯一 portable Composer，不形成第二个 Runtime构造器。
- Unity Authority与后续Fantasy DotRecast Authority Scene使用同一个ServerAuthoritative Launcher/Host launch语义。
- 迁移完成后公共 Simulation/Unity Composer与Factory builder中不再出现 ServerAuthoritative namespace或具体类型判断。

## Non-Goals

- 不让 Source、Launcher或Pipeline package执行 Program operation、World solve或Pipeline Pass。
- 不让 Launcher选择五项 Composition组件，也不允许 Active Session切换 Launcher。
- 不把每个 Network Model强制做成独立 Composer；只有确有额外启动约束的模型提供专属 Launcher。
- 不把 Runtime Launcher变成 ScriptableObject、场景配置或运行时插件扫描系统。

## Target Chain

```text
五项显式 Composition Definition
  -> Program Runtime创建target-specific Unity Composer
  -> Source preparation返回Prepared Source + Runtime Launcher
  -> selected Pipeline提供Float32 Runtime Package
  -> Unity Composer构造唯一Float32 Composition Request
  -> Prepared Source的Runtime Launcher
       -> Standard Launcher
            -> unique portable Float32 Composer
       -> ServerAuthoritative Authority Launcher
            -> Authority Host launch request validation
            -> same unique portable Float32 Composer
```

Fantasy Server内DotRecast Authority Scene使用相同模型入口：

```text
Authority Scene manifest lowering
  -> same Float32 Runtime Package
  -> same ServerAuthoritative Authority Launcher
  -> same Host launch validation
  -> same portable Float32 Composer
```

## Decision 1: Runtime Launcher由Prepared Source显式提供

`IFloat32SimulationSessionPreparedSource` 增加只读 `RuntimeLauncher`。Source preparation已经拥有模型Endpoint、policy、history资源、locked roster与Source ports，因而它能够在Ready时交付与这些资源匹配的 Launcher。

公共 Composer只调用接口，不读取 Source具体类型。Launcher不拥有Source资源，也不直接Dispose；资源仍由Prepared Source与最终 Runtime handle按现有顺序持有。

### Tradeoff

- 由Pipeline提供Launcher会迫使Pipeline知道Endpoint、Source policy与locked roster，混淆Pass/factory所有权。
- 增加第六个Launcher Definition资产会给作者增加没有业务意义的组合项，并允许错误配对。
- 由Prepared Source提供接口可复用现有显式Source选择与preparation生命周期，同时不让公共基座认识模型类型，因此选择该方案。

## Decision 2: Pipeline Descriptor与三个Factory Catalog形成Neutral Runtime Package

新增 `Float32SimulationPipelineRuntimePackage`，原子保存：

- `SimulationPipelineDescriptor`。
- `SimulationPipelinePassFactoryCatalog`。
- `Float32PipelinePassRuntimeFactoryCatalog`。
- `Float32PipelineProductRuntimeCatalog`。
- 由上述内容形成的稳定package identity。

Package构造时必须核对Pass identity、phase、configuration hash、Product contract与Backend identity。`Float32SimulationSessionCompositionRequest`改为持有整个package，而不是四个可被错误组合的独立参数。

Float32 Pipeline Definition通过 `IFloat32SimulationPipelineRuntimePackageProvider` 显式交付package。Standard Local、Preview与Prediction可复用同一个pass-authored package builder；Authority Definition将portable canonical catalog包装成相同neutral package。公共builder只调用provider接口，不识别具体Pipeline类型；缺失provider直接失败，不使用旧FactorySet或按Pass猜测fallback。

### Tradeoff

- 继续保留四个独立字段改动更小，但公共代码仍需要知道Authority catalog并可能拼出descriptor/factory不一致的组合。
- Neutral package增加一个值对象，却把完整性验证集中到唯一位置，并允许Standard与模型专属Pipeline使用同一输入，因此选择package方案。

## Decision 3: Standard Launcher是唯一无额外模型约束的入口

portable Float32模块提供 `Float32StandardSessionRuntimeLauncher`。它只验证request与runtime package完整，然后调用 `Float32SimulationSessionComposer.Compose(request)`。

Local、Preview与当前ServerAuthoritative Prediction都使用Standard Launcher，因为它们的模型规则已经由Source descriptor、Pipeline compile、restore ports与Pass factory验证，无需额外Host级约束。

Standard Launcher不是fallback。每个Prepared Source必须显式返回Launcher；缺失、Target ABI不匹配或重复调用都直接失败。

## Decision 4: Authority Launcher只增加约束，不创建第二Runtime

portable ServerAuthoritative模块提供 Authority Launcher。它持有Authority Source policy、locked roster与Source握手或Authority Scene manifest锁定的完整Authority PipelineIdentity，接收已经包含neutral runtime package的Float32 Composition Request，并通过既有Host launch request完成：

- Authority Backend与Pipeline id、revision、schema校验。
- neutral package与Authority canonical Pass/Product合同校验。
- Source policy、TickRate、execution support与四个Source port校验。
- Program roster、initial state、World body、output route与locked roster校验。
- Solver runtime/descriptor与Committer/diagnostics校验。
- 唯一portable Composer编译Pipeline后、创建RuntimeHandle前，对编译结果执行完整Authority PipelineIdentity精确校验。

校验成功后仍只调用 `Float32SimulationSessionComposer.Compose(request, expectedPipeline)` 的严格入口；普通入口与严格入口共享同一编译和Runtime构造实现。Authority Launcher不得复制Pipeline compiler、Backend request、LaunchPlan或Runtime handle创建。

现有 `ServerAuthoritativeAuthorityHostLaunchRequest` 保留为模型内的完整校验对象，但改为消费neutral runtime package，不再要求公共 Unity代码传递 `ServerAuthoritativeAuthorityPipelineCatalogSet`。

### Tradeoff

- 删除Host launch校验并让Authority直接使用Standard Launcher可以减少代码，但会失去Host portability change锁定的Source policy、roster和canonical catalog闭环。
- 复制一个Authority Composer会让模型拥有第二套Runtime构造。
- 专属Launcher执行额外校验后委托同一Composer，保留约束且不复制Runtime，因此选择该方案。

## Decision 5: 公共Unity Composer只负责Request Lowering

`UnityFloat32SimulationSessionComposer`继续作为Program Runtime Definition创建的target-specific Unity adapter，但其职责仅包括：

- 强类型校验Program Runtime、Backend、Solver与Actor registration属于Float32 ABI。
- 创建Program Runtime、Solver、initial state、Committer、diagnostics和output routes。
- 从所选Pipeline provider取得neutral runtime package。
- 构造一次 `Float32SimulationSessionCompositionRequest`。
- 调用Prepared Source明确提供的Runtime Launcher。
- 在失败时独立尝试释放Solver与Source；清理失败只能附着到原异常，不能覆盖原始结构化failure。

它不得引用ServerAuthoritative namespace，不得判断Model、Pipeline或Prepared Source具体类型，也不得拥有Launcher registry。

## Decision 6: 五项Composition不增加Launcher配置

Program Runtime、Execution Backend、Pipeline、Session Source与WorldSolver仍是唯一五项显式Composition选择。Launcher不是第六个业务维度；它是Prepared Source运行合同的一部分，必须与Source descriptor和Target ABI固定匹配。

如果作者选择不兼容的Source、Pipeline或Backend，package provider、Launcher或portable Composer必须在Runtime创建前失败，不得替换为Standard Launcher。

## Identity And Compatibility

- ProgramHash、LayoutHash、PipelineId/Revision/Hash、Source identity、Source policy hash、Solver identity和Composition identity保持不变。
- Runtime package identity只用于启动完整性校验和diagnostics，不写入Gameplay packet或替换现有PipelineHash。
- Launcher identity包含Source、policy、locked roster与expected Authority PipelineIdentity，只用于diagnostics和启动错误，不成为新的网络模型选择字段。
- Protocol、checkpoint、snapshot、command与reliable event bytes保持不变。

## Failure And Ownership

- Pipeline没有Float32 package provider：Composition失败。
- Prepared Source没有Launcher：Preparation结果无效并失败。
- Launcher与Target ABI、expected Authority PipelineIdentity、Pipeline package或Source descriptor不兼容：Runtime创建前失败。
- Authority Launcher约束失败：不调用portable Composer，也不回退Standard Launcher。
- portable Composer失败：Launcher透传原始结构化failure。
- Unity request lowering失败时，Composition owner必须分别尝试释放Solver与Prepared Source；任一Dispose失败不得阻止另一资源释放，也不得覆盖原始结构化failure。

## Migration Order

1. 锁定当前identity、调用点与删除清单。
2. 建立neutral runtime package及一致性校验。
3. 建立Launcher合同与Standard Launcher。
4. 让全部Float32 Pipeline Definition提供package。
5. 让全部Float32 Prepared Source显式提供Launcher。
6. 将Unity Composer收敛为request lowering并删除具体类型分支。
7. 接入ServerAuthoritative Authority Launcher与Host launch request。
8. 删除旧FactorySet分支和旧具体转换。
9. 更新DotRecast依赖说明后再接入Fantasy Authority Scene装配。
