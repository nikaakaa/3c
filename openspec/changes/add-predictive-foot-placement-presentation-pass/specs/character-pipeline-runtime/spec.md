# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: PresentationFrame 必须原子提交动画播放生命周期

PresentationFrame MUST按固定顺序读取 Committer queue、采样 selected/retained producer、更新 AnimationPlaybackLifecycle、调用 Animancer adapter并完成本帧Evaluate、执行唯一已装配Pose Post Process Pass、推进Camera、退休 outgoing并 acknowledge batch。Pose Post Process MUST只消费Animancer最终姿势、同帧Body frame和Presentation-owned配置；该阶段整体 MUST不执行 Program、TreeClip、Motion、Action、Effect 或 WorldSolver，也 MUST不产生Gameplay事实、网络输出或第二次VisualRoot写入。

#### Scenario: Selection 与首个 Sample 同批

- **WHEN** target selection 与合法 sample 同批到达
- **THEN** lifecycle MUST原子切换 Current/Outgoing
- **AND** Pose Post Process MUST只观察切换后Animancer生成的最终姿势

#### Scenario: 动画输出尚未就绪

- **WHEN** RequireOutput layer仍等待target首个合法sample且当前没有正式动画输出
- **THEN** Pose Post Process MUST不对残留骨骼姿势求解
- **AND** 已有Pose Post Process历史 MUST按正式reset语义清除

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation、Foot Placement和Camera diagnostics MUST进入统一 structured Trace/view model。Inspector MUST不遍历旧stage、Final IK组件或runtime service私有集合形成平行调试链。Foot Placement trace MUST只读取正式Presentation snapshot，不得重新执行地面查询或solver。

#### Scenario: 查看一次 Dodge Tick

- **WHEN** Debug Session 定位 Dodge EventId
- **THEN** MUST关联 input、operation、world batch 与 committed animation command

#### Scenario: 查看楼梯上的右脚replant

- **WHEN** Foot Placement snapshot记录右脚因超出reach从Locked释放
- **THEN** 统一Trace MUST显示同帧Body、visible producer、surface、constraint reason和pelvis offset
- **AND** Inspector MUST不直接读取Final IK mutable solver状态
