## MODIFIED Requirements

### Requirement: Timeline Editor 必须完整编辑显式注册的 Continuous Curve Channel

Timeline Editor MUST通过显式 typed Curve Channel Catalog 显示和编辑 Timeline owner 已经正式拥有的 Continuous Curve。每个 descriptor MUST声明稳定 ChannelId、owner 类型、显示名、颜色、time domain、value domain、单位、完整 curve 读取、正式 owner mutation 和领域 validator。Catalog MUST覆盖 Animation Segment 的 Weight、Ease In 和 Ease Out，MotionCurve Clip 的 Weight、Position X/Y/Z、Yaw 和 Ease In/Out，MotionWarp Clip 的 Position Progress 与 Yaw Progress，以及正式相机 Sequence/Response 等 Clip 和 CameraResourceClip 自己拥有的 Weight 和 Ease In/Out。旧 CameraStateClip 的曲线 owner MUST随相机序列请求迁移到对应正式 Clip，不能保留旧类型兼容编辑入口。共享相机效果/Curve 资源 MUST只提供只读引用显示及打开真实 owner 的导航，不进入 Timeline-local 可写 Catalog。`presentation.locomotion-phase`、`presentation.foot-placement-weight`、AnimationClip 骨骼曲线和其它 Clip 内注册曲线 MUST只由 Unity Animation Window 编辑，不得进入 Timeline Catalog。

每个具有 registered Timeline channel 的 Track MUST显示可折叠 `CURVES` 分组，展开后每个 ChannelId 拥有独立 lane。每个 Clip 或 Segment MUST只在自己的 StartFrame..EndFrame 范围显示自己的 Timeline-local curve、key 与边界；重叠内容 MUST不在作者层合并曲线。Curve Lane MUST按完整 `AnimationCurve.Evaluate` 结果绘制插值，显示原始 key、tangent handle、当前游标 time/value、value reference 与单位。

#### Scenario: 展开Animation Track曲线

- **WHEN** 作者展开包含 Animation Segment 的 CURVES 分组
- **THEN** Editor MUST只显示 Weight、Ease In 与 Ease Out 三个 Timeline-local typed channel
- **AND** Foot Placement Weight 与 Locomotion Phase MUST不出现在该分组

#### Scenario: 展开MotionCurve Track曲线

- **WHEN** 作者展开 MotionCurve Clip 的 CURVES 分组
- **THEN** Editor MUST显示 Weight、Position X/Y/Z、Yaw 与 Ease In/Out
- **AND** Position 与 Yaw MUST使用 unbounded value view 及明确单位

#### Scenario: 展开相机请求和共享效果曲线

- **WHEN** 作者展开相机 Sequence Clip 的 CURVES 分组并查看所引用效果的曲线
- **THEN** Timeline MUST只允许直接修改该 Clip 的正式 Timeline-local channels
- **AND** 共享效果曲线 MUST显示真实 owner 导航，不复制为 Clip 私有曲线

## ADDED Requirements

### Requirement: 相机预览必须与动画共享唯一 Preview 生命周期

TimelinePreviewSession MUST在现有显式角色预览目标上装配相机能力，使同一会话的已发布 Projection、Body 可见姿态、最终动画目标、相机请求、时间和镜头输出保持一致。相机 MUST复用正式 Runtime 的序列、效果、阻尼、碰撞与 Adapter 实现，只将外部输入替换为明确 Preview fixture。预览 MUST不执行 Gameplay Program、TreeClip、AI、GameplayEffect 或 WorldSolver，也不得直接解释相机作者对象形成另一个播放器。

#### Scenario: 预览技能镜头和动作

- **WHEN** 作者在有效角色目标上播放包含相机轨道的有限 Action Timeline
- **THEN** 同一会话 MUST先发布 Body 与最终动画姿态，再推进相机并显示结果
- **AND** 相机事件 MUST由该会话的明确预览命令源提交，不能读取真实运行 Actor 的请求容器

