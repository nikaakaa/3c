# 实施盘点

## 迁移前调用链

```text
SimulationSessionCompositionPreparation
  -> ISimulationSessionComposer.Compose
  -> UnityFloat32SimulationSessionComposer
  -> Float32SimulationPipelineFactorySet.Build
  -> Float32SimulationSessionCompositionRequest
  -> Float32SimulationSessionComposer.Compose
```

迁移前 Authority 在公共 Unity 组合层分叉：

```text
Float32SimulationPipelineFactorySet
  -> ServerAuthoritativeAuthorityPipelineDefinition.BuildPortableCatalog
  -> AuthorityCatalog

UnityFloat32SimulationSessionComposer
  -> ServerAuthoritativeAuthorityPreparedSource 强制转换
  -> ServerAuthoritativeAuthorityHostLaunchRequest.Launch
  -> Float32SimulationSessionComposer.Compose
```

迁移前 `Float32PassExecutionBackendDefinition.BuildPortableFactoryCatalog` 同样通过旧
`Float32SimulationPipelineFactorySet` 取得 portable factory catalog，属于必须同步迁移的
authoring compatibility 调用点。

## Prepared Source

- Local：`LocalSimulationSessionPreparedSource`。
- Preview：`PreviewSimulationSessionPreparedSource`。
- ServerAuthoritative Prediction：`ServerAuthoritativePreparedSource`。
- ServerAuthoritative Authority：`ServerAuthoritativeAuthorityPreparedSource`。

四者均实现 `IFloat32SimulationSessionPreparedSource`。迁移前接口只交付 descriptor、ports、
restore source 与 source egress；Launcher 尚未成为正式合同。

## Pipeline Lowering

- Standard Local、Preview、ServerAuthoritative Prediction 由四个 phase 的
  `IFloat32SimulationPipelinePassRuntimeProvider` 生成 descriptor、portable factories、
  runtime factories 与 Product runtime catalog。
- ServerAuthoritative Authority 由 portable
  `ServerAuthoritativeAuthorityPipelineCatalog.Create` 原子生成同一组输入。
- 迁移前公共 `Float32SimulationPipelineFactorySet` 通过具体 Pipeline 类型判断选择以上两条路径，
  并额外暴露模型专属 `AuthorityCatalog`。

正式资产只使用以下四个具体 Pipeline Definition：

- `StandardLocalSimulationPipelineDefinition`。
- `PreviewSimulationPipelineDefinition`。
- `ServerAuthoritativePredictionPipelineDefinition`。
- `ServerAuthoritativeAuthorityPipelineDefinition`。

仓库不存在直接使用 plain `SimulationPipelineDefinition` 的正式资产，因此无需资产类型迁移。

## Identity 输入

- ProgramHash 与 LayoutHash 来自已经编译的 `Float32ProgramRuntime`，本 change 不修改 Program 或 ABI。
- PipelineHash 来自既有 `SimulationPipelineDescriptor.DescriptorHash` 及 compiled plan，package 只包装原 descriptor。
- Source policy hash 来自 `ServerAuthoritativeAuthoritySourcePolicy` canonical bytes，本 change 不修改 policy codec。
- Authority Launcher从Source握手或Authority Scene manifest接收完整expected Authority PipelineIdentity；它只用于启动期精确核对，不形成第二份Pipeline identity。
- Composition identity 继续由 Session、World、Clock、TickRate、Program、Backend、compiled Pipeline、
  ProgramCatalog、roster、Source、Solver、Snapshot codec、Committer、Model、Endpoint 与 Protocol 形成。
- Runtime package identity 与 Launcher identity 只用于启动校验和 diagnostics，不进入上述网络与 Composition identity。

## Authority Host 启动约束

- canonical Float32 Backend。
- canonical Authority Pipeline descriptor、Pass factories、runtime factories 与 Product runtime。
- Authority Source policy、TickRate、execution support 与 Pipeline identity。
- Runtime Package descriptor先核对expected Authority Pipeline id、revision、schema；唯一portable Composer在同一次正式编译后、创建RuntimeHandle前核对完整expected PipelineHash。
- 禁止 restore source，Prepared Source 必须进入资源 owner。
- AcceptedInput、AuthorityClock、FullBaselineRequest、AuthoritySend 四个精确 Source port。
- locked roster、Program roster、initial Character state、World body 与 output route 一致。
- Solver runtime、descriptor 与 initial World state 一致。
- Committer 与 diagnostics 必须存在。

## 删除清单与并行边界

- 删除公共 Unity Composer 的 Authority Source 强制转换和 Authority 条件分支。
- 删除公共 Factory builder 的 ServerAuthoritative import、具体 Pipeline 判断与 `AuthorityCatalog`。
- 删除 Composition Request 中四个可独立组合的 Pipeline/factory 参数。
- 不保留旧 overload、fallback Launcher、字符串 registry、反射扫描或兼容 adapter。
- 不修改协议、Solver、Program ABI、Pipeline Pass 语义或 Scene 资产。
- `add-dotrecast-authoritative-server-backend` 当前尚未进入 task 3.12，必须在本 change 完成后消费新 Authority Launcher。
- `refactor-agent-authoring-compiler-modules` 可继续并行；其工作区改动不属于本 change，不回退。

## 迁移后正式调用链

```text
SimulationSessionCompositionPreparation
  -> ISimulationSessionComposer.Compose
  -> UnityFloat32SimulationSessionComposer
  -> IFloat32SimulationPipelineRuntimePackageProvider.BuildRuntimePackage
  -> IFloat32SimulationSessionRuntimeLauncher.Launch
  -> Float32SimulationSessionComposer.Compose
  -> Float32PassExecutionBackend
```

## 迁移结果核对

- 四份正式Pipeline的`BuildPortableDescriptor`和canonical Pass构造未修改；Runtime Package只包装同一descriptor，因此DescriptorHash与PipelineHash形成输入不变。
- Program artifact、Source policy codec、Solver descriptor、Composition descriptor和协议codec均未修改，因此ProgramHash、Source policy hash、Composition identity与packet/checkpoint bytes不引入新输入。
- `Float32PassExecutionBackend.Create`与`Float32PassBackendCompositionRequest`仍只由portable `Float32SimulationSessionComposer`创建。
- Standard Launcher与Authority Launcher最终都调用同一个portable Composer；Authority Launcher只增加policy、roster、port与canonical package约束。
- Unity Authority Prepared Source传入`Compatibility.AuthorityPipeline`；Fantasy DotRecast Authority Scene后续从其canonical manifest传入同一字段。
- Unity request lowering的失败清理会分别尝试Solver与Prepared Source；清理异常附着到原异常，不覆盖原结构化failure或堆栈。
- 公共`UnityFloat32SimulationSessionComposer`和pass-authored package builder不再引用ServerAuthoritative namespace或具体Source/Pipeline类型。
- pass-authored package builder文件已正式命名为`Float32SimulationPipelineRuntimePackageBuilder.cs`，不再保留旧FactorySet文件名。
- DotRecast Authority Scene manifest迁移属于`add-dotrecast-authoritative-server-backend`的2.1至2.20，不在本change中建立半迁移类型或兼容别名。
