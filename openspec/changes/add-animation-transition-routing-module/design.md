# Design: 独立Animation Transition Routing模块

## Context

项目现有动画系统已经能执行普通CrossFade和Inertialization，但选择逻辑分散在不同Policy和图拓扑中。第一阶段需要验证的是“过渡控制协议”，不是再次实现动画数学。

模块边界固定为：

```text
稳定Identity + Exact Rules + Frame Facts
  -> Transition Routing Module
  -> Decision + Typed Request + Completion Contract + Snapshot
```

模块不得知道：

- AnimationClip、Animancer、PlayableGraph或Unity Transform。
- Pose数组、骨骼速度、Quaternion residual或Blend Profile内容。
- CharacterAnimationPresentationProfile、Pose Graph或Projection资产。
- Gameplay State、Action、Timeline、Motion Matching或Network Model。

## Goals

- 独立编译完整source-target transition matrix。
- 用UE对应术语表达Standard Blend与Inertialization。
- 形成可重复驱动的request准备、capture、提交、release许可和reset状态机。
- 在连续打断中保持单一pending request和稳定generation。
- 用正式Fixture观察每帧输入、输出和错误。
- 为后续唯一Pose Plan接入提供不可变合同，不提前影响现有动画板块。

## Module Ownership

模块拥有：

- Blend Logic合同。
- exact rule查找。
- request event identity。
- request lifecycle。
- transition generation。
- capture/release握手状态。
- 结构化reason与snapshot。

模块不拥有：

- source播放器寿命。
- Standard Blend entry和weight。
- Stored Pose。
- completed Pose history。
- residual、velocity、衰减和rebase数学。
- Marker relation。
- Foot Feature或Foot Placement。

## Data Model

### AnimationTransitionBlendLogic

合法值只有：

```text
StandardBlend
Inertialization
```

`StandardBlend`的Duration可以为零。`Inertialization`必须拥有正Duration。`Custom`和Stored Pose不是合法值。

### TransitionEndpointIdentity

endpoint由稳定source identity和selection generation组成。模块不接收显示名，也不从对象引用推导identity。

### AnimationTransitionRule

每条规则至少包含：

- RuleId
- SourceEndpointId
- TargetEndpointId
- BlendLogic
- Duration
- BlendCurveIdentity
- BlendProfileIdentity

Rule只保存identity和数值合同，不保存Unity资产对象。

### CompiledTransitionRoutingPlan

编译产物包含：

- PlanId
- SchemaVersion
- DefinitionRevision
- OrderedEndpointCatalog
- 完整exact pair table
- CanonicalHash

缺少pair、重复pair、未知endpoint、Inertialization到Empty、非法duration或重复RuleId必须编译失败。

### TransitionRoutingFrameInput

每帧输入包含：

- PlanId
- FrameId
- OwnerNodeId
- CurrentEndpoint
- RequestedEndpoint
- SelectionGeneration
- TargetReady
- CapturePlanReady
- CaptureCompleted
- ReleaseCompleted
- ResetReason

这些都是调用方已经确认的事实。模块不得访问外部Runtime猜测事实。

### PoseInertializationRequest

request包含：

- RequestEventId
- RouteOwnerNodeId
- RuleId
- SourceEndpoint
- TargetEndpoint
- SelectionGeneration
- RequestGeneration
- Duration
- BlendProfileIdentity

request不包含Pose、速度、骨骼索引、播放器handle或consumer对象。

### TransitionRoutingFrameOutput

输出为不可变结果，包含：

- DecisionKind
- ActiveRuleId
- StandardBlendCommand
- InertializationRequest
- RequestLifecycle
- CapturePermission
- ReleasePermission
- RebaseRequired
- StructuredReason

调用方必须显式回报capture与release completion，模块不自行推进外部资源状态。

## State Machine

### Standard Blend

exact rule选择Standard Blend时，模块立即输出Standard Blend命令。Duration为零时结果标记为Hard Cut outcome，但不改变Blend Logic。