#### Scenario: 预览 Profile 默认跟随

- **WHEN** 作者从 Character 工作区明确启动默认相机预览并提供目标与输入 fixture
- **THEN** 预览 MUST使用同一已发布相机计划与正式求值实现
- **AND** MUST不伪造 Gameplay Timeline 或另建 Simulation Session

### Requirement: 有状态相机的 seek 必须重建同一运行历史

相机 Preview MUST显式保存会话初始镜头、目标轨迹、输入轨迹、时间尺度、随机种子和命令顺序。向后 seek、任意跳转、循环或重新播放 MUST从同一会话初始状态或精确匹配的会话检查点，经正式相机 Runtime 按该 fixture 的时间步骤重建到目标时间；不能仅对最终时点采一次曲线。重建 MUST可取消且不在 Inspector 绘制中同步阻塞，过程中 MUST明确显示准备状态而不声称已完成采样。

#### Scenario: 从技能尾部拖回震屏之前

- **WHEN** 作者把游标从效果退出阶段拖回首次 Shake 之前
- **THEN** Preview MUST清除未来事件的生命周期、阻尼和噪声历史，并重建目标时点
- **AND** 再次播放经过该 Shake 时 MUST只触发一次

#### Scenario: 正向播放与直接跳转同一时点

- **WHEN** 两次预览使用相同已发布计划、初始状态和完整 fixture
- **THEN** 连续播放与 seek 重建在目标时点 MUST得到一致的序列/效果状态与镜头结果
- **AND** MUST不使用当前场景相机姿态作为每次 seek 的不同隐式起点

### Requirement: 相机 Preview 必须独占明确物理输出并报告缺失上下文

预览目标 MUST明确提供相机输出、Rig、目标绑定、必要物理场景及已发布 Projection，并对其物理输出建立独占会话归属。第二个预览或 Live Runtime 占用相同输出时 MUST拒绝绑定。没有目标、资源过期、缺少碰撞场景或 Shot 绑定时 MUST显示具体不可用原因；数据仍可编辑，但 MUST不搜索场景、关闭必要效果或采用默认镜头补齐。Stop、Dispose、target 切换和 domain reload MUST释放会话拥有的相机资源与状态并恢复其接管前输出绑定，不修改作者资产。

#### Scenario: 两个窗口预览同一个相机

- **WHEN** 第二个窗口试图绑定已被一个 Preview session 占用的 Rig
- **THEN** 目标 MUST拒绝第二个会话并显示占用关系
- **AND** 两个窗口 MUST不能交替写入同一输出

#### Scenario: 相机 Projection 已过期

- **WHEN** Profile 已修改而匹配的新 Projection 尚未发布
- **THEN** Preview MUST显示需要明确 Build 并阻止把旧结果标为当前配置预览
- **AND** MUST不自动构建临时相机计划

### Requirement: Timeline 相机 Live Debug 必须消费真实运行快照

Timeline Live Debug MUST将同一正式运行实例的 Camera producer/generation、活动效果、原时点、当前时间域、进入/退出、目标与混合状态投影到相应 Clip 和事件。Follow/Pin MUST继续属于当前窗口绑定。Live MUST只读，不能调用 Preview 求值、重建运行历史或修改真实相机；多个实例 MUST由作者明确选择，不按列表顺序猜测。

#### Scenario: 同一个技能存在两个 generation

- **WHEN** 旧技能相机正在退出而同源新技能已经开始
- **THEN** Live Debug MUST分别显示两个 generation 与其状态和来源
- **AND** 作者 Pin 一个实例 MUST不改变另一个窗口或实际镜头裁决

#### Scenario: 相机资源和快照 revision 不匹配

- **WHEN** 当前资源已修改而 Live 快照来自旧 Projection
- **THEN** 窗口 MUST显示不匹配并停止用新 source mapping 解释旧快照
- **AND** MUST不退回 Authoring Preview 冒充 Live
