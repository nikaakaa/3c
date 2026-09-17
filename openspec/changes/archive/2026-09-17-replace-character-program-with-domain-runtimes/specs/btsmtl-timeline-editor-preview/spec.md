## ADDED Requirements

### Requirement: Timeline预览必须复用原生Pose实现

有限动作 Timeline Preview MUST继续使用自己的 session-local 动作状态与正式 Action adapter，绑定同一原生 Pose Factory、资源、Slot、Source 和最终输出规则。它 MUST不要求旧角色 Program／Pose Image，不执行 Gameplay 或树逻辑，也不读取活动角色私有状态；持续 Locomotion 预览继续归 Pose 入口。

#### Scenario: 在同一动画配置上预览有限动作
- **WHEN** 作者预览一个合法有限动作 Timeline
- **THEN** 预览 MUST通过动作请求进入原生 Pose 实例，保留动作时间与 Slot 混合语义
- **AND** MUST不创建另一套动画执行器或生成临时角色 Program


### Requirement: 预览接入必须以领域实际准备和采用事实为准

预览 MUST消费角色领域工厂提供的技能、Pose、Camera、Motion准备与采用事实，分别显示请求来源／版本、Pending／Ready／Missing／Invalid／Failed及精确原因，并显示当前actor真正采用的版本和实例。预览 MUST不重建Character Build／ProgramEpoch，不计算假全局版本，不实现Camera或Motion准备，不因作者保存或准备Ready就显示已采用。原独立作者预览的会话实现迁移由预览任务唯一负责；本任务只提供正式实例／输入／结果合同，不扩建窗口私有执行路径。

#### Scenario: Camera尚未采用新绑定
- **WHEN** Camera资源准备已Ready但当前actor仍使用旧BindingId
- **THEN** 预览 MUST明确显示当前实际BindingId和待采用状态，不显示整个角色已更新
