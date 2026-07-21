## Context

当前正式链路已经存在：

```text
BTSMTL Authoring
  -> Character Simulation Compiler
  -> Float32 / Fixed Program
  -> Session Source + Pipeline
  -> SimulationKernel
  -> WorldSolver
  -> Committer
  -> Presentation
```

问题不在主链缺失，而在主链内部的边界深度不一致：

- Runnable/Composite/StateMachine 已使用 portable control runtime，Timeline、GameplayEffect 与 Transaction ordering 却由 Float32/Fixed 各写一份。
- Session Composition 已允许选择 Solver/Source/Pipeline，Core identity 却仍知道具体 Authority Host 产品。
- selected Body 已成为 prediction contact 与 remote presentation 的共同事实，但 visual root 缺少只属于表现层的连续收敛。
- Program execution layout 已缓存一部分 topology，Tick 中仍反复构建可复用数据。
- Camera node、Fantasy endpoint、Editor 与 Build Tool 的对外合同清楚，内部实现却尚未按职责收口。

本 change 使用“保留正式外壳，替换内部 owner”的迁移方式。每一阶段先建立唯一新 owner，再迁移调用方，最后删除旧 owner；禁止新旧实现并存作为兼容方案。

## Goals

- Float32 与 Fixed 对相同 Semantic Operation 使用同一份业务控制算法。
- Numeric Target、Network Model、Host Product、WorldSolver 与 Presentation 的所有权互不泄漏。
- Remote visual 在不改变 selected Body/contact/event horizon 的情况下连续表现。
- 稳态 Tick 不重复构建 Program/roster 级数据，临时 workspace 有明确生命周期。
- Camera authoring node 能正式编译并通过现有 Presentation 边界输出。
- Fantasy endpoint、Tree/Timeline Editor 与 Network Test Tool 的内部模块可独立理解和修改。
- 完成后删除重复实现，保持一条编译、运行、构建和调试链路。

## Non-Goals

- 不追求把所有数值类型包装成一个万能 interface，也不把 Program/State 改成运行时切换 numeric backend。
- 不用 source generator 生成两份业务 runtime。
- 不用延迟远端 Body 的方式换取平滑。
- 不把 Fantasy Room、KCC 或 Editor Window 拆成大量公开 framework。
- 不做性能基准测试框架；本 change 只通过代码边界、编译产物和 diagnostics 关闭已识别的重复分配。

## Decision 1: Portable Control Module + Target Leaf Port

### 选择

保留两套明确的 Target ABI：

```text
Float32 Program/State/Numeric ABI
Fixed Program/State/Numeric ABI
```

在二者之上扩展现有 portable control 模式：

```text
Portable Timeline Control
  -> Target Timeline Port

Portable GameplayEffect Control
  -> Target GameplayEffect Port

Portable Pipeline Transaction Coordinator
  -> Target Transaction Port
```

portable 模块拥有与数值无关的业务顺序：Timeline segment/cycle/window/cue 生命周期、GameplayEffect apply/stack/period/expire/prediction bookkeeping、outer transaction 的 restore/evaluate/resolve/finalize/publish/commit 顺序。Target port 只提供 numeric calculation、curve sample、typed state access、Target-specific value storage 与 codec。

### 为什么不合并 Program/State

把全部状态泛型化会扩大 Snapshot、Hash、Compiler、Solver 和网络 ABI 的改动面，并让 Float32 使用 Fixed 的约束。当前业务只需要共享规则，不需要运行时交换数值格式。

### 为什么不用 source generator

生成代码仍会形成两份可执行业务算法，diff 和调试结果容易漂移；generator 还会增加编译工具本身的维护边界。portable control + leaf port 可以直接保证规则只有一个 owner。

### Tradeoff

- 收益：修一次生命周期规则即可覆盖 Float32/Fixed，后续新 Numeric Target 只实现 leaf port。
- 代价：需要设计更窄的 typed port，并重写现有 runtime 的访问方式；某些值类型会通过 generic specialization 传播到编译产物。
- 约束：不得用 `object`、reflection、字符串 dispatch 或 fallback target 消除类型差异。

## Decision 2: Shared Transaction Coordinator 不拥有 Numeric State

Pipeline Transaction 的阶段顺序、失败回滚和 output disposition 由 portable coordinator 负责；具体 working state、snapshot codec、motion/world request/result 仍由 Target transaction port 管理。

