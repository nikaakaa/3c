# Design: 角色 Presentation Runtime 内部模块边界

## Context

正式表现链路已经成立：

```text
Simulation Commit
  -> ICharacterPresentationRuntime
  -> CharacterSimulationPresentationRuntime
  -> AnimationPlaybackRuntime / Camera / VisualRoot
  -> Animancer / CameraRig / Transform
```

问题不在链路方向，而在一个类内部和 Network adapter之间的责任分布。当前 `CharacterSimulationPresentationRuntime` 同时是 facade、Body clock、visual filter、Transform presenter、Camera runtime和 Animation coordinator；ServerAuthoritative adapter又拥有另一份 remote visual filter。继续在同一类增加功能会让本地卡顿、远端顿挫、动画 readiness和相机问题无法独立定位，也会让其它 Network Model重复实现视觉策略。

本设计只重构 Unity Presentation，不改变 Gameplay Program或网络同步语义。

## Goals

- `CharacterSimulationPresentationRuntime` 继续是唯一公开 runtime边界。
- Body、Animation、Camera各自拥有明确状态和输入输出。
- Body采样策略显式配置，不与相机、Network Model或Actor名称绑定。
- selected remote visual convergence回到通用 Presentation。
- 外部调用方只提交 committed body/command并调用一次 `Present`。
- local、simulated remote、observed remote复用同一协调器，不复制处理链。
- Reset、Dispose和 diagnostics顺序保持唯一且可审查。

## Non-Goals

- 不把 Presentation做成动态插件总线。
- 不建立 local presenter、remote presenter、rollback presenter三套类层级。
- 不让 Body模块读取 Source、History、packet或 WorldSolver。
- 不让 Camera模块读取逻辑 Transform或建立第二份body history。
- 不让 Animation模块参与逻辑 producer仲裁。
- 不实现缺失的 Cue/VFX/Audio最终消费者。

## Decision 1: 一个公开协调器，三个内部责任块

最终结构：

```text
CharacterSimulationPresentationRuntime
  |- CharacterBodyPresentationRuntime
  |- CharacterAnimationPlaybackRuntime
  |- CharacterCameraPresentationRuntime? 仅显式本地相机组合创建
  `- existing presentation signal routing
