# Change: 收敛角色配置到根 SO

## Why
`PlayerLocomotionController` 上目前平铺了 4 个平行配置 SO：

```
PlayerLocomotionController
  ├─ stateMachineDefinition    → CharacterStateMachineDefinitionSO
  ├─ runAnimationConfig        → RunLocomotionAnimationConfigSO
  ├─ turnInPlaceAnimationConfig → TurnInPlaceAnimationConfigSO
  ├─ config                    → BasicMovementConfigSO
  └─ ...
```

这导致：
- 没有单一配置入口，新模块只能继续在 Controller 上新增序列化字段
- 配置分散在不同目录，新增角色时需要手动挂接多个 SO
- `TurnInPlaceAnimationConfigSO` 暴露了这种分裂：一个非核心能力的配置独立存在于 Controller 的顶层

参照 BBB 的 `PlayerSO` 模式，本变更新增 `CharacterConfigSO` 作为根配置，将所有子模块引用收敛到根内。

## What Changes
- 新增 `CharacterConfigSO` ScriptableObject，包含以下子模块引用：
  - `stateMachine` → `CharacterStateMachineDefinitionSO`
  - `movement` → `BasicMovementConfigSO`
  - `locomotionAnimation` → `RunLocomotionAnimationConfigSO`
  - `turnInPlace` → `TurnInPlaceAnimationConfigSO`
  - (预留 `action`、`dodge` 等后续模块扩展位)
- `PlayerLocomotionController` 将 4 个独立序列化字段合并为 1 个 `CharacterConfigSO` 引用
- Controller 内部解析子配置的代码改为从根 SO 解引用
- 现有的子 SO 仍然作为独立资产存在，维持各自的编辑器可编辑性
- 创建默认 `CharacterConfig` 资产文件，引用已有子 SO 资产

## Not Changing
- 不改逻辑状态机拓扑、transition 条件、evaluator 或 runner
- 不合并动画配置入状态机定义
- 不改 BBBCharacterController 或其它上层入口
- 不改运行时 Tick 顺序、运动执行或动画播放逻辑
- 不删除现有子 SO 或目录结构

## Impact
- Affected specs: `character-config-root`
- Related specs: `unified-character-state-machine`, `turn-in-place-locomotion`, `wasd-locomotion-pipeline`
- Affected code: `Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
- Affected assets: `Assets/Configs/3C/**`, new `CharacterConfig.asset`

## Resolved Choices
- 第一版不将 `Action/Dodge` 配置纳入 `CharacterConfigSO`；等根 SO 落地后再按需扩展
- 子 SO 保留独立 CreateAssetMenu，不强制只能通过根 SO 创建