```text
Coordinator
  1. Ingress
  2. Schedule
  3. Optional Restore
   4. N x Step
      - 按 compiled phase order 执行全部 Step Pass
      - 唯一 Evaluate Actors 核心锚点
      - 唯一 ResolveBatch 核心锚点
      - 唯一 Finalize Actors 核心锚点
      - 附加 Step Pass 依照 descriptor 顺序和 Product 依赖执行
  5. Egress
  6. Atomic Publish
  7. Commit external outputs
```

Coordinator 不读取 Float32/Fixed payload，不拥有 WorldSolver。核心三个锚点必须按 Evaluate、ResolveBatch、Finalize 排列，但 Step phase 不是封闭枚举；Rollback History 这类附加 Pass 可以消费 Finalize Product，在 completed step 与 pipeline projection 冻结前更新自己的事务状态。Target port 只识别本 Target 的三个核心锚点，不得硬编码具体 Network Model 的附加 Pass。这样可以共享原子推进规则，又不强行统一数值状态或把 Rollback 身份泄漏进 neutral Core。

## Decision 3: ProgramExecutionServices 与 ActorWorkspace 分层

生命周期分为三类：

```text
ProgramExecutionServices
  immutable, 每 Program/Layout 构建一次

SessionExecutionWorkspace
  roster/schedule/transaction 级可复用容器

ActorExecutionWorkspace
  每 Actor 清空复用的 facts/trace/segments/GE scratch
```

Program services 缓存 topology、SourceMap index、Timeline curve lookup、GE descriptor/index 和 state access policy。Session/Actor workspace 只复用容量，不保留 Gameplay state。越过 outer transaction 的 Snapshot、published output、history 和 diagnostics payload 必须复制或冻结，不能引用下一 Tick 会清空的内存。

Timeline 单 segment 采样使用无集合 fast path；只有跨 segment/cycle 时才使用 bounded scratch。GameplayEffect 的稳定 descriptor/index 在 layout 构建，Tick 只处理当前实例。

正式实现中，Unity Float32/Fixed输入adapter复用各自的input value/request集合，typed input构造函数直接写最终排序数组；Program Evaluate Pass持有固定容量PendingEvaluation batch并在每Step reset。跨事务发布对象仍可创建独立只读快照，这些冻结分配不属于Tick scratch优化范围。

### Tradeoff

- 收益：减少角色数、replay 数增长时的 GC 和重复排序。
- 代价：workspace reset/freeze 边界需要严格审查；错误复用会污染下一 Tick 或 Snapshot。
- 约束：不宣称所有路径绝对零分配；错误报告、容量增长和真正持久输出仍可分配。

## Decision 4: Host Profile 由 Product 拥有

neutral Core 只保留通用 identity 组成：Program、ABI、Pipeline、Solver capability、Host product token。它不枚举 Unity/DotRecast 产品名称，也不提供具体 profile factory。

```text
ServerAuthoritative Model
  -> protocol / prediction-authority semantics

Unity Authority Product
  -> Unity worker host profile + manifest lowering

DotRecast Authority Product
  -> DotNet scene host profile + manifest lowering
```

Handshake 仍比较正式 identity，但 identity 的具体 Host product 部分由已选 Product adapter 提供。迁移时重建 manifest/hash，并删除旧 Core profile API；不读取旧 manifest。

### Tradeoff

- 收益：增加第三个 Authority backend 不修改 Core 或现有 Product。
- 代价：Product adapter 需要显式提供更多 launch/manifest 字段。
- 约束：客户端 prediction Solver 与 authority backend 可不同；兼容校验比较声明能力和模型合同，不要求 SolverId 相同。

## Decision 5: Selected Body 上的 Visual Pose Convergence

Remote Prediction Schedule 继续是 selected Body tick 的唯一 owner。World contact 与 reliable event horizon 继续使用该 stream。Presentation 增加一个只保存视觉姿态的 filter：

```text
Committed selected Body frames
  -> Remote Body interval
  -> Visual Pose Convergence
  -> Remote renderer/animation root
```

filter 可以：

- 在相邻 selected frame 之间按 PresentationFrame 插值。
- selected target 被新 authority 信息替换时，从当前 visual pose 有界收敛到新 target。
- 当前 outer transaction 产生零 Current step 时，继续朝已经提交的 target 收敛。
- HardRecovery/stream reset 时清空旧视觉速度和 target，并从显式新 anchor 重建。

