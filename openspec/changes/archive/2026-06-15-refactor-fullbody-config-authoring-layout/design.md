## Context
当前配置已经具备统一状态机、动作逻辑、基础移动动画、Animancer transition 和 motion profile 等 Module，但作者 Interface 仍然浅：同一个 Dodge 或 TurnBack 需要同时理解状态机 node 字段、modules、Action config、request policy、animation profile、motion profile 和目录历史。删除测试上做一个“删除测试”：如果删除当前 Dodge Action Config 或 state machine 里的旧 `output`，复杂度不会消失，而会分散到运行时读取路径和资产手工同步中，说明现在的 Seam 还不够深。

## Goals / Non-Goals
- Goals:
  - 让 `CharacterConfig` 或等价角色配置根成为设计者的正式作者入口。
  - 让目录结构本身表达 StateMachine、Action、Animation、Movement、Input、Camera 的 Module 归属。
  - 让 Dodge motion、TurnBack motion profile、request policy 和 animation key 都只有一个正式配置权威。
  - 用自动测试和静态校验锁住目录、命名、引用和重复字段。
- Non-Goals:
  - 不新增第二套角色状态机 runner。
  - 不重写 Animancer Presenter。
  - 不实现轻攻击、跳跃、受击或通用动作编辑器。
  - 不通过 fallback 配置维持旧资产可运行。
  - 不在本变更中引入 Resources、全局单例或硬编码路径加载。

## Decisions
- Decision: 角色配置根是作者入口，不是运行时 fallback。
  - Reason: 一个深 Module 的 Interface 应该让设计者从一个入口追踪正式子配置，同时运行时仍必须因为缺失正式配置而报错停止相关输出。
- Decision: `Assets/Configs/3C` 使用职责优先目录，而不是历史类型名优先目录。
  - Reason: 目录是资产作者最先接触的 Interface。目录名表达职责后，新增 Attack/Jump 不需要复制 Dodge 的散配置搜索路径。
- Decision: Action motion 参数归 Action 逻辑配置，状态机节点只保存 state id、variant key、request/timeline/animation semantic key 和必要能力模块。
  - Reason: 状态机 Module 的 Depth 来自“决定逻辑状态”，不是保存每个动作的 gameplay 数值。动作数值放在 Action Module 能提高 Locality。
- Decision: TurnBack motion profile 归 Locomotion Animation/Motion 配置，状态机只输出 `Locomotion.Turn.Back` 或等价 source id。
  - Reason: baked motion profile 是动画运动源数据，不应让状态机资产引用测试命名 profile 或重复 alias。
- Decision: 默认请求策略集合采用 FullBody/State request 命名。
  - Reason: 该资产已经承载 TurnBack 和 Dodge，不再是 Dodge-only Adapter。命名必须反映 Interface 覆盖范围。
- Decision: 默认策略集合正式命名为 `CorinFullBodyStateRequestPolicySet.asset`。
  - Reason: `FullBody` 表达运行域，`StateRequest` 表达它服务的是进入 Dodge、TurnBack 和后续状态请求的准入策略，不再误导为 Dodge-only 或 Action-only。
- Decision: 现有 `CharacterConfig.asset` 正式迁移并命名为 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
  - Reason: 角色配置根是角色作者入口，应按角色归属组织；根目录不继续作为短期正式入口，避免后续多角色时扩散平铺配置。
- Decision: 角色正式资产使用 `Corin + 语义` 命名，不继续使用 `Default`。
  - Reason: 这些资产不是模板，而是 Corin 的正式配置；`Default` 会隐藏角色归属，让 Interface 继续依赖人的记忆。
- Decision: 本次只将 Generic Animancer transition library 作为 Corin 的正式 rig variant。
  - Reason: 当前实现目标优先收口可琳正式配置，Humanoid 暂不进入必需配置闭环；若保留 Humanoid 资产，只能作为参考、测试或未来另行审批的 rig variant。
- Decision: 目录迁移保留 `.meta` GUID，并通过引用校验确认 prefab/scene/root config 没有断链。
  - Reason: Unity 资产引用稳定性比文件名更重要；目录清理不能变成隐式重建配置。

## Target Layout
```text
Assets/Configs/3C/
  Character/
    Corin/
      CorinCharacterConfig.asset
  StateMachine/
    FullBody/
      CorinFullBodyStateMachine.asset
  Action/
    FullBody/
      CorinFullBodyActionSet.asset
      RequestPolicy/
        CorinFullBodyStateRequestPolicySet.asset
      Dodge/
        CorinDodgeActionConfig.asset
  Animation/
    Corin/
      Locomotion/
        CorinLocomotionAnimationConfig.asset
        MotionProfiles/
        FootPhase/
      FullBody/
        Action/
      Animancer/
        RigVariants/
          Generic/
            CorinGenericAnimancerTransitionLibrary.asset
            Transitions/
        Parameters/
  Movement/
    BasicMovementConfig.asset
  Input/
    CharacterInput.inputactions
  InputReferences/
  Camera/
```

`Character/Corin/CorinCharacterConfig.asset` 是对现有 `CharacterConfig.asset` 的正式迁移目标；实施阶段不得同时保留根目录 `CharacterConfig.asset` 作为第二正式入口。`Default*` 旧名可以作为迁移输入被搜索和更新，但不得作为迁移后的正式资产名。

## Risks / Trade-offs
- Risk: 目录迁移与当前未提交资产变更重叠。
  - Mitigation: 实施前先列出 dirty assets，逐项判断是否属于本迁移；不得回退用户改动。
- Risk: 移除 state machine 旧 `output` 字段前运行时仍读取旧字段。
  - Mitigation: 先加校验和 characterization，再修改读取路径，最后清理资产字段。
- Risk: `Animacer/Pramater` 拼写修正影响 transition library 引用。
  - Mitigation: 迁移保留 `.meta`，并增加静态引用校验。
- Risk: 现有 `DefaultDodgeInterruptPolicySet`、`DefaultDodgeActionConfig` 或 `DefaultRunLocomotionAnimationConfig` 被测试或 prefab 按名称查找。
  - Mitigation: 禁止硬编码路径/名称；若发现名称查找，先改为正式引用或 GUID 引用。

## Migration Plan
1. 先实现只读校验，暴露当前重复字段、旧目录、测试资产引用和缺失根引用。
2. 再接入正式 Action motion/config 解析路径，确保运行时不再需要 state machine 中重复的 Dodge 数值。
3. 移动和重命名资产目录，保留 `.meta` 并验证 GUID 引用。
4. 清理默认状态机旧 `output` 双权威字段和测试 motion profile 引用。
5. 最后收紧测试，确认旧目录和旧拼写不再作为正式配置入口。

## Resolved Questions
- `CharacterConfig.asset` 正式迁到 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`。
- 默认请求策略集合正式命名为 `CorinFullBodyStateRequestPolicySet.asset`。
- 正式角色资产采用 `Corin + 语义` 命名，`Default` 不用于 Corin 正式资产。
- 本次只做 Generic 正式 rig variant；Humanoid 暂不纳入正式配置闭环。
