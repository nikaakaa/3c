## 1. Scope 确认

- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 确认依赖 `add-action-interrupt-arbiter` 的纯 runtime policy 模型。
- [x] 1.3 确认本变更只实现策略集合数据源。
- [x] 1.4 确认本变更不新增完整动作状态 catalog。
- [x] 1.5 确认本变更不新增动作状态机。
- [x] 1.6 确认本变更不接输入缓冲。
- [x] 1.7 确认本变更不接 Animancer、Animator、AnimationClip 或 TransitionAsset。
- [x] 1.8 确认本变更不修改 Locomotion 四阶段状态图。
- [x] 1.9 确认本变更不迁移 `MoveStop -> MoveStart`。
- [x] 1.10 如果实现需要绕过当前角色主线或新增第二条动作入口，停止并回到 OpenSpec。

## 2. 目录与模块边界

- [x] 2.1 新建或复用 `Assets/Scripts/Character/Action/Config/`。
- [x] 2.2 确认 `Action/Config` 只放 Unity 配置包装。
- [x] 2.3 确认 `Action/Model` 继续放纯数据模型。
- [x] 2.4 确认 `Action/Solver` 继续放编译和校验逻辑。
- [x] 2.5 确认 `Action/Config` 不引用 Animancer。
- [x] 2.6 确认 `Action/Config` 不引用 AnimationClip。
- [x] 2.7 确认 `Action/Config` 不引用现有角色控制器。
- [x] 2.8 确认测试放在 `Assets/Tests/Editor/ActionInterruptPolicyDataTests.cs` 或等价编辑器测试文件。

## 3. 策略定义模型

- [x] 3.1 新增 `ActionInterruptPolicyDefinition`。
- [x] 3.2 definition 包含 from state id 字符串。
- [x] 3.3 definition 包含 target state id 字符串。
- [x] 3.4 definition 包含 min priority。
- [x] 3.5 definition 包含 timing rule。
- [x] 3.6 definition 包含 window start。
- [x] 3.7 definition 包含 window end。
- [x] 3.8 definition 包含 force。
- [x] 3.9 definition 可选包含 debug name 或 note。
- [x] 3.10 definition 不包含 `AnimationClip`。
- [x] 3.11 definition 不包含 `UnityEngine.Object` 场景引用。
- [x] 3.12 definition 不包含 Animancer 类型。

## 4. 策略集合模型

- [x] 4.1 新增 `ActionInterruptPolicySet` 或等价纯数据集合。
- [x] 4.2 policy set 暴露只读策略定义列表。
- [x] 4.3 policy set 支持空集合。
- [x] 4.4 policy set 保持策略顺序稳定。
- [x] 4.5 policy set 不持有 ScriptableObject。
- [x] 4.6 policy set 不持有 MonoBehaviour。
- [x] 4.7 policy set 不持有 Animancer 类型。

## 5. ScriptableObject 配置入口

- [x] 5.1 新增 `ActionInterruptPolicySetSO` 或等价配置资产。
- [x] 5.2 配置资产可通过 Unity CreateAssetMenu 创建。
- [x] 5.3 配置资产序列化多条 policy definition。
- [x] 5.4 配置资产提供只读读取方法。
- [x] 5.5 配置资产提供 `ToPolicySet` 或等价转换方法。
- [x] 5.6 配置资产提供 `Validate` 或等价校验入口。
- [x] 5.7 配置资产不引用 AnimationClip。
- [x] 5.8 配置资产不引用 Animancer。
- [x] 5.9 配置资产不引用角色 prefab 或场景实例。

## 6. 编译与转换

- [x] 6.1 新增 `ActionInterruptPolicySetCompiler` 或等价转换器。
- [x] 6.2 compiler 将 from state id 转成 `ActionStateId`。
- [x] 6.3 compiler 将 target state id 转成 `ActionStateId`。
- [x] 6.4 compiler 复制 min priority。
- [x] 6.5 compiler 复制 timing rule。
- [x] 6.6 compiler 复制 window start。
- [x] 6.7 compiler 复制 window end。
- [x] 6.8 compiler 复制 force。
- [x] 6.9 compiler 输出 `IReadOnlyList<ActionInterruptPolicy>` 或等价 runtime policy 列表。
- [x] 6.10 compiler 对 null set 安全处理。
- [x] 6.11 compiler 不调用 `ActionInterruptArbiter`。
- [x] 6.12 compiler 不切换状态。
- [x] 6.13 compiler 不播放动画。

