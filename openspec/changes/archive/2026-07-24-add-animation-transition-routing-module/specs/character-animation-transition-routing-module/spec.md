# Character Animation Transition Routing Module Specification

## ADDED Requirements

### Requirement: Transition Routing模块必须独立于现有Pose执行链

项目 MUST提供target-neutral Transition Routing模块，只消费稳定identity、compiled transition rules与调用方显式提交的frame facts，并只输出transition decision、typed request、completion permission与结构化诊断。模块 MUST不引用AnimationClip、Animancer、PlayableGraph、Pose Graph、Character Animation Presentation Profile、Corin资产、Gameplay State或Network Model，也 MUST不计算Pose、骨骼速度、CrossFade weight、Stored Pose或Inertial residual。

#### Scenario: 独立执行模块帧

- **WHEN** Fixture向模块提交合法Plan、current endpoint、requested endpoint与readiness facts
- **THEN** 模块 MUST只根据compiled rule和当前workspace产生Frame Output
- **AND** MUST不访问角色资产、场景对象或现有动画Runtime

### Requirement: Blend Logic合同必须使用UE对应语义

`AnimationTransitionBlendLogic` MUST只包含`StandardBlend`与`Inertialization`。Hard Cut MUST表示为Duration为零的Standard Blend outcome；`Custom`与Stored Pose MUST不进入Blend Logic。Inertialization rule MUST使用正Duration，并且 MUST不以Empty作为target。

#### Scenario: 编译零时长Standard Blend

- **WHEN** exact rule选择StandardBlend且Duration为零
- **THEN** Plan MUST保留StandardBlend作为Blend Logic
- **AND** Frame Output MUST把结果标记为Hard Cut outcome

#### Scenario: 编译非法Inertialization

- **WHEN** rule选择Inertialization但target为Empty或Duration非正
- **THEN** Compiler MUST以结构化reason拒绝Definition
- **AND** MUST不改用Standard Blend

### Requirement: Routing Plan必须编译完整exact transition matrix

模块Compiler MUST从ordered endpoint catalog与rule table生成不可变Compiled Transition Routing Plan。每个合法source-target pair MUST存在唯一exact rule；重复endpoint、重复RuleId、重复pair、缺失pair、未知endpoint或非法identity MUST编译失败。Plan MUST包含schema version、Definition revision、稳定PlanId与canonical hash。

#### Scenario: exact pair缺失

- **WHEN** Definition声明三个endpoint但缺少一个合法source-target pair
- **THEN** Compiler MUST指出缺失的source与target identity
- **AND** MUST不生成默认rule或稀疏fallback

### Requirement: Inertialization Request必须是无Pose的typed控制事件

`PoseInertializationRequest` MUST包含RequestEventId、owner node identity、RuleId、source endpoint、target endpoint、selection generation、request generation、Duration与Blend Profile identity。Request MUST不包含Pose、骨骼数组、速度、播放器handle、Unity对象或consumer实例。

#### Scenario: 发布已准备请求

- **WHEN** Inertialization rule的target与capture plan均已准备
- **THEN** 模块 MUST发布具有稳定event与generation的typed request
- **AND** request MUST只表达控制身份和参数合同

### Requirement: Request生命周期必须原子处理capture completion

Inertialization transition MUST在target尚未准备时使用AwaitingTarget，在request发布时使用Prepared，并在后续Frame尚未收到capture completion时进入AwaitingCaptureCompletion。若下一份Frame已经携带匹配completion，模块 MUST允许从Prepared直接进入Committed，不得强制调用方提交一帧空等待。模块 MUST只在target identity、selection generation、request generation和capture completion全部匹配后输出旧source release permission。任何reset、过期completion或identity不匹配 MUST使旧completion失效，并且 MUST不输出release permission。

#### Scenario: target尚未准备

- **WHEN** exact rule选择Inertialization但TargetReady为false
- **THEN** lifecycle MUST保持AwaitingTarget
- **AND** 模块 MUST不发布request或release permission

