# Change: 增加角色 Action Catalog 配置入口

## Why
当前角色帧管线已经把 Locomotion 与 Action 收到同一 Character frame pipeline 下，但动作配置仍以 `CharacterConfigSO.DodgeAction` 和静态 Dodge resolver 为主。继续做普攻、闪避、技能或编辑器前，需要先把“角色拥有哪些 Action、每个 Action 的正式逻辑定义在哪里”收束为一个可校验的 SO 数据入口。

## What Changes
- 新增 `CharacterActionCatalogSO` 或等价角色动作目录，作为 `CharacterConfigSO` 的正式 Action 子模块。
- 新增 `CharacterActionDefinitionSO` 或等价动作定义，按稳定 `ActionStateId` 管理 Dodge、LightAttack 等动作逻辑配置。
- 先迁移 `Action.Dodge`：Dodge 运动参数、请求类型、输入来源、优先级、抗性、变体和动作动画 key 种子必须能从 Action Catalog 追踪。
- 将 `CharacterConfigSO.DodgeAction` 降级为迁移遗留，不再作为正式 gameplay 解析入口或 fallback。
- 让 Action request resolver 从正式 Action Catalog 读取动作定义，再输出纯 runtime `CharacterResolvedAction` 或等价结果。
- 保持动作动画 Profile 仍由独立动作动画绑定配置解析，Action Catalog 不直接持有 Animancer、AnimationClip 或动作动画 Profile。
- 本变更不实现完整技能编辑器、不实现轻攻击连段、不实现 hitbox、伤害、VFX/SFX、Camera event、IK、Root Motion 权威或网络协议变更。

## Impact
- Affected specs:
  - `character-action-catalog`
  - `character-config-root`
  - `fullbody-action-framework`
  - `fullbody-config-boundaries`
  - `dodge-action`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Config/CharacterConfigSO.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Config/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Solver/CharacterActionRequestResolution.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/Solver/CommittedActionRequestSubmissionProviders.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/CharacterRuntimeCore.cs`
  - `3cDemo/Client/3C_Client/Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
  - `3cDemo/Client/3C_Client/Assets/Configs/3C/Action/Corin/**`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/*`
