## 1. 文档收口
- [x] 1.1 更新 `AGENT.md` 的状态机库选择：角色主线改为项目自研统一分层状态机。
- [x] 1.2 更新 `docs/agents/unityhfsm-usage-guide.md`，标记为历史参考或非主线参考。
- [x] 1.3 新增或更新自研分层状态机运行时指南。
- [x] 1.4 更新 `openspec/project.md` 中 BBB 旧架构描述。
- [x] 1.5 搜索文档中的 UnityHFSM 优先口径。
- [x] 1.6 搜索文档中的 `BasicLocomotionStateMachine`、`LocomotionStateGraphConfigSO` 作为正式权威的旧口径。
- [x] 1.7 搜索文档中的 `BBBCharacterController`、`MotionDriver`、`AnimancerFacade`、`PlayerStateRegistry` 旧主线描述。
- [x] 1.8 将文档统一为“一棵统一分层状态机”口径。
- [x] 1.9 明确 UnityHFSM 包存在不等于角色主线使用。

## 2. 当前行为 Characterization
- [x] 2.1 增加状态图 transition 选择测试。
- [x] 2.2 增加 wildcard source transition 选择测试。
- [x] 2.3 增加 transition priority 胜出测试。
- [x] 2.4 增加 state time 推进测试。
- [x] 2.5 增加 variant 捕获测试。
- [x] 2.6 增加 pending transition path 诊断测试。
- [x] 2.7 增加 restore round-trip 测试。
- [x] 2.8 增加 TurnBack 方向 restore 测试。
- [x] 2.9 增加 animation request 一次性输出测试。
- [x] 2.10 增加 input consume 一次性输出测试。

## 3. 通用状态图运行时边界
- [x] 3.1 设计通用 runtime interface 草图。
- [x] 3.2 确认 interface 不暴露 FullBody、Dodge、TurnBack 专有类型。
- [x] 3.3 确认 runtime snapshot 不保存 Unity 对象。
- [x] 3.4 确认 runtime 不引用 UnityHFSM。
- [x] 3.5 确认 runtime 不调用 motion executor。
- [x] 3.6 确认 runtime 不调用 animation presenter。
- [x] 3.7 确认 runtime 不读取 InputAction。
- [x] 3.8 确认 runtime 不读取 CharacterController 或 Transform。
- [x] 3.9 将默认状态机资产迁移到 `Assets/Configs/3C/StateMachine/` 并移除旧 `Statemachine` 并行入口。
- [x] 3.10 保持当前状态 id 序列化含义不变。

## 4. 经典生命周期接口
- [x] 4.1 设计 `Enter` 输入 context。
- [x] 4.2 设计 `Tick` 输入 context。
- [x] 4.3 设计 `Exit` 输入 context。
- [x] 4.4 设计统一 frame builder 或等价输出聚合类型。
- [x] 4.5 设计状态 payload 的 snapshot/restore 边界。
- [x] 4.6 确认生命周期接口不暴露 Unity 对象。
- [x] 4.7 确认生命周期接口不直接执行运动或动画。
- [x] 4.8 覆盖 Enter 一次性输出测试。
- [x] 4.9 覆盖 Tick 持续输出测试。
- [x] 4.10 覆盖 Exit 一次性输出测试。
- [x] 4.11 覆盖 transition 发生帧仍只产出一个 frame。

## 5. Timeline Facts 拆分
- [x] 5.1 为当前 timeline 采样增加 characterization 测试。
- [x] 5.2 抽出 timeline facts sampler。
- [x] 5.3 让 sampler 输入 active state snapshot、runtime animation facts 和 timeline policy。
- [x] 5.4 让 sampler 输出 `StateTimelineWindowFacts` 或等价纯数据。
- [x] 5.5 移除 Action request resolver 对 runner timeline 方法的直接依赖。
- [x] 5.6 验证 TurnBack enter window 行为不变。
- [x] 5.7 验证 Dodge request window 行为不变。
- [x] 5.8 验证 ActionCanExit / LocomotionAnimationCanExit 行为不变。

## 6. State Output 拆分
- [x] 6.1 为当前 `CharacterStateMachineFrame` 输出增加 characterization 测试。
- [x] 6.2 抽出 state output resolver。
- [x] 6.3 将 motion output 解析移入 output resolver。
- [x] 6.4 将 animation request 构建移入 output resolver 或明确子模块。
- [x] 6.5 将 input consume 输出移入 output resolver。
- [x] 6.6 将 run latch 输出移入 output resolver。
- [x] 6.7 将 TurnBack motion policy 输出移入 output resolver。
- [x] 6.8 确认 output resolver 只输出纯数据。
- [x] 6.9 确认 output resolver 不执行运动。
- [x] 6.10 确认 output resolver 不播放动画。

