# Change: 增加有限Action动画统一作者工作面

## Why

`refactor-animation-control-boundaries`会把动画运行职责拆成两条正式链：持续Locomotion由Presentation Fact与PoseStateMachine在表现帧选择和采样；有限Action继续由Gameplay Action Timeline拥有Action时间、Window、Motion、Warp、Cue和生命周期，并把committed raw visual sample交给表现层的Action Playback、AnimationSlot和Pose Graph。最终AnimationClip采样、混合、惯性化和骨骼Pose都属于Presentation Tick，不追求帧同步确定性。

完成这项运行重构后，作者仍需分别打开ActionProfile、Gameplay Graph、Timeline、Animation Presentation Profile、Pose Graph和Runtime Debug才能理解一个攻击。已有能力已经分配给正确owner，但缺少像UE Montage Editor那样把一个有限Action的动画、时间内容、Slot消费和运行状态放在同一工作面中的产品闭环。

直接新增Montage资产、Section运行时或自由播放时钟会重新复制Action Timeline和Animation Playback真相，也会把已经拆开的Gameplay时间与Presentation Pose混回一起。本change只增加Editor-only统一作者工作面，不新增运行时模型、不修改Semantic IR或Numeric Target Program、不改变每Fixed Tick提交raw visual sample的现有合同。

## What Changes

- 新增Character Editor所有的`Action Animation Workspace`：
  - 从精确Character Definition、ActionProfile、Action Context入口、有限Action Timeline、Action producer binding、AnimationSlot consumer和Runtime Debug binding建立typed session。
  - 主时间轴复用正式Timeline Editor Core显示和编辑Animation Track、TreeClip Window、Motion Curve、Motion Warp、Cue、Marker和typed Curve。
  - Identity、Gameplay、Animation、Slot、Blend、References、Preview和Live面板只编辑或读取对应正式owner。
- 建立唯一跨owner mutation与导航：
  - Action admission、Tags、Block/Cancel、Target和退出语义继续由ActionProfile与Gameplay Graph拥有。
  - Animation Clip、Marker和Clip Curve继续由有限Action AnimationTrack拥有。
  - Window、Motion、Warp和Cue继续由Action Timeline拥有。
  - producer resource、Rig和Analysis binding继续由Animation Presentation Profile拥有。
  - Slot topology和Blend Policy继续由Pose Graph拥有。
  - Workspace不保存上述字段的副本，也不创建第二Undo或第二序列化资产。
- 对齐拆分后的时钟口径：
  - Workspace把`Action Logic Time`显示为Simulation提交的committed raw visual sample来源。
  - Workspace把`Projected Presentation Time`显示为表现帧在相邻raw sample之间得到的视觉时间。
  - Workspace把`Marker Effective Time`显示为Marker Sync对视觉采样的表现修正。
  - 三种时间必须分栏展示，不得以一个`Montage Position`字段混为同一权威。
- 增加正式Preview入口：
  - Preview复用重构后唯一Animation Preview Runtime、Action Playback fixture、AnimationSlot、Transition Routing和完整Pose Plan。
  - Preview可以使用表现Tick播放动画，但不得创建Simulation Session、推进Gameplay Timeline或生成Gameplay Window/Motion真相。
  - 需要查看Gameplay时间内容时，Timeline Preview继续使用其正式Action fixture和明确目标。
- 增加统一Live Debug：
  - 从唯一RuntimeDebugSession显示ActionInstance、committed Timeline sample、projected presentation sample、Marker effective sample、Playback lifecycle、Slot route、Blend/Inertialization和Final Pose贡献。
  - Live模式只读，不重新执行Gameplay Graph、Timeline或Pose Graph。
- 保持Numeric Target边界：
  - Workspace作者数据与Preview不保存Float32或Fixed数值状态。
  - Float32与Fixed Session可以通过相同Presentation Contract和Projection进入同一Workspace Live视图。
  - Numeric Target只作为当前运行Session或显式Build目标的只读身份显示；Workspace不提供活动Session热切换。
- Build、分析和artifact生成只能由已有明确按钮触发。selection、mutation、窗口恢复、asset import、Preview和Live Debug不得自动Build或自动选中生成资产。
- 本change不增加Montage Section、Jump、Seek、Pause、Resume、SetRate或新的Action playback command；不迁移Corin或其它角色资产。

## Impact

- Affected specs:
  - `character-action-animation-authoring-workspace`（新增）
  - `character-action-authoring-closure`
- Affected active changes:
  - `refactor-animation-control-boundaries`必须先完成运行代码合同与旧路径清理，但Corin资产任务和change归档必须等待本工作区完成。本change直接读取其最终Action Playback、AnimationSlot、三层时间、Preview和diagnostics合同。
  - `refactor-pose-graph-to-btsmtl-authoring-domain`先提供共享Editor Shell、Capability Catalog、Document v3 typed mutation和owner解析；本change只提供Action业务编排。
  - Blend Space与Motion Matching后续change只影响持续Pose source，不进入有限Action Workspace。
  - `refactor-agent-authoring-to-synced-json-document`提供Document package和事务基础，不增加第二mutation入口。
- Affected code:
  - Character Editor Action Animation Workspace
  - typed open request、session context、owner resolver与mutation router
  - Timeline Editor Core嵌入adapter
  - ActionProfile、Gameplay Graph、Timeline、Presentation Profile和Pose Graph导航入口
  - Animation Preview adapter
  - RuntimeDebugSession只读聚合adapter

## Business Tradeoffs

### 方案一：新增独立Montage资产和运行时

- 优点：可以直接复制UE Montage的资源、Section和自由播放接口。
- 代价：与现有Action Timeline、Action Playback和AnimationSlot形成第二份时间、动画和生命周期真相；作者和Runtime都需要决定到底相信哪条路径。

### 方案二：保持当前分散编辑入口

- 优点：不增加Editor模块，运行和作者数据都无需变化。
- 代价：一个攻击需要跨多个窗口理解，正确的模块边界变成作者额外心智负担；作品展示时难以说明完整动画链路。

### 方案三：增加不拥有数据的统一作者工作面

- 优点：作者体验接近Montage Editor，同时保留现有Action Timeline、Action Playback、Slot和Pose Graph边界；不影响Float32/Fixed、回滚和表现Tick采样。
- 代价：Editor必须实现严格typed context、mutation路由和revision对账，不能靠复制字段简化UI。

本change采用方案三。

## Dependencies And Sequencing

- `refactor-animation-control-boundaries`必须先完成Action Playback、AnimationSlot、三层时间、Preview、diagnostics和旧运行路径清理；不要求先执行其Corin资产迁移，也不要求先归档整个change。
- `refactor-pose-graph-to-btsmtl-authoring-domain`必须先安装共享Editor Shell、Capability Catalog、Document v3 typed mutation和统一owner解析合同。
- 本change随后安装通用Action Workspace，只组合共享UI与既有模块adapter，不新增临时Action Playback adapter、第二份GraphView或数据副本。
- 本change完成后，`refactor-animation-control-boundaries`与Pose Graph重构才能执行一次Corin Document v3资产迁移；工作区不得在迁移后再反向改变schema或owner关系。
- 本change不自行迁移任何角色资产。唯一串行关系见`openspec/character-pipeline-serial-execution.md`。