```

`CharacterSimulationPresentationRuntime` 负责：

- 校验 Actor、Program/Projection和producer identity。
- 接收 committed body interval与 PresentationCommand。
- 按 command kind路由到 Animation、Camera或既有信号通道。
- 每个 PresentationFrame只执行一次固定顺序。
- 统一 Reset、Dispose和错误传播。

它不再保存 Body history、Camera request容器或 Camera resolver。

### Tradeoff

- 收益：外部合同不扩散，内部状态可以独立审查，remote actor不再分配 Camera容器。
- 代价：增加两个内部对象和少量构造装配，但这些对象与角色runtime同寿命，不增加每帧动态选择。

## Decision 2: Body Runtime唯一拥有视觉姿态

定义正式 `CharacterPresentationBodyInterval`：

```text
ActorId
PreviousTick / PreviousBody
CurrentTick / CurrentBody
StreamUpdateKind: Append | Reset
```

`CharacterBodyPresentationRuntime` 输入只有已经由 Committer或具体Model Egress提交的 canonical body interval，以及 `GameplayPresentationFrameContext`。输出为只读 `CharacterBodyPresentationFrame`：

```text
PreviousTick / CurrentTick
SampleAlpha
VisiblePosition / VisibleRotation
AnimationSampleTick / AnimationSampleAlpha
ResetReason / VisualError diagnostics
```

它唯一拥有：

- committed interval历史。
- presentation delta驱动的表现时钟。
- 同tick替换、tick回退与显式stream reset识别。
- 本地预测分支替换后的visual recovery。
- selected stream的区间插值与有界位置/朝向收敛。
- visual bind offset和最终VisualRoot写入。

Network adapter不得创建 `WorldBodyState visualBody`，也不得调用 `SmoothDamp`后再把结果伪装成canonical body输入。

Committed branch replacement 必须先在当前 presentation sample tick 对旧分支与替换分支分别采样。Recovery offset只包含这两个同一时刻姿态之间的差；相邻PresentationFrame之间本来应发生的正常位移不得进入recovery。新分支提交时只移除replacement起点及之后的旧样本，起点之前仍有效的历史继续保留。已有recovery与新的真实分支差连续合成，表现帧先应用当前offset，再为后续帧衰减，因此连续replay不会反复把角色冻结在上一帧位置。

### Tradeoff

- 收益：visual pose真正只有一个owner；本地和远端顿挫都能从同一body diagnostics解释；后续DotRecast或其它网络模型不需复制filter。
- 代价：ServerAuthoritative model日志不再直接拥有visual pose细节，必须读取Presentation diagnostics；rollback replacement还需要在提交边界对新旧分支各做一次同tick采样，这是有界的Presentation成本。

## Decision 3: Body策略在创建时显式锁定

Body策略只有两个正式值：

### CommittedStream

适用于本地owner和会在当前进程完整模拟的Actor，包括Deterministic Rollback的两名simulated actor。它按提交tick历史和presentation delta推进，不依赖是否拥有相机。

### SelectedStream

适用于只消费外部selected body interval的observed actor。它接受Egress明确提交的stream reset，并使用正式remote visual profile执行区间插值与有界收敛。它不自行读取原始authority buffer，也不选择tick。

策略在runtime创建后不可切换。缺少所需profile或提交了不符合策略的stream update时直接失败。

### Tradeoff

- 收益：解决`ownsCamera == local clock`的隐式耦合；Actor表现语义可读且可复用。
- 代价：Factory调用方必须明确知道自己提交的是committed stream还是selected stream，装配参数更多但含义真实。

## Decision 4: Remote visual profile属于Character Presentation

现有 `ServerAuthoritativeRemotePresentationProfile` 的四项收敛参数是视觉手感参数：

- position smooth time
- max position speed
- yaw smooth time
- max yaw speed

它们不决定packet、ack、prediction、authority tick或correction，因此不属于Network Model。正式类型迁移为 `CharacterRemotePresentationProfile`，位于Character Presentation模块；Corin资产迁入角色Presentation配置目录并保留原`.meta` identity。

ServerAuthoritative场景仍显式引用该唯一资产，但只把它交给Presentation Factory。不得保留旧类型、旧namespace、MovedFrom、兼容wrapper或两个profile。

### Tradeoff

- 收益：参数可由角色表现作者调节，并可被不同observed stream复用。
- 代价：Network测试场景仍需一个Character表现引用；这是角色外观装配，不是Network policy泄漏。

## Decision 5: Camera是可选能力，不是Actor时钟标志

`CharacterCameraPresentationRuntime` 只在Factory收到完整camera binding时创建。它拥有：

- Camera State/Response/Target实例容器。
- pending Camera Cue。
- State/Response/Modifier resolver。
- target binding resolver。
- look input与look input id。
- follow/aim bind offset。
- rig adapter应用。

每帧输入是同一个 `CharacterBodyPresentationFrame` 的visible pose。默认follow/aim点不得重新读取logic body或VisualRoot Transform。Camera模块不保存Body history。

没有camera binding的simulated/observed actor不创建任何Camera容器；收到Camera command直接报告配置错误。

### Tradeoff

- 收益：camera ownership与Body时钟完全解耦，remote actor内存和心智负担更小。
- 代价：Camera构造校验从主类迁到内部模块，必须保持同样的fail-fast错误质量。

## Decision 6: Animation启动策略由协调器拥有

`CharacterAnimationPlaybackRuntime` 保持现有职责，不迁入Body或Camera。Factory显式选择：

- `RequireCommittedSelection`：用于本地owner和完整simulated actor。required layer没有逻辑selection时保持现有错误。
- `AwaitCommittedSelection`：用于observed actor。可靠selection尚未到selected body horizon前，Body可继续表现；第一份合法selection到达后，动画按现有PendingFirstSample/Current/Outgoing/Retired语义推进。

外部不再读取 `HasRequiredAnimationOutput`，不再调用 `PresentBody`。`Present` 内部固定执行：

```text
Begin diagnostics frame
  -> BodyRuntime.ResolveAndApply
  -> AnimationPlaybackRuntime.Present（按启动策略）
  -> CameraRuntime.Present（若存在）
  -> frame signal cleanup
