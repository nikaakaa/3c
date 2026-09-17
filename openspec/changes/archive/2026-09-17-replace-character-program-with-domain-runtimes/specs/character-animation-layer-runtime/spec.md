## MODIFIED Requirements

### Requirement: 持续Pose与有限Action控制边界必须分离

持续 Locomotion MUST由 committed Body／Intent 形成 Fact，再由原生 PoseState 选择 state-local source。有限 Action MUST通过有实例和 generation 的播放命令进入 Slot。图、资源、Rig 与动作绑定 MUST由各自正式配置和实例准备提供，不编进整体角色 Program／Projection；旧 BaseLocomotion channel、PoseSlot、LayerId 和按 Profile 顺序猜测配置 MUST保持删除。

#### Scenario: Locomotion持续输出

- **WHEN** 当前Body Fact合法
- **THEN** PoseStateMachine MUST拥有明确active State与Pending、Ready或Invalid source状态
- **AND** 系统 MUST不等待BaseLocomotion Selection Input

#### Scenario: FullBodyAction为空

- **WHEN** FullBodyAction channel没有活动playback
- **THEN** AnimationSlot MUST透传同帧Source Pose
- **AND** MUST不创建fallback clip、默认Idle或第二条Locomotion路径

#### Scenario: Action command引用未知binding

- **WHEN** command的producer、channel或Slot binding不能精确匹配动画运行绑定
- **THEN** 动作接口与动画运行绑定校验 MUST失败
- **AND** command MUST不进入Lifecycle或原生Pose图



### Requirement: 基础Pose必须由正式state-local source输出

Base Pose、Idle、Move、Start、Stop、Turn与可选Motion Matching MUST来自Pose Graph中PoseStateMachine选择的ClipPlayer、BlendSpacePlayer或SelectedPosePlayer provider。角色Gameplay、Timeline与Action Lifecycle MUST不提供持续BaseLocomotion producer。Required source缺失或Clip Phase relation无效时 MUST报告typed Pending或Invalid，不得回退旧Sequence、默认Idle、bind pose或历史sample。

#### Scenario: Clip binding失效

- **WHEN** active PoseState的Clip Binding与动画运行绑定 identity不一致
- **THEN** provider MUST发布Invalid并阻止正式Pose提交
- **AND** MUST不继续使用旧Sequence或上一帧source



### Requirement: 动画帧必须按固定职责顺序执行

每个PresentationFrame MUST按固定顺序读取committed Body/Intent与本次typed动画输入、构造Fact、求值PoseStateMachine、提交target provider demand、解析readiness、采样state-local source、消费有限Action frame、执行Transition Routing与AnimationSlot、执行Local Pose composition与Virtual Bone派生、显式转换到Component Pose、执行Component Pose控制、让FootPlacement与PoseBone目标源发布typed Goal Contribution、由唯一Goal Assembler形成一个Goal Set、由唯一FullBodyIK求解Component Pose、显式转回Local Pose，最后发布FinalAnimationPoseFrame。Action visual sampler MUST只生成有限Action sample；PoseState provider MUST只处理其state-local source。任一阶段 MUST不重新仲裁其它阶段的选择或写回Gameplay。

#### Scenario: 攻击期间角色速度归零

- **WHEN** FullBodyAction Slot仍有完整权重但Body速度已经归零
- **THEN** PoseStateMachine MUST继续更新到Stop或Idle目标
- **AND** Action结束时Slot MUST回到当时的当前Source Pose



### Requirement: PoseState source必须按provider demand和state relevance管理

PoseStateMachine MUST只向相关State的显式source plan提交固定容量demand，并以实例绑定的source handle、PlayerNodeId、SourceGeneration、continuity identity和frame lease接收sample。Pending target MUST不启动transition，Ready target MAY进入Routing，Invalid MUST阻止正式publication。State离开active后只要transition仍需要其Pose，state relevance MUST保持source；release完成后 MUST精确清理。Pose source MUST不创建作者Source字符串、Gameplay PlaybackId或Action retention。

#### Scenario: Start State切向Locomotion

- **WHEN** Locomotion target Ready且transition仍共同显示Start
- **THEN** Start与Locomotion source MUST同时保持relevant
- **AND** transition完成后 MUST只释放Start source



### Requirement: 每类连续性必须只有一个明确owner

ClipPlayer、BlendSpacePlayer与SelectedPosePlayer MUST只管理自身source sample和discontinuity；PoseStateMachine Transition MUST拥有State到State的clock、blend和release；AnimationSlot MUST拥有Source Pose与Action source之间的handoff；显式BlendStack MUST只拥有自身连接source的entry、Stored Pose、dense per-bone blend和retirement；Inertialization MUST独占局部completed Pose history、residual与rebase。Runtime MUST不为AnimationChannel、Graph branch或Output自动创建隐藏Stack、StateMachine、Slot或全局Inertialization。

#### Scenario: PoseState连续切换

- **WHEN** A到B transition尚未结束又接受合法B到C切换
- **THEN** PoseState 正式transition policy MUST处理现有Pose历史
- **AND** MUST不把历史注入无关BlendStack

