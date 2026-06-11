## 1. 变更接管和冲突处理
- [x] 1.1 确认 `refactor-unified-character-state-machine` 已获批准后再开始实现。
- [x] 1.2 标记 `add-fullbody-action-framework` 不再作为继续实现基线。
- [x] 1.3 标记 `add-fullbody-hfsm-state-tree` 不再作为继续实现基线。
- [x] 1.4 标记 `centralize-fullbody-hfsm-tree-data` 不再作为继续实现基线。
- [x] 1.5 标记 `refactor-fullbody-config-boundaries` 不再作为继续实现基线。
- [x] 1.6 标记 `add-dodge-action-profile` 中依赖分裂路径的运行时和配置部分需要被统一状态机替换。
- [x] 1.7 不删除任何日志调用；如需删除日志，单独等待用户确认。

## 2. 现状删除清单
- [x] 2.1 列出 `BasicLocomotionStateMachine` 的调用点和测试。
- [x] 2.2 列出 `LocomotionStateGraphConfigSO` 的资产引用和 prefab 引用。
- [x] 2.3 列出 `FullBodyHfsmStateTreeBuilder` / `FullBodyHfsmStateTreeDriver` 的调用点。
- [x] 2.4 列出 `DodgeActionRuntime` 的调用点和测试。
- [x] 2.5 列出 `DodgeFullBodyActionModule` 的调用点和测试。
- [x] 2.6 列出 `FullBodyActionSetSO` 和 `FullBodyActionAnimationSetSO` 的资产引用。
- [x] 2.7 列出 `ActionAnimationProfileSO` 作为独立动作动画入口的引用。
- [x] 2.8 标注哪些类型可以直接删除，哪些需要临时迁移 adapter。

## 3. 统一状态机数据模型
- [x] 3.1 定义统一状态 ID 类型。
- [x] 3.2 定义层级路径规则。
- [x] 3.3 定义状态节点数据。
- [x] 3.4 定义状态标签数据，例如 FullBody、Locomotion、Action、Dodge、Movement。
- [x] 3.5 定义状态变体数据，例如 Directional、Backstep。
- [x] 3.6 定义 transition 数据。
- [x] 3.7 定义 transition 优先级规则。
- [x] 3.8 定义 transition 条件列表。
- [x] 3.9 定义状态输出块。
- [x] 3.10 定义状态机快照数据。
- [x] 3.11 保证数据模型不引用 Animancer、CharacterController、InputAction、Cinemachine 或场景对象。

## 4. Transition 条件模型
- [x] 4.1 定义 `HasMoveIntent` 条件。
- [x] 4.2 定义 `NoMoveIntent` 条件。
- [x] 4.3 定义 `StateCanExit` 或等价动画可退出事实条件。
- [x] 4.4 定义 `HasInputRequest(Dodge)` 条件。
- [x] 4.5 定义 `StateElapsedAtLeast` 条件。
- [x] 4.6 定义 `RequestPriorityAtLeast` 或等价优先级条件。
- [x] 4.7 定义 `CurrentStateHasTag` 条件。
- [x] 4.8 定义条件 evaluator 上下文。
- [x] 4.9 测试每个条件只读取纯数据上下文。
- [x] 4.10 静态测试条件 evaluator 不引用 Animancer 或 Unity 输入系统。

## 5. 状态输出模型
- [x] 5.1 定义输入驱动基础移动输出。
- [x] 5.2 定义配置距离/时长的动作位移输出。
- [x] 5.3 定义立即转向输出。
- [x] 5.4 定义请求消费输出。
- [x] 5.5 定义 Run latch 写入输出。
- [x] 5.6 定义 Action/State fact 写入输出。
- [x] 5.7 定义动画转换请求输出。
- [x] 5.8 测试输出模型不直接调用运动、动画或输入缓冲对象。

## 6. 动画转换接入
- [x] 6.1 定义状态动画绑定数据。
- [x] 6.2 支持状态直接绑定 Animancer `TransitionAssetBase` 或等价 transition 引用。
- [x] 6.3 支持状态变体绑定不同 transition，例如 Dodge Directional 和 Backstep。
- [x] 6.4 支持 fade、speed、start time 或等价表现参数。
- [x] 6.5 保证动画绑定只在逻辑状态决定后被消费。
- [x] 6.6 删除或退役独立游离的 `ActionAnimationProfileSO` Dodge 入口。
- [x] 6.7 保留 Animancer 播放 adapter，不让状态机读取 Animancer runtime state。
- [x] 6.8 测试缺失动画绑定时配置校验失败。

