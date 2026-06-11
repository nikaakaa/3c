## Context
项目运行时已经形成基础 Locomotion、FullBody Action、Dodge module、动画 Presenter 和 motion executor 的分层边界。状态调试需要更多可见性，但日志系统不能成为新的状态权威，也不能让状态机为了日志依赖 Unity 表现对象。

当前相机组件里存在局部 `debugLog` 和 `debugLogInterval`，这是临时可用的组件级调试方式。新日志系统先建立统一入口和宏裁切能力，后续是否迁移现有相机日志需要单独审批。

## Goals / Non-Goals
- Goals: 提供统一诊断日志入口、分类开关、通道 key 开关、编译宏裁切和状态机日志接入。
- Goals: 提供一个可挂到场景对象上的 Inspector 开关控制器，用于 Play Mode 中开关通道 key 和按前缀/后缀/包含文本快速批量筛选。
- Goals: 通过 EditMode 测试证明宏裁切边界、分类过滤、格式稳定和状态机日志只消费快照事实。
- Goals: 给用户明确 Play Mode 验证方式，能看到 FullBody/Locomotion/Dodge 状态变化。
- Non-Goals: 不做文件落盘日志、远程日志、运行时屏幕 UI、日志聚合服务器或性能分析器。
- Non-Goals: 不删除现有局部日志，不强制把第三方包或美术工具日志接入本系统。
- Non-Goals: 不修改状态机状态数量、transition 条件、运动命令或动画播放规则。

## Decisions
- Decision: 使用项目自有轻量 `RuntimeDiagnosticLog` 作为统一门面，而不是直接在业务代码散落 `Debug.Log`。
  - Reason: 门面可以集中处理分类、等级、格式和裁切；业务模块只表达“发生了什么”。
- Decision: 常规诊断日志使用 `THIRDPERSON_DIAGNOSTIC_LOGS` 编译宏控制。
  - Reason: 该符号可通过 Unity Scripting Define Symbols 或构建配置开启；未开启时诊断日志调用不进入普通构建产物。
- Decision: 运行时开关只作为宏开启后的二级过滤。
  - Reason: 宏负责打包裁切，运行时分类开关负责 Play Mode 中临时观察，二者职责分离。
- Decision: 每条或每组日志使用稳定 `ChannelKey` 作为日常调试开关；未显式提供 key 时使用 `分类.消息` 作为默认 key。
  - Reason: 分类适合作大分组，通道 key 才能支撑新增日志后的细粒度开关和前后缀批量筛选。
- Decision: Inspector 开关控制器只读写 `RuntimeDiagnosticLog.Filter` 的通道 key 状态，不提供测试日志按钮。
  - Reason: Inspector 入口是统一门面的过滤前端，不制造额外日志噪声，不拥有独立日志状态，不读取状态机内部对象，也不绕过宏裁切出口。
- Decision: Editor/测试环境可直接验证 Inspector 开关；Player 构建仍以 `THIRDPERSON_DIAGNOSTIC_LOGS` 作为最终开关。
  - Reason: 场景组件便于开发期试日志，发布期是否保留诊断输出由编译符号决定。
- Decision: 状态机日志只接收 `FullBodyStateSnapshot`、Locomotion phase/path、Action tracker snapshot 和仲裁结果等纯数据。
  - Reason: 日志层不得读取 UnityHFSM 内部对象、Animancer state、Animator state、CharacterController 或输入动作对象。
- Decision: 第一版只在统一主链入口和状态树 driver 周边记录关键状态变化。
  - Reason: 避免每帧刷屏；优先记录状态路径变化、Action accepted/rejected、Dodge complete、异常配置缺失等对调试最有价值的事件。

## Risks / Trade-offs
- Risk: 每帧日志过多影响 Play Mode 可读性。
  - Mitigation: 默认只记录状态变化和关键 Action 决策；需要连续帧输出时必须通过分类或采样开关明确开启。
- Risk: 宏裁切导致测试环境难以覆盖日志行为。
  - Mitigation: 将格式化和过滤规则拆为可测试纯逻辑；日志发射入口用宏包裹，静态测试验证宏存在。
- Risk: 日志接入状态机后误把日志系统做成状态观察旁路。
  - Mitigation: 规格要求日志只消费已有快照，不反向驱动状态，不持有状态机内部对象。
- Risk: 与活跃 FullBody HFSM 变更重叠。
  - Mitigation: 本变更依赖现有或活跃变更产出的 `FullBodyStateSnapshot`；若实现时该变更未归档，只在其已审批主线内接入，不复制第二套快照。

## Migration Plan
1. 新增诊断日志模型、分类、等级、门面和可测试过滤逻辑。
2. 给 FullBody 主调度入口接入状态路径变化和 Action 决策日志。
3. 给 Locomotion 状态路径变化接入日志，优先从现有状态机或控制器快照读取。
4. 新增场景 Inspector 开关控制器，只对统一日志过滤器的通道 key 状态做前端操作。
5. 保留相机现有局部日志，不在本变更中迁移。
6. 添加 EditMode 测试、静态边界检查和 Play Mode 手动验证步骤。

## Open Questions
- Inspector 控制器是否默认随示例场景提交，还是只提供组件由用户手动挂载。
