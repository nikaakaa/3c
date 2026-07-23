# Change: 重构动画播放为显式Blend Stack算法内核

## 重新基线

本change已经安装的entry、独立clock、CrossFade、Stored Pose、Per-Bone Blend Profile、source capture与exact release继续保留，但最终所有权由`refactor-animation-selection-pose-graph-boundary`重新定界。现有Inertial残差数学、history与rebase实现作为迁移输入交给`refactor-inertial-blending-to-local-pose-node`，不再属于Blend Stack目标职责。

- Blend Stack不再由每个`PoseSlotId`自动装配，而是由Pose Graph中的显式`BlendStack`节点按需实例化。
- Stack消费`AnimationSelectionFrame`并输出普通Pose Value；不再拥有`PoseSlotFrame`专用输出合同。
- transition配置属于具体Stack节点引用的`CharacterAnimationBlendPolicy`，不再属于按Pose Slot组织的全局Blend Library。
- 直接播放由`SelectedPosePlayer`负责；单Pose惯性残差由局部`Inertialization`节点负责。
- 未连接Blend Stack的分支不得在Runtime或Preview后台补建Stack。
- 本change不能按“隐藏per-slot Stack已经完成”独立归档；节点化、算法拆分与旧路径删除必须与两个边界change共同完成。

## Why

迁移前代码已经把Animancer fade替换成per-PoseSlot `AnimationBlendStackRuntime`，并实现CrossFade、Stored Pose、Inertial、Per-Bone transition、source capture与retirement，但owner仍在隐藏Slot。当前实现已经把CrossFade与Stored Pose迁入显式BlendStack节点，把单Pose惯性残差迁入局部Inertialization节点，并删除固定Stack自动装配。

进一步拆分后，三个模块分别回答三个问题：

- `SelectedPosePlayer`：当前Selection采样成什么Pose，何时发生离散source切换。
- `BlendStack`：需要同时保留多个source时，怎样CrossFade、压缩Stored Pose并精确释放source。
- `Inertialization`：不保留旧source时，怎样用上一份完成Pose的残差平滑一个局部分支。

这样MM高频jump不必承担多source Stack；需要共同可见期的Action CrossFade仍使用Stack；作者能在Pose Graph中看见实际处理顺序。

## What Changes

- 每个编译后的显式`BlendStack`节点固定装配唯一`AnimationBlendStackRuntime`；没有该节点的分支不创建Stack。
- 分离`AnimationPlaybackId`、source identity、`AnimationBlendEntryId`与Animancer visual；同source连续sample不重启clock，新generation创建新entry。
- `CharacterAnimationBlendPolicy`由具体Blend Stack节点引用，只保存capacity、Stored Pose、CrossFade default、exact override、canonical curve和`CharacterAnimationBlendProfile`。
- Compiler只为该节点可达selection/Empty组合物化完整CrossFade exact table；Runtime缺失pair直接失败，不fallback。
- 每个CrossFade entry拥有独立Fade Clock，并按每骨骼duration multiplier和push depth计算nested residual weight。
- 容量或快速替换触发时捕获当前完整node Pose、velocity、Pose Parameter和每脚feature aggregate为Stored Pose，再原子释放不再需要的source。
- `AnimancerPoseSamplingBackend`只创建Clip/ManualMixer source、写sample time和child weight、捕获source Pose并管理playable寿命。
- Blend Stack Job只完成source capture后的CrossFade、Stored Pose、参数与feature输出；不执行Inertial residual、跨分支Layer/Additive、FootPlacement或最终写回。
- 旧`AnimationBlendTechnique.Inertial`、Stack accumulator、residual workspace、Projection payload与snapshot字段迁移到局部Inertialization节点后删除。
- Preview、Runtime与Live Debug复用同一编译Pose Plan，按PoseNodeId显示entry、clock、curve、Bone weight、Stored capture、Pose Value与retirement原因。

## Impact

### Specs

- 保留并重写`character-animation-blend-stack`为显式节点算法合同。
- `character-animation-layer-runtime`、`character-animation-presentation-authoring`、`character-animation-pipeline`与`character-foot-placement-presentation`的新边界统一由`refactor-animation-selection-pose-graph-boundary`和`refactor-inertial-blending-to-local-pose-node`修改，本change不再维护重复delta。

### Code

- Blend Stack entry、CrossFade clock、Stored Pose、capacity、source usage与release。
- Animancer source sampling backend与单次PlayableGraph Evaluate装配。
- node-local Blend Policy、Rig/Blend Profile与Projection payload。
- PoseNode snapshot、Preview与Live Debug。
- 现有Inertial数学迁移到唯一PoseInertialization Runtime/Job。

### Active Change 关系

- `refactor-animation-selection-pose-graph-boundary`删除PoseSlot隐藏owner并建立Selection、显式Player与完整Pose Plan。
- `refactor-inertial-blending-to-local-pose-node`接收现有Inertial数学和history，建立`SelectedPosePlayer -> Inertialization`局部路径。
- `add-character-presentation-pose-graph`提供节点authoring、validator、compiler、workspace与Corin最终图。
- Motion Matching只输出`AnimationSelectionFrame`；推荐接入`SelectedPosePlayer -> Inertialization`，不得建立私有fade、Stack或惯性器。

## Breaking Changes

- 删除`PoseSlotId`作为Stack owner和每PoseSlot自动装配规则。
- 删除全局per-slot Blend Library与PoseSlotFrame专用输出。
- 删除Blend Stack内全部Inertial technique、rule、state、workspace、contribution与diagnostics字段。
- 删除Animancer fade、Layer weight、TransitionLibrary与旧global compositor。
- Stack owner改为稳定PoseNodeId，exact transition pair只属于该节点的可达selection集合。
- 不提供旧Projection、旧Blend Library、旧Stack Inertial或旧PoseSlot payload兼容读取。