## 7. 默认统一状态树
- [x] 7.1 创建默认统一状态机资产。
- [x] 7.2 添加 `FullBody` 根节点。
- [x] 7.3 添加 `FullBody/Locomotion` 子树。
- [x] 7.4 添加 `Idle` 状态。
- [x] 7.5 添加 `MoveStart` 状态。
- [x] 7.6 添加 `MoveLoop` 状态。
- [x] 7.7 添加 `MoveStop` 状态。
- [x] 7.8 添加 `FullBody/Action` 子树。
- [x] 7.9 添加 `Dodge` 状态。
- [x] 7.10 为 `Dodge` 添加 `Directional` 变体。
- [x] 7.11 为 `Dodge` 添加 `Backstep` 变体。

## 8. 默认 transition 迁移
- [x] 8.1 配置 `Idle -> MoveStart`。
- [x] 8.2 配置 `MoveStart -> MoveLoop`。
- [x] 8.3 配置 `MoveStart -> MoveStop`。
- [x] 8.4 配置 `MoveLoop -> MoveStop`。
- [x] 8.5 配置 `MoveStop -> MoveStart`。
- [x] 8.6 配置 `MoveStop -> Idle`。
- [x] 8.7 配置 `Locomotion/* -> Dodge` 的 Dodge 请求 transition。
- [x] 8.8 配置 `Dodge -> MoveLoop` 或等价继续移动 transition。
- [x] 8.9 配置 `Dodge -> Idle` 或等价无输入回落 transition。
- [x] 8.10 确认这些 transition 全部在同一张状态机配置中可见。

## 9. 运行时组装
- [x] 9.1 新建统一状态机 runner 或等价运行时解释器。
- [x] 9.2 runner 每帧读取统一 context。
- [x] 9.3 runner 推进 transition。
- [x] 9.4 runner 产出统一状态快照。
- [x] 9.5 runner 产出运动输出包。
- [x] 9.6 runner 产出动画输出包。
- [x] 9.7 runner 产出输入请求消费输出。
- [x] 9.8 runner 产出 Run latch 输出。
- [x] 9.9 runner 不直接调用 `CharacterController.Move`。
- [x] 9.10 runner 不直接调用 Animancer 播放 API。

## 10. 旧路径删除
- [x] 10.1 删除或退役 `BasicLocomotionStateMachine`。
- [x] 10.2 删除或退役 `LocomotionStateGraphConfigSO`。
- [x] 10.3 删除或退役 `FullBodyHfsmStateTreeBuilder`。
- [x] 10.4 删除或退役 `FullBodyHfsmStateTreeDriver`。
- [x] 10.5 删除或退役 `DodgeActionRuntime`。
- [x] 10.6 删除或退役 `DodgeFullBodyActionModule`.
- [x] 10.7 删除或退役 `FullBodyActionSetSO`。
- [x] 10.8 删除或退役 `FullBodyActionAnimationSetSO`。
- [x] 10.9 删除旧分裂路径对应测试。
- [x] 10.10 删除旧分裂路径对应资产或迁移为统一状态机资产。

## 11. Prefab 和配置迁移
- [x] 11.1 迁移可琳 prefab 到统一状态机入口。
- [x] 11.2 移除 prefab 上旧 FullBody Action 入口引用。
- [x] 11.3 移除 prefab 上旧 Locomotion 状态图引用。
- [x] 11.4 绑定统一状态机资产。
- [x] 11.5 绑定运动执行 adapter。
- [x] 11.6 绑定 Animancer 播放 adapter。
- [x] 11.7 绑定输入缓冲 adapter。
- [x] 11.8 保持相机 Look 输入继续响应。

## 12. 自动测试
- [x] 12.1 测试默认状态为 `FullBody/Locomotion/Idle`。
- [x] 12.2 测试移动输入进入 `MoveStart`。
- [x] 12.3 测试 `MoveStart` 可退出后进入 `MoveLoop`。
- [x] 12.4 测试无移动输入进入 `MoveStop`。
- [x] 12.5 测试 `MoveStop` 可退出后进入 `Idle`。
- [x] 12.6 测试 `MoveStop` 中重新移动进入 `MoveStart`。
- [x] 12.7 测试有移动输入和 Dodge 请求进入 `Dodge/Directional`。
- [x] 12.8 测试无移动输入和 Dodge 请求进入 `Dodge/Backstep`。
- [x] 12.9 测试 Directional 输出 4m/0.35s 或配置等价位移。
- [x] 12.10 测试 Backstep 输出 3m/0.35s 或配置等价位移。
- [x] 12.11 测试 Directional 完成后写 Run latch。
- [x] 12.12 测试 Backstep 完成后不写 Run latch。
- [x] 12.13 测试 Idle 后 Run latch 重置。
- [x] 12.14 测试 Dodge active 时 Locomotion 不再有第二运动输出。
- [x] 12.15 测试 Dodge active 时 Locomotion 不再有第二 base layer 动画输出。
- [x] 12.16 测试动画 transition 从状态输出解析。
- [x] 12.17 静态测试统一状态机 runner 不引用 Animancer runtime。
- [x] 12.18 静态测试统一状态机 runner 不引用 `CharacterController`。
- [x] 12.19 静态测试项目运行时代码不再引用旧分裂路径类型。

