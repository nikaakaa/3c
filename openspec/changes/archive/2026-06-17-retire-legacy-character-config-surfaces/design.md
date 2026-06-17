# 设计说明

## 目标主线

正式角色运行链路应保持单线：

1. `CharacterConfigSO` 是角色配置根。
2. Locomotion 子配置提供移动参数、Run latch 配置、Locomotion 状态图和动画 profile。
3. FullBody Action 子配置提供动作定义、请求策略与动作动画 profile。
4. `CharacterFrameRuntimeController` 统一驱动 Locomotion 与 FullBody Action。
5. `CharacterAnimancerPresenter` 统一消费状态视图并播放 Locomotion/Action 动画。

任何旧字段、旧目录、旧 adapter 或旧 presenter 都不能成为第二条正式路径。

## 废弃面分类

### 必须从正式路径删除

- `PlayerLocomotionController` 上的旧平铺序列化字段，例如 `runAnimationConfig`、旧 `config`、旧 `stateMachineDefinition`。
- `FullBodyActionRuntime` 上的旧平铺序列化字段，例如 `stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig`。
- 会解析旧字段或从旧字段 fallback 的公开属性/方法。
- `Assets/Configs/3C/Action/FullBody`、`Assets/Configs/3C/StateMachine/FullBody`、`Assets/Configs/3C/Animation/FullBody` 作为正式配置目录。
- 正式 prefab/scene 上的 `FullBodyActionTickAdapter`、`LocomotionTickAdapter`、旧 locomotion/action Animancer presenter。

### 可只读保留

- `FullBodyStateView` 可作为诊断和动画观察视图保留，但不能写入状态、推进动作生命周期或执行请求仲裁。
- 历史 GUID 迁移信息可通过 meta 或测试说明保留，但旧路径本身不能作为正式加载入口。

### 仅测试/迁移可见

- 退役 adapter/presenter 若仍需要用于冲突诊断，必须位于 Editor/test-only 或硬禁用路径，并由测试证明它不会注册进正式 runtime。
- 旧字段名可出现在测试断言或迁移扫描清单中，但不能出现在正式运行时的配置解析逻辑里。

## 实现策略

先加静态约束与资产扫描测试，再删除或隔离旧入口。这样可以先锁定“不会回流”的边界，再逐步清理代码和资产。

清理过程不新增 fallback。发现旧字段仍有非空引用时，不通过兼容读取绕过，而是修复对应 prefab/scene/config，使其回到正式 `CharacterConfigSO` 依赖链。

## 规格策略

本变更使用新增要求约束当前正式主线，而不在本提案里直接重写所有历史 requirement。原因是 `refactor-locomotion-action-state-graphs` 已完成但尚未归档，基础规格仍含旧文本。实现和归档时应先让已完成状态图变更成为基线，再把本变更的清理要求并入基础规格。

## 风险与缓解

- Unity 序列化字段删除可能留下 YAML 残键。缓解方式是先增加扫描测试，再通过 Unity 序列化保存或文本级资产清理移除残键。
- 删除退役类可能影响已有冲突诊断测试。缓解方式是把诊断逻辑迁移到正式 runtime 或 Editor/test-only 扫描，而不是保留可挂载的运行时组件。
- 旧规格文本可能继续误导后续提案。缓解方式是本变更明确 active specs 中旧 FullBody 主树、旧目录、旧 Host Adapter 不再是正式路径，并在归档顺序上依赖已完成状态图变更。
