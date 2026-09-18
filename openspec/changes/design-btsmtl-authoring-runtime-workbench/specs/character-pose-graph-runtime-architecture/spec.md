## MODIFIED Requirements

### Requirement: Preview与正式Runtime必须复用同一Module Factory和Program Image

完整角色预览 MUST使用独立场景中的真实 Actor，由正式运行入口装配唯一 Program Image、actor-local Execution View、Program Runtime、Source Module、Constraint Module、Final Publication、根帧事务和 Tuning Snapshot。场景预览与其它正式运行 MUST使用同一模块合同和 completion 语义；窗口 MUST不再装配自己的模块实例或注入替代的 Presentation Fact、Action、query、world context 与 source sample。场景中的每个 Actor MUST继续独立拥有自己的运行状态，不得共享可变工作区或形成第二个播放器。

#### Scenario: Preview缺少world context

- **WHEN** 预览场景的真实 Actor 缺少 Foot Placement 要求的精确世界上下文
- **THEN** 正式 Program Runtime MUST发布 typed Unavailable 并停止该帧发布
- **AND** 预览 MUST不跳过约束、伪造地面或创建简化运行实例

### Requirement: Reset、Replacement与Dispose必须按Owner清理状态

Program replacement、Projection revision 变化、Actor reset、Fault 和 Dispose MUST由根 Runtime 产生 typed reset reason，按固定顺序由 Program、Source、Constraint 和 Final Publication 清理各自状态。Reset MUST提升相关 generation，使旧 Frame lease、Tuning Candidate、source completion、constraint result 和 diagnostics 失效；Projection replacement MUST释放旧 Execution View 并只从新 Image/hash 创建新 View，不迁移旧 ABI 或保留旧 source 补充路径。

场景重建 MUST经正式 Session/Actor 的 Quiesce、Dispose 与构造完成，不增加窗口 seek 重置原因或让 Editor 直接清模块内部页。Constraint 成功 Reset MUST保持已经验证的初始化结果和既有独立 Reset 证据，不借本次迁移改动 BendHistory、Vendor 方向修正或恢复已删除的 Vendor 读取。

#### Scenario: Projection被显式重建

- **WHEN** 新预览运行使用显式 Build 后的 Projection
- **THEN** 旧 Actor 状态、Frame lease 和延迟 completion MUST失效，旧 source 按正式顺序释放
- **AND** 新 Runtime MUST只从新 Image 与容量建立状态

#### Scenario: 场景重新开始试验

- **WHEN** 作者在同一次 Play 中重建预览场景
- **THEN** 模块 MUST收到正式 Actor 释放与重建生命周期
- **AND** MUST不由窗口按名字重置 Foot、动画或相机内部字段