## 13. 手动验证
- [ ] 13.1 在 Unity Editor 打开可琳角色，确认只有统一状态机入口负责 FullBody base layer。
- [ ] 13.2 在统一状态机资产中看到 Locomotion 四阶段和 Dodge 状态。
- [ ] 13.3 在同一状态机资产中看到 Dodge 进入和退出 transition。
- [ ] 13.4 在 Dodge 状态或变体配置中看到 Directional/Backstep 的 Animancer transition。
- [ ] 13.5 Play Mode 中普通 WASD 能走 Idle/MoveStart/MoveLoop/MoveStop。
- [ ] 13.6 Play Mode 中有方向按 Shift 进入 Dodge Directional 并向输入方向冲刺。
- [ ] 13.7 Directional 结束后继续按方向键进入 Run。
- [ ] 13.8 Play Mode 中无方向按 Shift 进入 Dodge Backstep 且不强制 Run。
- [ ] 13.9 Dodge active 时基础移动不叠加额外位移或 base layer 动画。
- [ ] 13.10 动作期间相机 Look 继续响应。
- [ ] 13.11 松开移动回 Idle 后再次普通移动默认 Walk。
- [ ] 13.12 替换 Dodge 动画 transition 后无需修改逻辑代码。

## 14. 文档和 OpenSpec
- [x] 14.1 更新 `docs/agents/character-animation-state-roadmap.md`，记录统一状态机口径。
- [x] 14.2 更新最终 specs，移除与统一状态机冲突的 Locomotion 特化要求。
- [x] 14.3 更新最终 specs，移除与统一状态机冲突的 Action/Dodge 特化要求。
- [x] 14.4 运行 `openspec validate refactor-unified-character-state-machine --strict --no-interactive`。
- [x] 14.5 运行相关 EditMode 测试并记录命令和结果。
- [ ] 14.6 记录用户 Play Mode 手动验证结果。

## 15. 验证记录
- [x] 15.1 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`：通过，0 个错误。
- [x] 15.2 `openspec validate refactor-unified-character-state-machine --strict --no-interactive`：通过。
- [x] 15.3 `openspec validate --all --strict --no-interactive`：通过，25 项通过。
- [x] 15.4 Unity MCP EditMode `Tests.Editor.UnifiedCharacterStateMachineTests`：15/15 通过。
- [x] 15.5 Unity Console 编译错误：0 条；测试后保留一条 Test Runner 保存 `TestResults.xml` 的路径日志，未清理。
- [x] 15.6 抽象清理后复跑 `dotnet build .\Assembly-CSharp.csproj --no-restore /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`：通过，5 个 warning，0 个错误。
- [x] 15.7 抽象清理后复跑 `dotnet build .\Assembly-CSharp-Editor.csproj /m:1 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly`：通过，6 个 warning，0 个错误；首次 `--no-restore` 因缺少 `Temp/obj/Assembly-CSharp-Editor/project.assets.json` 未执行编译。
- [x] 15.8 抽象清理后复跑 Unity MCP EditMode `Tests.Editor.UnifiedCharacterStateMachineTests`：18/18 通过。
- [x] 15.9 抽象清理后复跑 `openspec validate refactor-unified-character-state-machine --strict --no-interactive`：通过。
- [x] 15.10 抽象清理后复跑 `openspec validate --all --strict --no-interactive`：通过，25 项通过。

## 16. 抽象清理
- [x] 16.1 将通用状态机输出中的 Dodge 专用位移字段替换为通用动作位移定义。
- [x] 16.2 移除状态机 Runner 对 `DodgeActionConfig` 的直接依赖。
- [x] 16.3 保留状态机 Runner 只解释状态输出，不直接知道 Dodge 距离/时长结构。
- [x] 16.4 将 FullBody 控制器中的 Dodge 输入请求构建拆到独立模块。
- [x] 16.5 更新默认统一状态机配置资产，确认 Directional 和 Backstep 位移配置仍在同一状态机里。
- [x] 16.6 补充或更新 EditMode 测试，覆盖通用动作位移解析和旧 Dodge 专用模型清理。
- [x] 16.7 运行运行时编译、OpenSpec 严格校验和 Unity EditMode 测试。
