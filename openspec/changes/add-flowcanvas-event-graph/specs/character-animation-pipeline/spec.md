## ADDED Requirements

### Requirement: 动画更新必须遵循原始事实到作者变量再到Pose的链路

正式事实层 MUST提供对齐后的原始Body/Intent观测、已提交状态和时钟身份；动画事件图 MUST唯一拥有当前确定迁移的速度、方向、加速度、朝向误差及运动阶段计算。Pose及其它原动画消费者 MUST读取同次完整变量结果，旧C#派生生产与同义Fact读取 MUST移除，不回填、不默认补值、不保留并行生产。

EventGraph MUST继续使用原生runtime。Pose根事务、Action、source、混合、曲线、Foot/FBBIK、Evaluate、Seal与最终Writer MUST保持原执行链；节点局部求值与时钟不能整体搬入全局事件更新。变量消费需求与静态绑定 MUST由既有Compiler或实例装配确定。

#### Scenario: 正式动画更新

- **WHEN** 对齐原始事实后执行本次原生事件更新
- **THEN** 系统 MUST冻结本次完整动画变量供同次Pose及原消费者读取
- **AND** 相同动画派生量 MUST不再由FactProjector重复计算

#### Scenario: 原已提交状态直接消费

- **WHEN** 既有规则使用MovementMode或Grounded等正式外部状态
- **THEN** MUST保持其原始事实来源和Gameplay含义
- **AND** MUST不因本次迁移新增动作或移动策略

#### Scenario: 事件更新失败

- **WHEN** 原生更新发生节点错误或输出非法
- **THEN** MUST不发布部分变量或以旧Fact结果继续
- **AND** MUST保持原实例Fault/Reset与Pose事务边界

### Requirement: Corin更新内容必须与原消费者共同迁移

Corin事件图的已确定派生计算、类型和变量引用 MUST与原Pose/条件/修正/选择等消费者共同接入。未迁移消费者不能被静默绕过；缺少真实图内容不能以Profile绑定、序列化或产品发布作为完成。

#### Scenario: 迁移部分完成

- **WHEN** 只有宿主或变量声明存在，原计算或原消费者尚未迁移
- **THEN** MUST保留该部分未完成状态
- **AND** MUST不删除Corin接入目标、生成空图或恢复旧提供者来宣称完成