#### Scenario: Action连续打断

- **WHEN** Slot从Attack切换到Dodge
- **THEN** Slot MUST按node-local route处理handoff
- **AND** PoseStateMachine MUST不保存Action transition



### Requirement: Finite与Cyclic source时间必须保持明确拓扑

Runtime MUST支持Cyclic与Finite source之间的显式同组映射。Cyclic source MAY按duration回绕并维持展开cycle；Finite source MUST不回绕，target occurrence MUST单调前进。首次存在多个相同有向pair occurrence时 MUST按与raw target time的最小距离选择，并以稳定authoring identity破同；relation存活期间 MUST保持occurrence连续性。source正式release时 MUST以target最后effective/raw time建立continuation anchor，之后按raw delta连续推进。

#### Scenario: Run进入Finite Stop

- **WHEN** Run到Stop Transition启用同组同步
- **THEN** Runtime MUST选择Stop中最近的兼容pair occurrence
- **AND** 后续共同可见帧 MUST沿Stop有限序列前进

#### Scenario: Finite coverage耗尽

- **WHEN** relation要求Finite target越过资源Phase coverage
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕或静默解除同步



### Requirement: Source backend必须只负责采样和物理资源释放

Animancer source backend MUST只按完整Action playback或Presentation Pose source identity创建、复用和释放source playable，采样producer内部clip membership或state-local source，并把capture job安装到同一PlayableGraph。它 MUST不拥有Gameplay/PoseState仲裁、跨source transition weight、AnimationSlot、Inertialization、Pose composition、IK或Final writer。每个表现帧 MUST只执行一次正式原生Pose图和一次PlayableGraph Evaluate。

#### Scenario: Transition同时采样两侧source

- **WHEN** State或Slot transition要求两个source共同可见
- **THEN** backend MUST分别提供两个source capture
- **AND** transition weight MUST只由对应owner计算



### Requirement: Runtime、Preview和Live Debug必须使用同一事实源

正式Runtime、Action Timeline Preview、Pose Graph Fact Preview、MM Query Fixture和Live Debug MUST复用匹配revision的动画运行绑定、source backend、Routing Plan、原生Pose图与completion语义。Preview入口 MUST分别只提交Action command、Presentation Fact或state-local query fixture。Diagnostics MUST按Action playback identity或Provider/Player/Source/generation显示各自生命周期、effective sample、transition、release和Pose contribution；不得从Animancer weight或Animator骨骼反推第二份事实。

#### Scenario: Projection变为Stale

- **WHEN** authoring revision变化而动画运行绑定尚未显式Build
- **THEN** Preview与Runtime preparation MUST停止
- **AND** MUST不创建临时Plan、旧动画运行绑定 fallback或独立PlayableGraph

#### Scenario: Presentation binding变为Stale

- **WHEN** authoring revision变化而Presentation binding尚未显式Preparation
- **THEN** Preview与Runtime preparation MUST停止
- **AND** MUST不创建临时Plan、旧Presentation binding fallback或独立PlayableGraph



### Requirement: Locomotion Phase映射必须编入source-local计划

Locomotion Phase MUST继续使用来源明确的 forward／inverse 资源数据与可达 relation。每个 relation MUST保留 TransitionId、固定 leader、秒域 coverage 和有效性身份；实例绑定建立 source-local 关联，运行按 relation／transition／generation 和 continuation 计算有效采样时间。系统 MUST不因取消图编译退回 normalized time、旧 Marker 或逐帧搜索／重建关系；资源数据预处理独立于 Pose 图执行。

#### Scenario: RunLoop接任MovingTurn

- **WHEN** MovingTurn到RunLoop relation具有合法Phase计划
- **THEN** RunLoop Player MUST按Phase inverse得到effective time并采样Pose与Foot Feature
- **AND** MovingTurn与RunLoop各自raw clock MUST保持不变



### Requirement: Locomotion Phase relation必须服从Transition generation与Player continuation

Phase 关联准备 MUST按既有规则固定 leader：不同 clock authority 时 CommittedMovement 优先，同 authority 时 outgoing 优先；候选覆盖完整可见混合窗口，合法备选按正式规则选择，两侧不足必须明确拒绝关系。一个 generation 内不得按权重或时间动态更换 leader。转换替换、反向 edge、正常 release、AlwaysResetOnEntry、分支或图替换、Reset 与 Dispose MUST保持原独立 relation 和 continuation 清理规则。

#### Scenario: 同authority的Turn进入RunLoop

- **WHEN** MovingTurn与RunLoop都使用CommittedMovement且MovingTurn coverage覆盖完整Blend窗口
- **THEN** 关联准备 MUST把outgoing MovingTurn固定为该edge relation的leader
- **AND** Runtime MUST不因RunLoop weight超过MovingTurn而换leader

#### Scenario: Transition在Blend中被替换

- **WHEN** 当前relation generation尚未完成时更高优先级Transition替换目标State
- **THEN** Runtime MUST按旧edge release规则关闭旧generation，再为新TransitionGeneration建立新relation
- **AND** MUST不复用旧follower cycle、effective anchor或relation cursor
