## REMOVED Requirements

### Requirement: Preview必须执行正式Projection和Pose Plan

**Reason**：旧合同以独立 Action/Fact/Query fixture 和非连续 seek 驱动完整角色。
**Migration**：采用“场景预览必须使用真实Actor的Selection与Pose Plan”，通过正式输入与运行结果观察角色，移除编辑器独立求值。

## ADDED Requirements

### Requirement: 场景预览必须使用真实Actor的Selection与Pose Plan

Action Timeline、Pose Graph、Blend Space 与 Motion Matching 的完整角色预览 MUST连接独立场景 Play 中的真实 Actor，使用其正式 Projection、readiness、Player、Routing、Slot、Inertialization、source、release 和 reset 结果。作者输入 MUST经正式角色输入、C#控制与唯一Action服务产生ActionInstance及技能输出，Presentation Fact MUST来自实际角色提交；窗口不能替代选择。state-local Locomotion source MUST不要求技能或ActionInstance，有限技能动画 MUST关联其准确释放和调用generation。预览 MUST不创建 BaseLocomotion Timeline、手动 Gameplay winner、简化 Player、隐藏 Stack、临时 PlayableGraph 或 Animancer direct Play 路径。

#### Scenario: Pose预览改变移动输入

- **WHEN** 作者通过明确的正式输入让角色从静止进入移动
- **THEN** Body/Intent MUST产生真实速度 Fact，再由正式规则决定 Pose State
- **AND** 窗口 MUST不覆盖 HorizontalSpeed 或发送强制 PlayRun 事件

#### Scenario: Timeline编辑游标跳转

- **WHEN** 作者把编辑游标移到另一个 Action sample 位置
- **THEN** 编辑视图 MUST更新定位，真实 Actor 的 lifecycle 与动画状态 MUST不变
- **AND** MUST不为游标跳转插入 BlendStack 或执行额外 Pose 采样
