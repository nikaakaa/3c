## Context
当前主线已经形成：

```text
FullBodyActionTickAdapter / PlayerFullBodyActionController.Tick
  -> CharacterFramePipeline
  -> FullBodySubmissionBuilder
  -> LocomotionFrameBuilder / unified CharacterStateMachineRunner
  -> CharacterFrameOutputApplier
```

这个顺序解决了“只有一个角色帧管线”的问题，但 Module Interface 还不够窄：

- `CharacterFramePipeline` 的调用面仍是 `PlayerFullBodyActionController`。
- `FullBodySubmissionBuilder` 仍通过 `PlayerFullBodyActionController` 访问 `PlayerLocomotionController`、runner、input buffer、配置和 interrupt policy。
- `PlayerFullBodyActionController` 仍承载 Unity host、runner owner、端口装配、输出副作用、snapshot restore、diagnostics 和配置解析。
- `PlayerLocomotionController` 仍承载 Locomotion runtime adapter、frame builder facade、motion/playback/window、snapshot restore、diagnostics 和引用解析。

大类本身不是问题；问题是这些大类的 Interface 太宽，调用方必须理解它们内部的顺序和状态。删除测试可以看出：如果删除这些大类的宽 Interface，复杂性会重新散落到 pipeline、builder、tick adapter 和测试里。因此下一步不是按行数机械拆文件，而是在当前主线处建立更深的端口 Module。

具体混杂点：

- `PlayerLocomotionController`
  - 旧 direct tick 入口仍在类内作为 retired/diagnostic 路径。
  - 新 `LocomotionFrameBuilder` facade 也在同一类内。
  - rollback snapshot capture/restore 也在同一类内。
  - diagnostic、reference resolve、camera/facing resolve 分布在同一文件尾部。
  - 这说明它的变化原因包括 Unity host、input reader、Locomotion decision builder、animation playback clock、motion executor adapter、rollback owner、diagnostic adapter、reference resolver 和 camera/facing resolver。
- `PlayerFullBodyActionController`
  - 已经不再是 phase owner，但仍是 Character 管线背后的操作面板。
  - Unity tick host、pipeline for-pipeline 方法、runner rebuild、reference resolve 和 interrupt policy cache 混在同一 Module。
- `CharacterFramePipelineTypes`
  - request submission、frame input、frame context、frame output/result 放在一个文件里。
  - 当前可以接受，但后续 UpperBody、HitReaction、Aim layer 继续加字段时会变成角色帧总线对象。
- `CharacterStateMachineTypes`
  - 通用层级状态图类型和角色业务词混在一起。
  - 这是通用运行时口径问题，但当前风险低于两个行为大类。

## Goals
- 让 `CharacterFramePipeline` 依赖角色帧运行时端口，而不是具体 MonoBehaviour。
- 让 `FullBodySubmissionBuilder` 依赖 FullBody/Locomotion 提交所需的窄端口，而不是通过 controller 取一切。
- 让 `PlayerFullBodyActionController` 成为 Unity host 和端口装配 adapter。
- 让 `PlayerLocomotionController` 成为 Locomotion runtime adapter，并保留 Unity 对象、playback/window、snapshot restore 的当前权威。
- 优先收口 `PlayerLocomotionController` 的对外 Interface，再收口 `PlayerFullBodyActionController` 的 pipeline 操作面板。
- 通过 fake adapter 让 pipeline 和 submission builder 能在 EditMode 中不创建完整 GameObject 也能验证顺序。
- 保持当前 FullBody-only 行为、Dodge、TurnBack、WASD、rollback replay 入口不回退。