### Inertialization Preparation

exact rule选择Inertialization时：

1. target未准备时进入`AwaitingTarget`。
2. target和capture plan准备后进入`Prepared`并发布request。
3. request发布后进入`AwaitingCaptureCompletion`。
4. 收到匹配request generation的capture completion后进入`Committed`并允许调用方释放旧source。
5. completion identity不匹配时进入`Invalid`，不得发出release许可。

### Repeated Request

已有Inertialization request时再次请求新target：

- 提升RequestGeneration。
- 旧generation的未完成completion全部失效。
- 输出`RebaseRequired`。
- 只保留一个当前request。

### Reset

显式Reset、seek、plan replacement或owner generation变化时：

- 清空pending request。
- 清空completion等待。
- 提升模块generation。
- 输出reset reason。
- 不输出任何旧source release许可。

## Atomicity

模块只在以下事实同时成立时允许release：

- target identity仍与当前request一致。
- selection generation一致。
- request generation一致。
- capture completion成功。
- reset未发生。

release completion只用于关闭模块事件，不允许反向修改已经提交的request identity。

## Fixture

### Definition

Fixture Definition只引用模块自己的endpoint和rule数据。它不是Character Profile，也不能被Projection Compiler发现。

### Explicit Operations

工作区只提供显式操作：

- Compile
- Reset Runtime
- Step Frame
- Run Sequence
- Clear Timeline

修改字段只标记Dirty。选择资产、打开窗口、domain reload和Play Mode变化不得触发Compile或Run。

### Scenario Sequence

作者可以配置有序Frame Facts：

- current/requested endpoint。
- target ready。
- capture plan ready。
- capture completed。
- release completed。
- reset reason。

Run Sequence逐项调用正式模块API，并把每帧输入输出保存到窗口内有界snapshot。Fixture不得实现一套简化状态机。

### Display

工作区显示：

- compiled rule matrix。
- 当前endpoint与requested endpoint。
- Blend Logic。
- request event与generation。
- lifecycle状态。
- capture/release许可。
- rebase标记。
- 结构化错误。

工作区必须明确显示`Pose Evaluation: Not Connected`，避免把控制闭环误认为视觉闭环。

## Assembly And Dependency Direction

正式方向为：

```text
Animation Identity/Core
  <- Transition Routing Module
  <- Editor Fixture

Transition Routing Module
  <- future Pose Plan integration
```

Transition Routing Module不得引用Character Presentation Runtime或Editor。Fixture可以引用模块和Editor基础设施，但不得引用Corin或角色资产。

## Future Integration Contract

后续接入时：

- Player或BlendStack负责构造Frame Facts。
- BlendStack继续执行Standard Blend。
- 下游Inertialization继续执行Pose history和residual。
- Pose Plan completion回报capture/release事实。
- Transition Routing Module只返回决策和许可。

不得把Fixture接入正式Pose Plan，也不得让正式Runtime读取Fixture Definition。

## Rejected Alternatives

### 在第一阶段复制Pose算法

会形成第二套CrossFade和Inertialization实现，拒绝。

### 直接修改Corin验证

会同时引入Policy迁移、Projection ABI、Graph拓扑和资产问题，无法隔离控制协议，拒绝。

### 用临时MonoBehaviour驱动

无法成为正式模块边界，后续还要删除并重写，拒绝。

### 自动编译Fixture

会把资产选择和字段编辑变成重操作触发器，不符合项目工作流，拒绝。

## Exit Boundary

该change只在以下实现边界全部完成后结束：

- 模块合同只有一份。
- 规则Compiler只使用模块专属Definition。
- Runtime state machine可由Frame Facts完整驱动。
- Fixture只调用正式Compiler与Runtime API。
- 现有动画资产、Projection和Runtime没有任何新引用。

视觉接入、角色迁移和旧Policy删除全部属于后续change。
