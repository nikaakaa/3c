> 已被 `refactor-unified-character-state-machine` 接管：本变更不再作为后续实现基线，FullBody 配置边界以统一状态机资产为准。

## 1. 现状复核
- [x] 1.1 读取 `centralize-fullbody-hfsm-tree-data` 的 proposal、design、tasks 和 spec delta。
- [x] 1.2 读取 `add-dodge-action-profile` 中 8A 相关任务和 spec delta。
- [x] 1.3 确认当前 `FullBodyHfsmTreeDefinitionSO` 不引用动画 Profile、打断策略或 Dodge motion config。
- [x] 1.4 确认 `FullBodyActionSetSO` 的动画 Profile 引用已拆出。
- [x] 1.5 确认当前 `PlayerFullBodyActionController` 只引用一个 FullBody 主调度入口组件。
- [x] 1.6 确认当前 `Assets/Configs/3C/Statemachine/FullBody` 下不再混有动画配置资产。

## 2. 代码边界设计
- [x] 2.1 定义动作逻辑配置入口的职责：只绑定 action id、动作运动配置和打断策略。
- [x] 2.2 定义动作动画绑定入口的职责：只绑定 action id 和 `ActionAnimationProfileSO`。
- [x] 2.3 定义 FullBody 主调度入口需要显式引用状态树、动作逻辑集和动作动画绑定集。
- [x] 2.4 确认动作 module 仍只消费 `FullBodyActionDefinition` 或等价逻辑定义。
- [x] 2.5 确认动画 Presenter 仍只消费动作动画命令和 Profile。
- [x] 2.6 若设计需要动画配置反向驱动状态机，停止实现并更新 OpenSpec。

## 3. 动作逻辑配置实现
- [x] 3.1 从 `FullBodyActionDefinition` 移除或弃用动画 Profile 字段。
- [x] 3.2 更新 `FullBodyActionDefinition.CreateDodge`，只接收 Dodge motion config 和 interrupt policy set。
- [x] 3.3 更新 `FullBodyActionSetSO.Validate`，只校验 action id、重复 id、Dodge motion config 和 interrupt policy set。
- [x] 3.4 保留 `DodgeActionConfigSO` 与 `ActionInterruptPolicySetSO` 独立复用能力。
- [x] 3.5 确认 `FullBodyActionSetSO` 不引用 `ActionAnimationProfileSO`。

## 4. 动作动画绑定实现
- [x] 4.1 新增或调整动作动画绑定数据，保存 action id 和 `ActionAnimationProfileSO`。
- [x] 4.2 新增或调整动作动画绑定集资产类型。
- [x] 4.3 绑定集支持按 `ActionStateId` 解析动画 Profile。
- [x] 4.4 绑定集校验空 action id。
- [x] 4.5 绑定集校验重复 action id。
- [x] 4.6 绑定集校验缺失动画 Profile。
- [x] 4.7 绑定集校验 `Action.Dodge.Directional` key。
- [x] 4.8 绑定集校验 `Action.Dodge.Backstep` key。
- [x] 4.9 绑定集不引用 Locomotion 状态图或 TransitionLibrary。

## 5. FullBody 主调度接入
- [x] 5.1 为 `PlayerFullBodyActionController` 增加动作动画绑定集引用。
- [x] 5.2 Dodge 尝试进入前，从绑定集解析当前 action 的动画 Profile。
- [x] 5.3 成功解析后把 Profile 传给 `IActionAnimationProfileReceiver`。
- [x] 5.4 缺失绑定集时不创建隐式默认资产。
- [x] 5.5 缺失动画 Profile 时保持动作逻辑路径不产生第二动画入口。
- [x] 5.6 确认运行时没有新增第二个 Dodge controller 或第二个 FullBody coordinator。

## 6. 资产目录重编排
- [x] 6.1 创建 `Assets/Configs/3C/Action/FullBody`。
- [x] 6.2 创建 `Assets/Configs/3C/Action/FullBody/Dodge`。
- [x] 6.3 创建 `Assets/Configs/3C/Animation/FullBody/Corin`。
- [x] 6.4 创建 `Assets/Configs/3C/Animation/Locomotion/Corin`。
- [x] 6.5 将 `CorinFullBodyActionSet.asset` 移到 `Action/FullBody` 并保留 GUID。
- [x] 6.6 将 `DefaultDodgeActionConfig.asset` 移到 `Action/FullBody/Dodge` 并保留 GUID。
- [x] 6.7 将 `DefaultDodgeInterruptPolicySet.asset` 移到 `Action/FullBody/Dodge` 并保留 GUID。
- [x] 6.8 将 `CorinDodgeActionAnimationProfile.asset` 移到 `Animation/FullBody/Corin` 并保留 GUID。
- [x] 6.9 新建 `CorinFullBodyActionAnimationSet.asset` 并绑定 `Action.Dodge -> CorinDodgeActionAnimationProfile`。
- [x] 6.10 将 `DefaultRunLocomotionAnimationConfig.asset` 移到 `Animation/Locomotion/Corin` 并保留 GUID。
- [x] 6.11 将 Locomotion `Bake/*MotionProfile.asset` 移到 `Animation/Locomotion/Corin/Bake` 并保留 GUID。
- [x] 6.12 保留 `DefaultFullBodyHfsmTreeDefinition.asset` 在 `Statemachine/FullBody`。
- [x] 6.13 保留 `DefaultLocomotionStateGraph.asset` 在 `Statemachine/FullBody/Locomotion`。
- [x] 6.14 移动后确认 `Statemachine/FullBody` 下不再有动画 Profile 或 Locomotion animation config。

