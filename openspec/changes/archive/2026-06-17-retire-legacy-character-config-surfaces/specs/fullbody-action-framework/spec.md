## ADDED Requirements

### Requirement: FullBody 兼容视图只能只读保留

`FullBodyStateView` 等兼容视图 MAY 保留为诊断和动画观察面，但 MUST 不成为动作仲裁、动作生命周期推进、配置解析或输入请求提交的来源。

#### Scenario: 兼容视图只暴露观察状态

- **GIVEN** `CharacterAnimancerPresenter` 或诊断工具读取 FullBody 状态
- **WHEN** 它访问 `FullBodyStateView`
- **THEN** 只能读取当前动作、phase、规格化时间和诊断状态
- **AND** 不能通过该视图提交动作请求、切换动作或推进动作生命周期

#### Scenario: 动作仲裁不读取兼容视图

- **GIVEN** 输入提交者请求 Dodge 或其他 FullBody Action
- **WHEN** Action runtime 判断能否进入、打断或结束动作
- **THEN** 仲裁只使用正式 runtime state、request policy 和 action definition
- **AND** 不从 `FullBodyStateView` 反向推导仲裁结果

### Requirement: 旧 Presenter 不得作为正式动画播放路径

正式动画播放 MUST 通过 `CharacterAnimancerPresenter` 接入 Locomotion 与 FullBody Action。旧 locomotion/action presenter 不得挂载到正式 prefab/scene，也不得作为未来动作模板引用。

#### Scenario: 正式 prefab 使用统一 Presenter

- **GIVEN** Corin 正式 prefab 被扫描
- **WHEN** 测试检查 Animancer presenter 组件
- **THEN** prefab 使用 `CharacterAnimancerPresenter`
- **AND** 不挂载旧 locomotion presenter 或旧 action presenter

#### Scenario: 未来动作不依赖旧 Presenter

- **GIVEN** 新 FullBody Action 配置或测试样例被添加
- **WHEN** 它声明动画播放依赖
- **THEN** 依赖目标是正式 action animation profile 与 `CharacterAnimancerPresenter`
- **AND** 不引用旧 action presenter 作为模板或桥接层

### Requirement: 旧 Action 配置 API 不得作为扩展入口

FullBody Action 扩展 MUST 通过正式 action definition、request policy 和 runtime port 接入。旧 Dodge runtime config、旧 interrupt policy 字段或旧 compatibility 属性不得作为新动作的扩展入口。

#### Scenario: 新动作配置不使用旧 Dodge 字段

- **GIVEN** 新 FullBody Action 配置被创建
- **WHEN** 测试检查其配置依赖
- **THEN** 配置使用正式 action definition 和 request policy
- **AND** 不读取旧 Dodge runtime config 兼容字段或旧 interrupt policy 字段

#### Scenario: 旧兼容属性不能补齐缺失配置

- **GIVEN** 正式 action configuration 缺失某个必需子配置
- **WHEN** runtime 初始化或请求动作
- **THEN** runtime 报告正式配置缺失
- **AND** 不通过旧 compatibility 属性补齐缺失配置