## Non-Goals
- 不新增 UpperBody、LowerBody、Facial、IK、Additive 或 AvatarMask layer。
- 不新增第二套状态机、第二个 frame pipeline、第二个 motion executor 路径或 Locomotion 自驱主线。
- 不迁移 playback restore/window 的权威语义。
- 不重写 `LocomotionFrameBuilder` 的纯数据构建职责。
- 不在本变更中拆分 `CharacterFramePipelineTypes` 的数据文件；只用静态测试防止继续膨胀为副作用总线。
- 不在本变更中拆分 `CharacterStateMachineTypes` 的通用/业务 model；该问题应由独立状态机 model 收敛变更处理。
- 不创建 fallback 配置、全局单例或 `Resources.Load` 路径。
- 不为了“面向接口”给每个 helper 都创建接口；只在 pipeline/runtime adapter seam 上建立端口。

## Decisions

### Decision: 端口只放在真实 seam 上
端口 Interface 只用于 Character frame pipeline、FullBody submission builder 和 Locomotion runtime adapter 之间。纯函数 helper、只有内部调用的 solver 和数据模型不因本变更新增接口。

理由：一个没有替换点的接口只是浅 Module，会增加调用者要理解的 Interface。这里的真实 seam 是“纯帧管线”和“Unity runtime host”之间，以及“FullBody 提交者”和“Locomotion runtime adapter”之间。

### Decision: Pipeline 不直接依赖 MonoBehaviour host
`CharacterFramePipeline` 的 public/inner phase 方法应接收角色帧运行时端口或等价契约。该端口暴露 input buffer 写入、FullBody submission 构建、output apply 和 diagnostics 所需能力，但不得暴露 Unity scene object。

理由：Pipeline 的 depth 应来自固定 phase 顺序和提交语义。它不应该知道调用者是 `PlayerFullBodyActionController`、测试 fake，还是未来的预测回放 adapter。

### Decision: FullBody host 保留 runner owner，并由包装 adapter 暴露端口
`PlayerFullBodyActionController` 仍是正式 runner owner 和 Unity host，但第一阶段优先通过 `FullBodyRuntimePortAdapter` 或等价内部 adapter 包装它，而不是让 controller 直接实现所有 pipeline port。pipeline 和 builder 的 Interface 不再是完整 controller 类型。

理由：当前 specs 要求 FullBody 主入口拥有唯一 runner。迁移不能把 runner owner 移到 Locomotion 或新的控制器路径。同时，如果让 controller 直接实现端口，容易只是把 concrete controller 换成 interface，`ForPipeline` 操作面板仍留在同一个大 Module。包装 adapter 能先把 pipeline 与 controller 解耦，再逐步把 implementation 从 controller 中搬出。

### Decision: Locomotion 第一阶段拆成 frame runtime port 和 output runtime port
Locomotion 第一阶段使用两个端口：`ILocomotionFrameRuntimePort` 或等价端口供 `FullBodySubmissionBuilder` 构建 prepare/evaluate/build 纯数据结果；`ILocomotionOutputRuntimePort` 或等价端口供 Character output apply 执行 motion、present animation、写 runtime facts 和 complete tick。两者都不得注册 tick driver、推进第二状态机或提交第二份 base layer 输出。

理由：prepare/build 与 output/apply 属于不同 phase 语义。合成一个巨大 `ILocomotionRuntimePort` 会把 `PlayerLocomotionController` 的宽 Interface 换名成接口版，仍然是浅 Module。拆成两个端口能让调用方只看到当前 phase 需要的能力。

### Decision: 测试 fake 是端口设计的一部分
每个新增端口必须至少有生产 adapter 和测试 fake 两种使用方式。测试 fake 用于证明 phase 顺序、request submission、frame submission 和 output apply 不依赖 Unity scene object。

理由：只有生产实现而没有替换点的接口会变成假 seam。测试 fake 让 Interface 成为可验证的测试面。

### Decision: 先端口化，后继续拆实现
第一阶段以调用方依赖收窄为验收标准，不强求一次性把 `PlayerLocomotionController` 的 playback/window、snapshot restore 和 diagnostics 全部搬出。实现拆分必须服从当前 active changes 的职责归属。