## 7. 校验

- [x] 7.1 扩展或复用 `ActionInterruptPolicyValidator`。
- [x] 7.2 校验空 policy set 合法。
- [x] 7.3 校验 from state id 为空时报错。
- [x] 7.4 校验 target state id 为空时报错。
- [x] 7.5 校验 min priority 小于 0 时报错。
- [x] 7.6 校验 `AfterElapsedTime` window start 小于 0 时报错。
- [x] 7.7 校验 `DuringElapsedTimeWindow` window start 小于 0 时报错。
- [x] 7.8 校验 `DuringElapsedTimeWindow` window end 小于 window start 时报错。
- [x] 7.9 校验非法 timing rule 报错。
- [x] 7.10 校验重复 from/target/timing 规则报告 warning。
- [x] 7.11 校验结果可被测试断言。
- [x] 7.12 校验器不依赖 Unity Editor API。

## 8. 自动测试

- [x] 8.1 新增 `ActionInterruptPolicyDataTests`。
- [x] 8.2 测试空策略集合合法。
- [x] 8.3 测试单条 definition 能编译成 runtime policy。
- [x] 8.4 测试多条 definition 编译后顺序稳定。
- [x] 8.5 测试非法 from state id 报错。
- [x] 8.6 测试非法 target state id 报错。
- [x] 8.7 测试负 min priority 报错。
- [x] 8.8 测试 `AfterElapsedTime` 负时间报错。
- [x] 8.9 测试 `DuringElapsedTimeWindow` 非法窗口报错。
- [x] 8.10 测试重复规则报告 warning。
- [x] 8.11 测试 SO 可转换为 policy set。
- [x] 8.12 测试编译后的策略能被 `ActionInterruptArbiter` 接受。
- [x] 8.13 测试配置数据不需要 Unity 场景对象。

## 9. 静态边界验证

- [x] 9.1 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animancer`。
- [x] 9.2 静态搜索 `Assets/Scripts/Character/Action` 不引用 `AnimationClip`。
- [x] 9.3 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animator`。
- [x] 9.4 静态搜索 `Assets/Scripts/Character/Action` 不引用 `CharacterController`。
- [x] 9.5 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Cinemachine`。
- [x] 9.6 静态搜索 `Assets/Scripts/Character/Action` 不引用 `UnityEngine.InputSystem`。
- [x] 9.7 静态搜索 `Assets/Scripts/Character/Action` 不引用 `BBBNexus`。
- [x] 9.8 静态搜索确认 `PlayerLocomotionController` 不依赖 policy set。
- [x] 9.9 静态搜索确认 `BasicLocomotionStateMachine` 不依赖 policy set。
- [x] 9.10 静态搜索确认 `BasicLocomotionAnimancerPresenter` 不依赖 policy set。

## 10. Unity 验证

- [x] 10.1 请求 Unity 刷新脚本。
- [x] 10.2 检查 Unity Console 没有 C# 编译错误。
- [x] 10.3 运行 Unity EditMode 定向测试 `ActionInterruptPolicyDataTests`。
- [x] 10.4 运行 `ActionInterruptArbiterTests`，确认已有仲裁行为不回退。
- [x] 10.5 如果 Unity MCP 或测试不可用，记录原因和手动验证步骤，不伪造结果。

## 11. OpenSpec 验证

- [x] 11.1 运行 `openspec validate add-action-interrupt-policy-data --strict --no-interactive`。
- [x] 11.2 如果实现过程中调整范围，同步更新 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 11.3 完成实现后只把真实完成项标记为 `- [x]`。

## 12. 手动验证

- [ ] 12.1 在 Unity Project 面板中创建 `ActionInterruptPolicySetSO` 资产。
- [ ] 12.2 在 Inspector 中添加一条 `Action.Attack01 -> Action.Dodge` 策略。
- [ ] 12.3 确认该资产不要求拖入动画 clip、角色 prefab 或场景对象。
- [ ] 12.4 在 Unity Test Runner 中确认 `ActionInterruptPolicyDataTests` 全部通过。
- [ ] 12.5 打开当前演示场景，确认 WASD、Look、Idle、MoveStart、MoveLoop、MoveStop 行为没有因为新增配置数据变化。
- [ ] 12.6 确认没有新增需要手动挂到角色 prefab 的运行时仲裁组件。
- [ ] 12.7 确认没有新增第二套角色控制器或第二条基础移动入口。