filter 不可以：

- 从 raw authority buffer 自己选 tick。
- 维护独立 Body delay cursor 或延迟 reliable event。
- 将 visual pose 写回 WorldSolver、Prediction State 或 contact body。

收敛实现只采用一次带`maxSpeed`的`SmoothDamp/SmoothDampAngle`积分结果。不得在同一帧再用`MoveTowards`修改已采用姿态，否则保存的velocity会对应未采用的candidate，下一帧形成可见抖动。

### 为什么不采用 delayed playout

delayed playout 会让屏幕上的远端角色故意落后于用于本地碰撞的 selected Body，重新制造“看见的位置”和“撞到的位置”不同。当前业务更重视动作交互一致性，因此选择同 target 的视觉收敛。

### Tradeoff

- 收益：不破坏 contact 与事件闭环，改善低频样本的视觉连续性。
- 代价：大的 authority replacement 期间，视觉短时间不等于 canonical body；diagnostics 必须同时显示 target 与 visual error。

## Decision 6: Camera Node 编译闭环

现有 `RequestCameraStateNode`、`EmitCameraCueNode`、`SetCameraResponseNode`、`SetCameraTargetNode` 保留为 authoring 入口。Compiler 为每个节点注册唯一 emitter，生成 versioned Camera operation 和稳定 Source Map；Target leaf 将其转换为现有强类型 PresentationCommand。

Camera operation 不访问 Unity Camera/Cinemachine，也不进入 Simulation state。缺失字段、未知 operation 或 Target 未实现时在 build/composition 明确失败，不在 runtime 跳过节点。

Compiler只把真实连接到Property Edge的输出端口纳入Program Value Graph；未连接输出不要求Numeric Target伪造运行语义。已连接的泛型输入端口由上游Value Edge与operation port contract推导类型，只有未连接输入才读取authoring本地值并编译为constant input binding。端口不存在、已连接类型不满足constraint或未连接输入没有可编码值时仍必须在build失败。

### Tradeoff

- 收益：已有 authoring 能力变成真实 Program 能力，不需要删除节点或让作者绕过 Graph。
- 代价：operation-set version、ProgramHash 与生成资产会变化。

## Decision 7: Fantasy Endpoint 保持一个外壳、拆内部模块

保留现有 endpoint interface 和唯一 network path，内部拆为：

- `ControlSessionModule`：connect/register/join/heartbeat/leave。
- `DatagramChannelModule`：ticket、handshake、command/snapshot ingress/egress。
- `CheckpointReconstructionModule`：delta/full checkpoint、baseline 和重建。
- `PredictionEvidenceModule`：ack、observation、liveness、metrics。
- `ConnectionCoordinator`：唯一状态机、failure、dispose owner。

transport callback 仍只验证外壳并写 Source queue。子模块不能独立切换 endpoint state、启动 simulation 或释放共享 session。

### Tradeoff

- 收益：协议、checkpoint 和 lifecycle 修改各自在局部完成，仍只有一个 endpoint 行为。
- 代价：需要显式定义 module event/result，而不是共享大类 mutable fields。

## Decision 8: Editor 保留 Surface、拆内部 owner

### Timeline

- interaction state：selection、drag、move、resize、capture；selection只以`IReadOnlyList`暴露，外部只能调用注册与选择命令。
- frame geometry：time/frame conversion、clip rect、overlap、hit test。
- rendering：track、clip、playhead、preview/live overlay；每次绘制显式接收frame range、viewport、playhead或overlay输入，不持有整个`TimelineFieldView`。
- window/session adapter：Authoring Preview 与 Live Debug binding。

interaction只依赖`ITimelineInteractionHost`提供的geometry、visible range、Undo数据入口、selection呈现、edit frame与preview refresh回调。`TimelineFieldView`是这些窄端口的UI适配器，不再成为interaction/rendering共享可变状态对象。

### Tree

- graph mutation service：link/delete/paste/condition graph cleanup/Undo。
- tree view：node/edge visual 与 selection forwarding。
- inspector content：Node/Edge/Graph settings。
- data catalog：Input/Blackboard filter 和 editor-only view state。
- window navigation：Graph locator、tab/page state、domain reload restore。
- runtime overlay：共享 debug session 的 window-local binding。

