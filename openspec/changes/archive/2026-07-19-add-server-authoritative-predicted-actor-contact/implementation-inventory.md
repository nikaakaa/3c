# 实施清单：ServerAuthoritative预测远端Actor接触

## 唯一数据链

```text
Fantasy authority snapshot
  -> AuthoritativeObservationBatch
  -> Prediction Observation Ingress
  -> ServerAuthoritativePredictionState
  -> History-owned RemoteBodyTimeline
  -> Schedule target-tick selection
     -> selected remote Body product
     -> [ObservedKinematicActorContact feature]
        -> ObservedWorldConstraintFrame
        -> Float32SimulationStep
        -> WorldSolveBatchRequest
        -> DotRecastWorldSolver
        -> ActorContactSolver(active/observed)
  -> outer transaction Commit
  -> Remote Presentation Egress
  -> selected Body buffer
  -> render interpolation
```

原始authority Body不再直接进入Remote Presentation。Schedule是唯一tick选择owner；Presentation只消费成功Current step提交的selected Body。Replay只读取History record保存的观察frame，不发布实时Body。

## 公共Float32合同

- `SimulationWorldContracts.cs`：`ObservedWorldConstraintFrame`是每个Step的必填值，按ActorId排序并进入World request hash；空约束也必须携带目标tick。
- `Float32PipelineProducts.cs`：`Float32SimulationStep`显式拥有观察frame。
- Local、Authority和Preview提供正式空frame。
- ServerAuthoritative Prediction先产生model-neutral selected Body frame。声明`ObservedKinematicActorContact`能力时转换为观察约束；未声明时向World提供正式空frame，仍由同一selected Body驱动远端表现。

## Prediction状态与恢复

- `ServerAuthoritativePredictionHistory.cs`：History模块拥有唯一RemoteBodyTimeline、locked remote roster、稳定tick样本、有界容量、插值和有限常速外推。
- `ServerAuthoritativePredictionStateCodec.cs`：History canonical schema为v2，保存Timeline和每条History record实际使用的观察frame。
- Correction v3、History v2、Journal v2顺序保持不变。
- Priming期间没有完整remote anchor时产生零Current step，pending离散请求不被消费。
- HardRecovery通过正式Egress重置selected Body stream；后续成功Current step提交新anchor。

## DotRecast求解

- `ActorContactSolver.cs`：同一Solver处理`ActiveSimulated`与`ObservedKinematic`。
- Active/Active保持原对称裁剪。
- Active/Observed使用相对轨迹扫掠，只修改Active的位置并保留切向分量。
- Observed/Observed不产生可提交修正。
- `DotRecastWorldSolver.cs`只为active roster执行Surface candidate、Surface reconstraint、Result和NextWorldState提交；observed只参加同批接触与最终间距校验。
- 观察frame的接触形状hash必须匹配Solver锁定配置。

## Composition与身份

- DotRecast Solver version与configuration identity已升级，并声明`ActorCollision | ObservedKinematicActorContact`。
- Unity CharacterController Solver未声明未实现的观察接触能力。
- Corin DotRecast Client A/B Prediction Composition要求`ActorCollision | ObservedKinematicActorContact`。
- ServerAuthoritative policy使用`MaximumRemoteBodyExtrapolationTicks`，旧`RemoteInterpolationDelayTicks`已删除。
- History schema、Remote Egress schema、Authority replication schema与Model identity已破坏性升级，没有旧reader或双写。
- 标准Float32 Pass与ServerAuthoritative Prediction Pass canonical implementation均为v2；对应6个标准Pass资产和7个Prediction Pass资产已同步迁移。Authority专属Pass继续保持v1。

## 诊断

- Prediction：timeline容量、首尾tick、淘汰数、Current/Replay目标tick、采样方式、来源tick、frame hash、外推上限拒绝、baseline时远端frame差异。
- DotRecast：观察frame hash与World request hash关联、Actor pair、两侧mobility、TOI、normal clip、depenetration和失败原因。
- 诊断只读取正式状态，不参与采样、求解、校正或提交。

## 已删除路径

- 原始authority Body直接进入Remote Presentation的旁路。
- Remote Presentation独立Body authority cursor和独立Body delay。
- `RemoteInterpolationDelayTicks`资产字段、Inspector字段和policy序列化。
- History v1 reader、v1 schema常量与v1/v2双写可能性。
- 第二Remote Body timeline、Presentation Transform反向进入World、第二Actor碰撞调用均不存在。

## 保持不变

- Authority仍以完整active roster执行一次DotRecast `ResolveBatch`。
- Unity Authority的Composition和DotRecast Authority roster配置不变。
- Fixed Rollback源码、资产、spec、构建产品和KCC合同不在本change范围。
## Corin Producer复制策略迁移

- `CorinServerAuthoritativeHybridModel.asset`的可靠Producer集合已从16项迁移为25项。
- 新增9项分别来自Attack3、Attack4、Attack5，每个攻击包含1个Timeline Track Producer与2个Clip Producer。
- 策略集合与当前`CorinCharacterPipelineDefinition.PresentationProjection.asset`的Program Producer集合完全一致；没有保留废弃Producer，也没有放宽`RequireProgramCoverage`。

## Selected Remote Body产品注册

- `CorrectionSchedule`是`SelectedRemoteBodyBatch`的唯一writer，`RemotePresentationEgress`是正式reader。
- `ServerAuthoritativePipelineProductSlots`注册唯一`OuterTransaction` slot，使Schedule产物可跨Step抵达Egress。
- Product Contract、Pass descriptor、runtime binding与Pass-authored runtime package使用同一`ServerAuthoritativeProducts.SelectedRemoteBodyBatch`身份。