理由：当前 active changes 很多，一次迁移所有内部实现会扩大风险。先收窄 Interface，后续再按端口背后的 implementation 拆模块。

### Decision: 模型大文件需要独立 change，但不进入本次主拆分
`CharacterFramePipelineTypes` 和 `CharacterStateMachineTypes` 的问题是概念聚合和文件膨胀，不是当前最危险的行为混杂点。本变更只允许为端口契约新增必要纯数据类型，不做大规模 model 文件重排。后续应分别规划角色帧数据契约拆分和状态机 model 口径收敛 change。

理由：把行为端口化和 model 口径收敛合在一起，会让验证面过大，也容易在未审批情况下改动状态机配置语义。

## Proposed Shape
命名可以在实施时按现有目录微调，但职责应保持：

```text
Character/Pipeline/
  Contracts/
    ICharacterFrameRuntimePort.cs
  Model/
    CharacterFramePipelineTypes.cs
  Runtime/
    CharacterFramePipeline.cs

Character/Action/FullBody/
  Contracts/
    IFullBodySubmissionRuntimePort.cs
    IFullBodyOutputRuntimePort.cs
  Runtime/
    PlayerFullBodyActionController.cs
    FullBodyRuntimePortAdapter.cs

Character/Movement/
  Contracts/
    ILocomotionFrameRuntimePort.cs
    ILocomotionOutputRuntimePort.cs
```

端口应以“当前调用方真正需要什么”为准。角色级端口放在 `Character/Pipeline/Contracts`；FullBody 侧第一阶段优先创建包装 adapter；Locomotion 侧第一阶段保留 prepare/build 与 output/apply 两类端口。不得让 `CharacterFramePipeline` / `FullBodySubmissionBuilder` 继续直接依赖完整 MonoBehaviour 大类。

## Sequencing
1. 先加静态和 fake adapter 测试，锁住当前直接依赖问题。
2. 先收窄 `PlayerLocomotionController` 对外 Interface，让 FullBody 只通过 Locomotion runtime port 访问子职责。
3. 抽出 Character frame runtime port，迁移 `CharacterFramePipeline` 的 phase 方法。
4. 抽出 FullBody submission runtime port，迁移 `FullBodySubmissionBuilder`。
5. 让 `PlayerFullBodyActionController` 和 `FullBodyActionTickAdapter` 只负责装配端口并进入同一 `CharacterFramePipeline`。
6. 保留现有行为回归测试，确认没有生成第二路径。
7. 记录两个 model 大文件需要后续独立 change，但不在本变更重排它们。

## Risks / Trade-offs
- Risk: 端口过大，变成 controller 的换名。
  - Mitigation: 静态测试限制端口文件对 Unity scene object 和具体 controller 的依赖；任务按调用组逐步切。
- Risk: 接口过多，Module 变浅。
  - Mitigation: 只为 pipeline/runtime seam 建接口，helper 不抽接口。
- Risk: 与 Locomotion builder active change 重叠。
  - Mitigation: 本变更只收窄 builder 外围端口，不迁移 builder 内部主干。
- Risk: rollback/playback restore 被顺手改语义。
  - Mitigation: tasks 明确 snapshot/playback 只经 characterization 覆盖，不在本变更重定义。

## Resolved Decisions
- `PlayerFullBodyActionController` 不作为首选直接端口实现；第一阶段优先由 `FullBodyRuntimePortAdapter` 或等价包装 adapter 暴露 pipeline 需要的端口。
- Locomotion port 第一阶段拆成 `ILocomotionFrameRuntimePort` 和 `ILocomotionOutputRuntimePort` 或等价 prepare/output 两类端口。
- `CharacterFramePipelineTypes` 和 `CharacterStateMachineTypes` 需要后续独立 model 拆分 change；本变更只记录风险、防止继续膨胀，不直接处理。