## 7. 动画配置归属
- [x] 7.1 盘点 `CharacterStateAnimationBinding` 当前字段使用点。
- [x] 7.2 设计状态机侧动画语义 key / timeline binding key。
- [x] 7.3 将 Locomotion 具体播放配置归属到 `RunLocomotionAnimationConfigSO` 或等价配置。
- [x] 7.4 将 Action 具体播放配置归属到 `ActionAnimationProfileSO` 或等价配置。
- [x] 7.5 迁移默认状态机资产中的具体动画播放字段。
- [x] 7.6 增加校验：状态机节点不得要求具体 clip/transition/fade。
- [x] 7.7 增加校验：动画 key 缺失时报告明确错误。
- [x] 7.8 覆盖动画 key 由状态输出产生、具体资源由动画配置解析。

## 8. Runner 收窄
- [x] 8.1 收窄 runner 字段到状态推进所需最小集合。
- [x] 8.2 runner 保留 active state。
- [x] 8.3 runner 保留 state time。
- [x] 8.4 runner 保留 variant。
- [x] 8.5 runner 保留 action direction 或将其归属到可恢复 state payload。
- [x] 8.6 runner 保留 pending transition path。
- [x] 8.7 runner restore 覆盖所有状态推进字段。
- [x] 8.8 runner 不再长期持有 output-only latch 标记。
- [x] 8.9 runner 不再长期持有 animation request 输出状态，或将其明确归属一次性输出 tracker。
- [x] 8.10 删除或降级不再需要的 runner public helper。

## 9. Pipeline 接入
- [x] 9.1 FullBody pipeline 在 GameplayDecision 前准备 request gate 所需 facts。
- [x] 9.2 FullBody pipeline 调用状态图 runtime 推进。
- [x] 9.3 FullBody pipeline 调用 output resolver。
- [x] 9.4 Locomotion frame pipeline 只消费外部传入 runtime/result。
- [x] 9.5 Action request gate 只读取纯数据 facts。
- [x] 9.6 Motion executor 只消费 motion command。
- [x] 9.7 Animation presenter 只消费 animation request。
- [x] 9.8 Runtime blackboard 写入顺序保持不变。

## 10. 静态边界测试
- [x] 10.1 测试 state graph runtime 不引用 UnityHFSM。
- [x] 10.2 测试 state graph runtime 不引用 Animancer runtime。
- [x] 10.3 测试 state graph runtime 不引用 CharacterController。
- [x] 10.4 测试 state graph runtime 不引用 InputAction。
- [x] 10.5 测试 state graph runtime 不引用 Transform。
- [x] 10.6 测试状态机模型不引用 `AnimationClip`。
- [x] 10.7 测试状态机模型不引用 `TransitionAssetBase`。
- [x] 10.8 测试生命周期实现不调用 Animancer 或 Animator 播放 API。
- [x] 10.9 测试生命周期实现不调用 `CharacterController.Move`。
- [x] 10.10 测试生命周期实现不读取 `InputAction`。
- [x] 10.11 测试生命周期实现不写 Transform。
- [x] 10.12 测试只有 FullBody 主入口创建正式 runner。
- [x] 10.13 测试没有新增 fallback 配置。
- [x] 10.14 测试没有新增第二 tick driver。

## 11. 行为验证
- [x] 11.1 运行统一状态机相关 EditMode 测试。
- [x] 11.2 运行 FullBody frame pipeline 相关 EditMode 测试。
- [x] 11.3 运行 Locomotion frame pipeline 相关 EditMode 测试。
- [x] 11.4 运行 rollback replay 相关 EditMode 测试。
- [x] 11.5 运行动作动画 Profile 相关 EditMode 测试。
- [x] 11.6 运行基础移动动画配置相关 EditMode 测试。
- [x] 11.7 运行静态边界检查脚本。
- [x] 11.8 运行 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 11.9 运行 `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`。
- [x] 11.10 运行 `openspec validate refactor-character-hierarchical-state-runtime --strict --no-interactive`。

## 12. 手动验证说明
- [x] 12.1 记录 Sandbox WASD 验证步骤。
- [x] 12.2 记录 Run/MoveLoop 验证步骤。
- [x] 12.3 记录 TurnBack 验证步骤。
- [x] 12.4 记录 Dodge Directional 验证步骤。
- [x] 12.5 记录 Dodge Backstep 验证步骤。
- [x] 12.6 记录状态路径诊断验证步骤。
- [x] 12.7 记录 rollback F6/F8 验证步骤。
- [x] 12.8 记录如何验证动画替换不需要修改状态机资产。