公开 Window/View 仍是原入口，serialized identity 和 UI 行为不变。提取的是 internal implementation，不创建另一套 editor framework。

### Tradeoff

- 收益：每项 authoring 行为有明确 owner，后续修改不会跨越千行类。
- 代价：迁移期间容易破坏 selection、Undo 或 domain reload 状态，必须逐个保持现有合同。

## Decision 9: 唯一 Network Test Product Build Workflow

建立 Editor-only workflow：

```text
NetworkTestProductBuildWorkflow
  -> INetworkTestProductBuildAdapter
     - UnityAuthority
     - DotRecastAuthority
     - DeterministicRollback
```

workflow 统一：

- Build 与 Run 分离。
- dotnet/msbuild 调用参数和 build-server shutdown。
- 临时目录使用同一 Network output parent 下的固定短 identity，保留 Windows Player 深层文件的路径预算，构建后原子替换正式输出。
- exact file closure、product identity、manifest 和场景清单校验。
- 同种 Product 覆盖自己的输出，不同 Product 使用不同目录；manifest 冲突按 Product root 下的完整路径判断，相同文件名不构成跨 Product 冲突。

`NetworkTestExternalProcessExecutor`唯一拥有外部进程与dotnet shutdown，`ServerProductBuildManifestUtility`唯一拥有服务端manifest/hash，`NetworkTestProductAdapterUtility`只提供Program identity、资产加载和Fantasy产品校验。三种adapter不得调用另一具体adapter的静态helper；产品注册表只位于Editor composition root。

adapter 只提供产品差异：Player scenes/assets、server project 或 no-server、output root、manifest fields、launch script。Run 只消费已经完成并通过 exact manifest 校验的 build，不隐式重新 Build。

### Tradeoff

- 收益：三个产品的构建修复只改一处，产品差异仍是显式模块。
- 代价：adapter contract 需要覆盖 Unity Player、普通 .NET Server 和无 Fantasy Server 三种形态。

## Serial Migration Order

1. 确认依赖 change 完成且 Editor/Runtime 编译绿色，冻结重复实现与 identity inventory。
2. 建立 portable Timeline、GameplayEffect 与 Transaction ports，不切换调用方。
3. 串行迁移 Float32，再迁移 Fixed；每个领域完成后立即删除旧控制实现。
4. 安装 execution services/workspace 生命周期并清理 Tick 重建。
5. 迁移 Host Profile 所有权并重建正式 manifest/hash。
6. 拆 Fantasy endpoint 内部模块，保持 endpoint interface 不变。
7. 安装 Remote visual pose convergence。
8. 补齐 Camera node Compiler/Program/Presentation 闭环。
9. 建立统一 Network Test Build Workflow 并迁移三个 Product adapters。
10. 拆 Timeline Editor，再拆 Tree Editor；保持公共 surface 与序列化状态。
11. 删除所有旧 helper、重复 runtime 和过时文档，完成程序集编译与 OpenSpec strict validation。

这个顺序避免同时改动 Numeric runtime、Network identity 和 Editor surface。任何阶段都只有一条已完成的正式路径；不能用 feature flag 长期保留旧实现。

## Stop Conditions

- shared control 必须通过 `object`、reflection 或字符串访问 Target state 才能成立。
- 迁移要求同时保留 Float32/Fixed 两份业务 control 作为 fallback。
- Host manifest 无法无损重建且只能继续读取旧 Core profile。
- Remote 平滑只能通过第二 Body tick/delay cursor 实现。
- Camera node 需要绕过 Program/PresentationCommand 直接访问 Unity Camera。
- Editor 拆分必须改变 asset identity、Undo 数据或创建第二写入口。

发生以上情况时停止实施并说明业务取舍，不继续硬做。

## Risks

- shared runtime 的 generic specialization 可能增加编译时间，但不应增加运行时 dispatch。
- workspace 复用最危险的是持久输出引用可变内存，因此 freeze/copy 边界必须先定义再优化。
- Host identity 迁移会让旧 build 产物失效，这是有意的干净迁移；Build Tool 必须明确拒绝混用。
- visual convergence 参数过软会形成拖尾，过硬仍会顿挫；参数属于正式 Presentation Profile，不进入 Network Model。
- Editor 内部拆分文件较多，但不能借机重做 UI 或加入不相关功能。
