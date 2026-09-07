## MODIFIED Requirements

### Requirement: Animancer必须只负责source采样

唯一 Source Module MUST 只消费完整 Action playback 或 Presentation Pose source identity，以及编译后的资源、effective sample、loop、play rate 和 source-local clip weight。每个动画资源 MUST 在 Build 时唯一选择 NativeClip 或 ACL backend，Action、Direct Clip、Blend Space、Motion Matching 和 Preview MUST 解析同一份 dense 资源合同；同一资源的不同使用点 MUST 不绕回另一个隐式 backend。NativeClip 仍由 Animancer Clip/ManualMixer 采样；ACL MUST 使用声明的资源数据，MUST 不依赖同素材 AnimationClip 的运行时强引用或备用播放器。

Program Runtime MUST 继续拥有 PoseState、Player endpoint、ActionPlaybackInput lifecycle、Transition、Slot、Blend Stack 和 Inertialization 逻辑。Source Module MUST 统一管理物理 source、capture、usage 对接和 release completion，MUST 不仲裁 State 或 Action winner、不推进 Action lifecycle、不读取运行时 AnimationClip 作者曲线、不选择 Phase leader、不计算跨 source weight、不执行 Foot Placement、Goal Assembly、FBBIK 或 Final Publication。有效时间 MUST 由 Program 一次确定，backend MUST 不重复累计时钟或再次应用 play rate。

资源准备 MUST 在表现帧外取得进展；预期 Pending MUST 通过正式 outcome 返回。已有合法 source 时，候选 target Pending MUST 由 Program 按既有语义保持当前 source 并采样本帧；Entry Pending MUST 不发布 Final Pose。Source MUST 不以历史 sample、bind pose、默认 Idle 或其它 backend 伪造 Ready。

#### Scenario: PoseState transition共同采样两个source

- **WHEN** Program 中的 Standard Blend 要求 source 与 target 同时可见，且各自需要的资源已经 Ready
- **THEN** Program Runtime MUST 发布两份 typed Demand 与各自 effective sample，Source Module MUST 在同一表现 Barrier 提供两份 capture
- **AND** source 间 weight、Transition clock 和 release permission MUST 仍由 Program 计算

#### Scenario: Source backend尝试选择State

- **WHEN** Source readiness 或 Playable 状态发生变化
- **THEN** Source Module MUST 只发布 Pending、Ready、Invalid 或 release completion 结果
- **AND** MUST 不直接修改 PoseState、Player generation、Transition 或 OutputPose

#### Scenario: 同一ACL资源被多个使用点引用

- **WHEN** 同一 ACL 动画同时出现在 Direct Clip、Action 或 Blend Space 的编译闭包中
- **THEN** 所有使用点 MUST 解析同一资源版本，各个活跃 source 保持自己的时间与 generation
- **AND** 运行包 MUST 不因某个使用点仍引用旧 Clip binding 而携带该素材的展开播放替代品

#### Scenario: 候选ACL资源仍在加载

- **WHEN** target 的所需 payload 或质量数据尚未完成准备
- **THEN** backend MUST 返回 Pending，加载任务 MUST 能独立于该表现帧的 Seal 继续完成
- **AND** 当前合法 source 的继续采样 MUST 保持既有节点语义，不能被误判为 target 已经 Ready