#### Scenario: capture完成

- **WHEN** 当前request收到匹配generation的成功capture completion
- **THEN** lifecycle MUST进入Committed
- **AND** Frame Output MUST允许调用方执行旧source release

#### Scenario: capture尚未完成

- **WHEN** Prepared request在后续Frame仍未收到capture completion
- **THEN** lifecycle MUST进入AwaitingCaptureCompletion
- **AND** 模块 MUST不输出旧source release permission

### Requirement: 路由决策与completion outcome必须独立

Frame Output MUST使用`TransitionRouteDecisionKind`只表达本帧路由动作，并使用独立的CompletionOutcome表达匹配completion经过校验后的结果。Capture completion MUST先产生release permission，release completion MUST只在后续Frame回报；同一Frame同时提交两种completion MUST进入Invalid。后续Standard Blend、Inertialization或无路由处理 MUST不覆盖已经产生的completion outcome。`CaptureCommitted`与`ReleaseCompleted` MUST不进入`TransitionRouteDecisionKind`。

#### Scenario: capture提交后继续解析当前规则

- **WHEN** Frame提交匹配capture completion且当前source-target仍选择同一Inertialization rule
- **THEN** CompletionOutcome MUST包含CaptureCommitted
- **AND** TransitionRouteDecisionKind MAY为None
- **AND** Frame Output MUST保留release permission

#### Scenario: 同帧提交capture与release

- **WHEN** Frame同时提交capture completion与release completion
- **THEN** lifecycle MUST进入Invalid
- **AND** Frame Output MUST不输出release permission或completion outcome

### Requirement: 连续打断必须保持单一当前request

Standard到Standard MUST输出新的普通混合命令；Standard到Inertialization MUST进入request准备；Inertialization到Inertialization MUST提升request generation并输出RebaseRequired；Inertialization期间收到Standard MUST只输出上游普通混合命令。模块 MUST不同时保留多个pending request，也 MUST不建立Pose accumulator概念。

#### Scenario: 惯性请求被再次打断

- **WHEN** 当前Inertialization request尚未结束且新target再次选择Inertialization
- **THEN** 模块 MUST提升request generation并失效旧completion
- **AND** MUST只保留新request并输出RebaseRequired

### Requirement: Reset必须清理控制状态但不得伪造外部资源结果

显式Reset、seek、owner generation变化或Plan replacement MUST清理pending request、completion等待与当前transition，并提升模块generation。Reset MUST记录结构化reason，但 MUST不输出capture success、release success或任何外部Pose资源状态。

#### Scenario: AwaitingCapture期间seek

- **WHEN** lifecycle处于AwaitingCaptureCompletion且收到seek reset
- **THEN** 模块 MUST清理当前request并提升generation
- **AND** 后续旧capture completion MUST被拒绝

### Requirement: Editor Fixture必须只通过显式操作驱动正式模块

项目 MUST提供模块专属Editor Fixture，使用独立Definition和有序Frame Fact序列调用正式Compiler与Runtime API。工作区 MUST提供显式Compile、Reset Runtime、Step Frame、Run Sequence与Clear Timeline操作；选择资产、打开窗口、修改字段、domain reload、asset import或Play Mode变化 MUST不自动Compile或Run。Fixture MUST显示Plan、rule、request lifecycle、generation、completion、reason和有界事件时间线，并明确显示Pose Evaluation未连接。

#### Scenario: 修改Fixture规则

- **WHEN** 作者修改一条Fixture transition rule
- **THEN** 工作区 MUST只把Definition标记为Dirty
- **AND** MUST等待作者显式点击Compile

#### Scenario: 执行Frame Sequence

- **WHEN** 作者显式点击Run Sequence
- **THEN** Fixture MUST逐项调用正式模块Frame API
- **AND** MUST显示每帧输入、输出和状态迁移
- **AND** MUST不创建PlayableGraph或伪造视觉Pose结果