```

Animation sample tick/alpha来自Body frame同一表现时钟，不能由Network site另传一套alpha。

### Tradeoff

- 收益：外部不再知道动画内部状态，Body和动画使用同一表现时钟，避免调用顺序分裂。
- 代价：observed actor允许“身体已移动但可靠动画selection尚未到达”的正式短暂状态；这比伪造Idle或由Network adapter猜动画更诚实。

## Decision 7: Factory是唯一装配入口

删除六个公开构造重载与调用方直接`new CharacterSimulationPresentationRuntime`。Factory提供业务明确的入口：

```text
CreateLocalOwner
CreateSimulatedActor
CreateObservedActor
```

三者只选择内部module组合，最终都返回同一 `CharacterPresentationRuntimeBinding` 和同一 `ICharacterPresentationRuntime`。

- `CreateLocalOwner`：CommittedStream + RequireCommittedSelection + Camera。
- `CreateSimulatedActor`：CommittedStream + RequireCommittedSelection + no Camera。
- `CreateObservedActor`：SelectedStream + AwaitCommittedSelection + no Camera + RemoteProfile。

Factory不检查具体Network Model类型，不使用反射、字符串约定或fallback。

### Tradeoff

- 收益：构造不变量集中，调用方无法组成半有效runtime。
- 代价：以后新增真正不同的Body source语义时必须扩展正式Factory合同，而不能临时拼参数；这是刻意约束。

## Decision 8: ServerAuthoritative只保留网络职责

迁移后 `ServerAuthoritativeRemotePresentationRegistration` 只拥有：

- selected Body与reliable event的模型输出队列。
- selected Body horizon和可靠事件发布顺序。
- remote visual GameObject与runtime registration生命周期。
- model-level queue/horizon diagnostics。

它把 `RemotePresentationBatch.BodySamples` 与 `ResetBodyStream`转换为通用 `CharacterPresentationBodyInterval`提交给runtime。它不保存visual velocity、visual yaw、smooth time或调用`PresentBody`。

Presentation diagnostics拥有target pose、visual pose、error和reset reason；Model diagnostics只保留selected tick、queue和reliable horizon。

## Error Semantics

以下情况必须直接失败：

- Actor/Program/Projection identity不一致。
- Body interval Actor不匹配、tick顺序非法或非Reset回退。
- SelectedStream缺少正式remote profile。
- CommittedStream收到仅selected stream允许的reset语义。
- 无Camera组合收到Camera command。
- RequireCommittedSelection组合缺少required animation selection。
- producer不在Projection或kind不匹配。

不得增加默认profile、默认Body策略、Transform搜索、Idle动画fallback或Network Model类型判断。

## Lifecycle

创建顺序：

```text
Projection validation
  -> BodyRuntime
  -> AnimationPlaybackRuntime
  -> optional CameraRuntime
  -> coordinator published
```

销毁顺序：

```text
stop external registration
  -> coordinator Reset
  -> CameraRuntime Dispose
  -> AnimationPlaybackRuntime Dispose
  -> BodyRuntime Dispose
  -> diagnostics target dispose
  -> visual object destroy
```

任一模块构造失败时由Factory释放已创建模块，不发布半成品binding。

## Alternatives Considered

### 只把1052行拆成partial或多个文件

优点是改动最小；缺点是`ownsCamera`、network visual filter和外部Present分支全部保留，只改善文件长度，不改善业务边界。拒绝。

### 分成LocalPresentationRuntime与RemotePresentationRuntime

优点是构造直观；缺点是会复制producer路由、Animation lifecycle、Projection校验、Reset/Dispose和diagnostics，Deterministic Rollback simulated actor也无法自然归类。拒绝。

### 为每个Presentation channel建立动态插件总线

优点是理论扩展性高；缺点是当前只有固定Body/Animation/Camera职责，会引入注册顺序、动态查找和错误延后。当前业务不需要。拒绝。

### 保留Network Model内的remote visual filter

优点是ServerAuthoritative代码局部自包含；缺点是其它模型重复视觉逻辑，且Network拥有visual pose，与当前spec相反。拒绝。

## Implementation Order

1. 固化现有调用点、Body策略、Camera资源和remote profile资产清单。
2. 定义Body interval/frame、策略和Factory创建合同。
3. 提取Body Runtime并迁移local clock、replacement、recovery和visual root应用。
4. 把selected stream interpolation/convergence迁入Body Runtime。
5. 提取Camera Runtime并保持现有resolver和binding行为。
6. 收窄协调器并统一每帧调用顺序。
7. 收敛Factory并迁移local、rollback、server-authoritative调用点。
8. 迁移remote profile资产与类型，删除Network visual filter和旧API。
9. 清理引用、编译相关程序集、更新架构文档并严格校验OpenSpec。

## Stop Conditions

实施中出现以下情况必须停止并说明tradeoff：

- 现有remote profile资产无法在保留`.meta` identity的前提下安全迁移。
- selected Body Egress没有足够信息表达显式stream reset，必须读取Network内部history才能重建。
- Animation sampling必须读取Network adapter私有时钟才能维持现有语义。
- 需要修改Program ABI、Projection schema或新增第二条Presentation output路径才能完成迁移。
