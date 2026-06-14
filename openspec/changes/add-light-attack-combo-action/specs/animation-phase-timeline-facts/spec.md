## ADDED Requirements
### Requirement: 轻攻击连段窗口事实
系统 MUST 提供轻攻击连段窗口事实，用于把当前攻击段的可接段窗口采样为纯数据 facts。该事实 MUST 供统一状态机和 Action 仲裁入口消费，不得让动画外观层、状态机 evaluator 或 MonoBehaviour 各自硬编码窗口规则。

#### Scenario: 窗口前不可接段
- **GIVEN** 当前状态为 `Action.Attack01` 或 `Action.Attack02`
- **AND** 当前 normalized time 早于配置 combo window start
- **WHEN** sampler 采样轻攻击连段窗口事实
- **THEN** `CanComboToNext` MUST 为 false

#### Scenario: 窗口内可接段
- **GIVEN** 当前状态为 `Action.Attack01` 或 `Action.Attack02`
- **AND** 当前 normalized time 位于配置 combo window start 和 end 之间
- **WHEN** sampler 采样轻攻击连段窗口事实
- **THEN** `CanComboToNext` MUST 为 true
- **AND** fact MUST 包含下一段 action state

#### Scenario: 窗口后不可接段
- **GIVEN** 当前状态为 `Action.Attack01` 或 `Action.Attack02`
- **AND** 当前 normalized time 晚于配置 combo window end
- **WHEN** sampler 采样轻攻击连段窗口事实
- **THEN** `CanComboToNext` MUST 为 false

#### Scenario: 第三段没有下一段
- **GIVEN** 当前状态为 `Action.Attack03`
- **WHEN** sampler 采样轻攻击连段窗口事实
- **THEN** `CanComboToNext` MUST 为 false
- **AND** fact MUST NOT 指向未定义第四段状态

#### Scenario: sampler 保持纯逻辑
- **WHEN** sampler 采样轻攻击连段窗口事实
- **THEN** sampler MUST NOT 读取 Animancer runtime 对象
- **AND** MUST NOT 读取 `AnimationClip`
- **AND** MUST NOT 读取 `TransitionAsset`
- **AND** MUST NOT 读取 Unity 场景实例或时间单例

### Requirement: 轻攻击窗口与伤害判定分离
系统 MUST 将轻攻击连段窗口与 hitbox、hurtbox、伤害、VFX、SFX、Camera event 和 IK 窗口分离。本变更只允许输出接段窗口事实，不得扩展完整动作 Timeline 轨道。

#### Scenario: 不输出 hitbox window
- **WHEN** 实施轻攻击连段窗口事实
- **THEN** 系统 MUST NOT 输出 hitbox active fact
- **AND** MUST NOT 输出 hurtbox fact
- **AND** MUST NOT 输出 damage fact

#### Scenario: 不输出表现事件轨道
- **WHEN** 实施轻攻击连段窗口事实
- **THEN** 系统 MUST NOT 输出 VFX、SFX、Camera event 或 IK event
- **AND** 后续表现事件轨道 MUST 另开 OpenSpec proposal