## 7. Prefab 和资产引用
- [x] 7.1 更新可琳 prefab 的 `actionSet` 引用仍指向同一 GUID。
- [x] 7.2 更新可琳 prefab 的 `actionAnimationSet` 引用指向新动画绑定集。
- [x] 7.3 保留 `hfsmTreeDefinition` 引用。
- [x] 7.4 保留 Action animation presenter 的 Profile 可由运行时绑定覆盖。
- [x] 7.5 检查移动资产后没有丢失 `.meta`。

## 8. 自动测试
- [x] 8.1 测试 `FullBodyActionSetSO` 能解析 Dodge motion 和 interrupt policy。
- [x] 8.2 测试 `FullBodyActionSetSO` 缺失 motion 或 interrupt policy 时报错。
- [x] 8.3 静态测试 `FullBodyActionSetSO` 不引用 `ActionAnimationProfileSO`。
- [x] 8.4 测试动作动画绑定集能解析 `Action.Dodge` 的 Profile。
- [x] 8.5 测试动作动画绑定集缺失 Profile 时报错。
- [x] 8.6 测试动作动画绑定集缺失 Directional key 时报错。
- [x] 8.7 测试动作动画绑定集缺失 Backstep key 时报错。
- [x] 8.8 测试 FullBody coordinator 进入 Dodge 时把绑定集 Profile 传给 Presenter。
- [x] 8.9 测试可琳 prefab 绑定 ActionSet、ActionAnimationSet 和 HFSM tree。
- [x] 8.10 测试配置资产目录归属：状态机目录不含动画资产，动画目录不含状态图资产。
- [x] 8.11 回归 `FullBodyActionFrameworkTests`。
- [x] 8.12 回归 `DodgeActionProfileTests`。
- [x] 8.13 回归 `FullBodyHfsmTreeDataTests`。

## 9. 静态边界检查
- [x] 9.1 检查 FullBody HFSM tree data 源码不引用 Animancer。
- [x] 9.2 检查 FullBody HFSM tree data 源码不引用 `AnimationClip`。
- [x] 9.3 检查 FullBody HFSM tree data 源码不引用 `CharacterController.Move`。
- [x] 9.4 检查 Action module 不直接调用 Animancer 播放 API。
- [x] 9.5 检查 Action animation binding 不引用 Locomotion 状态图。
- [x] 9.6 检查 Locomotion 状态图不引用 Dodge action module。

## 10. 手动验证
- [ ] 10.1 用户在 Unity Editor 中选择默认 FullBody HFSM 树资产，确认只显示状态树拓扑和绑定。
- [ ] 10.2 用户确认 `CorinDodgeActionAnimationProfile` 位于动画配置目录。
- [ ] 10.3 用户确认 `DefaultRunLocomotionAnimationConfig` 位于 Locomotion 动画配置目录。
- [ ] 10.4 Play Mode 中普通 WASD 仍能 Idle、MoveStart、MoveLoop、MoveStop。
- [ ] 10.5 Play Mode 中按方向再按 Shift 仍进入 Directional。
- [ ] 10.6 Directional active 时基础移动不叠加平面位移或 base layer 动画。
- [ ] 10.7 Directional 结束后继续按方向仍进入 Run 档位。
- [ ] 10.8 无方向按 Shift 仍进入 Backstep。
- [ ] 10.9 Backstep 结束后普通移动不强制 Run。
- [ ] 10.10 替换 `CorinDodgeActionAnimationProfile` 的任一 clip，确认无需修改动作逻辑代码。

## 11. 验证记录
- [x] 11.1 运行 `openspec validate refactor-fullbody-config-boundaries --strict --no-interactive`。
- [x] 11.2 记录 EditMode 定向测试结果。
- [x] 11.3 记录静态边界检查结果。
- [ ] 11.4 记录用户 Play Mode 手动验证结果。

验证记录：
- `openspec validate refactor-fullbody-config-boundaries --strict --no-interactive`：passed。
- `dotnet build .\Assembly-CSharp.csproj --no-restore -v:minimal /clp:ErrorsOnly`：passed，0 warnings，0 errors。
- `dotnet build .\Assembly-CSharp-Editor.csproj --no-restore -v:minimal /clp:ErrorsOnly`：passed，6 warnings，0 errors。
- Unity EditMode 旧静态边界测试 `ActionRuntimeStateTrackerDoesNotReferenceForbiddenRuntimeTypes`、`ActionPolicyDataDoesNotReferenceForbiddenRuntimeTypes`、`ActionModuleDoesNotReferenceForbiddenRuntimeTypes`：3/3 passed。
- Unity EditMode 定向测试 `ThirdPersonAction.Tests.FullBodyActionFrameworkTests`、`ThirdPersonAction.Tests.DodgeActionProfileTests`、`ThirdPersonAction.Tests.FullBodyHfsmTreeDataTests`：93/93 passed。
- 静态边界检查：`FullBodyActionSetSO` 不引用 `ActionAnimationProfileSO`；FullBody HFSM tree data 源码不引用 Animancer、`AnimationClip` 或 `CharacterController.Move`；`FullBodyActionAnimationSetSO` 不引用 Locomotion 状态图或 TransitionLibrary。
- Path 文档检查：`D:\Unity_Project_1\DG_Entity\docs\Path` 中无 FullBody/Dodge/ActionAnimation/Locomotion/3C 相关 Path 文档，本次为 no-op；一致性检查存在既有反链错误，和本次变更无直接关系。
- Play Mode 手动验证：待用户按 10.1-10.10 执行并回填 11.4。
