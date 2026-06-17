## ADDED Requirements

### Requirement: 旧平铺配置入口必须退役

正式角色运行时 MUST 只通过 `CharacterConfigSO` 及其子配置解析 Locomotion 与 FullBody Action 配置。旧平铺序列化字段不得作为 fallback、兼容读取或未来动作模板继续存在。

#### Scenario: 正式运行时不读取旧平铺字段

- **GIVEN** Corin prefab 和正式 scene 已绑定 `CharacterConfigSO`
- **WHEN** Locomotion 与 FullBody Action runtime 初始化
- **THEN** 配置解析只读取角色根配置及其子配置
- **AND** `runAnimationConfig`、旧 `config`、旧 `stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 不参与配置解析

#### Scenario: 缺失子配置时不 fallback 到旧字段

- **GIVEN** `CharacterConfigSO` 缺失 Locomotion 或 FullBody Action 子配置
- **WHEN** runtime 初始化或测试构造缺失配置场景
- **THEN** runtime 报告缺失正式配置
- **AND** runtime 不读取旧平铺字段补齐配置

### Requirement: 正式资产不得保留旧字段风险

正式 prefab、scene 和角色配置资产 MUST 不保留旧平铺字段的非空值；完成清理后，也不得保留可被 Unity 重新识别为正式配置面的旧字段序列化键。

#### Scenario: Prefab 不含旧字段残留

- **GIVEN** Corin 正式 prefab 被扫描
- **WHEN** 测试检查旧字段名和旧组件引用
- **THEN** prefab 不包含旧字段非空值
- **AND** prefab 不包含已退役字段的序列化键残留
- **AND** prefab 只通过 `CharacterConfigSO` 连接正式配置链

#### Scenario: Scene 不含旧字段残留

- **GIVEN** 正式 gameplay scene 被扫描
- **WHEN** 测试检查旧字段名和旧组件引用
- **THEN** scene 不包含旧字段非空值
- **AND** scene 不包含已退役字段的序列化键残留
- **AND** scene 不恢复旧配置入口或旧 presenter

### Requirement: 历史 GUID 迁移必须可追踪但不可成为旧路径入口

迁移后的正式配置资产 MAY 复用历史 GUID 保持引用稳定，但 MUST 位于正式目录，且旧目录不得作为加载或作者ing 入口继续存在。

#### Scenario: 迁移后的 Action 配置引用正式路径

- **GIVEN** Corin Dodge、RequestPolicy 等配置资产从旧目录迁移到角色专属正式目录
- **WHEN** 测试解析 GUID 引用
- **THEN** 引用解析到正式角色目录下的资产
- **AND** 旧 FullBody 配置目录不作为正式入口存在

#### Scenario: 旧 FullBody 状态机 GUID 不再被正式资产引用

- **GIVEN** 历史 FullBody 状态机资产已退役
- **WHEN** 测试扫描 prefab、scene 和角色配置资产
- **THEN** 正式资产不引用旧 FullBody 状态机 GUID
- **AND** Locomotion 状态图引用指向正式 Locomotion 配置目录
