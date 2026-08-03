# Design: Pose Source资产引用式作者模型

## Context

当前链路是：

```text
Pose Player.m_SourceId
  -> Profile.PoseSourceBindings按字符串查找
  -> PresentationPoseSourceBinding
  -> AnimationClip / BlendSpace / Motion Matching provider
  -> Projection binding
  -> Runtime PresentationPoseSourceId
```

字符串同时承担作者引用、Profile lookup、Document字段、编译source map和Runtime identity。作者UI因此容易把机器字段直接投影出来，shared Graph变体能力也和具体字符串命名约定绑在一起。

需要同时满足四个边界：

- 作者通过Unity对象和业务名称编辑动画，不输入identity。
- Profile继续是角色资源binding的唯一owner，shared Pose Graph仍能被不同角色Profile复用。
- Document v3、人工UI和编译器继续走同一typed Presentation Mutation与资产事务。
- Runtime不读取ScriptableObject或AssetDatabase，只消费Projection内的不可变dense计划。

## Goals

- Pose source资源关系在Unity authoring资产内完全使用类型化对象引用。
- Player Details直接显示最终资源和状态，支持选择、Ping、Open与Owner导航。
- Profile和Pose Graph不再保存可编辑Source Id或Provider Id字符串。
- Graph、Profile、Document、Compiler与Runtime只保留一条正式关系链。
- stable node/state/edge identity继续服务Undo、clipboard、Document与source map，但默认不出现在作者UI。
- Corin现有source完成一次正式迁移，旧字段和旧配置全部删除。

## Non-Goals

- 不把每个Graph、Node、State、Transition、Parameter、AnimationChannel或Slot都制作成顶层ScriptableObject资产。
- 不删除Unity YAML为对象引用自动保存的GUID和fileID；它们属于Unity序列化实现，不是作者字段。
- 不允许按资源显示名、目录、数组index或上一次窗口context解析引用。
- 不把持续Locomotion重新放入Timeline或Gameplay AnimationChannel。
- 不改变Action producer、Gameplay lifecycle、MotionWarp、FootPlacement算法或网络状态。
- 不增加自动Compile、自动Build、自动Analysis或AssetDatabase watcher。

## Decision 1: Source Slot与Profile Binding均使用子资产对象引用

Pose Graph拥有语义Slot，Profile拥有资源binding：

```text
CharacterPresentationPoseGraphAsset
  SourceSlots[]
    CharacterPresentationPoseSourceSlot
      name = "Idle"
      acceptedKind = Sequence

CharacterAnimationPresentationProfile
  PoseSourceBindings[]
    CharacterSequencePoseSourceBinding
      slot -> Idle Source Slot
      clip -> Corin_Idle.anim
      rig -> Corin Rig
      loop / play rate / marker / foot analysis

SequencePlayer
  sourceSlot -> Idle Source Slot
```

Source Slot和binding均是`ScriptableObject`子资产：

- Slot保存在Pose Graph `.asset`内，名字是作者可见业务名称。
- binding保存在Profile `.asset`内，按Sequence、Blend Space与Motion Matching使用不同具体类型。
- Profile数组只保存binding对象引用，不再保存内联union和Source Id。
- Player字段只接受类型匹配的Slot对象。
- Unity对象引用提供移动、重命名、Ping、Open、Undo和精确类型约束；实际YAML中的GUID/fileID由Unity管理。

### 为什么不让Player直接引用AnimationClip

直接Clip最接近ALS的单节点体验，但会把角色资源写进shared Pose Graph。两个角色若复用同一拓扑却使用不同Idle或Run动画，就必须复制整张图或增加第二覆盖系统。Source Slot保留语义拓扑，Profile binding负责角色资源，二者仍只有一条正式映射。

### 为什么不用隐藏字符串加Dropdown

Dropdown只能改善显示，无法消除拼写、复制、重命名、类型错误和跨Profile孤儿引用；底层仍需要字符串lookup与兼容诊断。该方案不满足用户已经明确提出的“不要这种字符串”，不采用。

### 为什么使用子资产而不是每个source一个顶层资产

顶层Source Asset便于跨Profile独立复用，但Idle、Start、Loop、Stop、Turn会快速制造大量Project文件。子资产仍有稳定Unity对象身份与ObjectField体验，同时保持Pose Graph/Profile作为清晰聚合根。需要跨角色复用时共享Pose Graph Slot，并由各Profile绑定自己的资源即可。

## Decision 2: 编译层使用dense source index，不延续作者Source Id

Projection Compiler以精确对象关系建立表：

```text
Player Source Slot object
  -> exact Profile Binding object
  -> typed resource object
  -> Projection source table index
  -> source resource plan
```

编译规则：

1. 只收集当前Definition/Profile可达Pose Graph中的Source Slot。
2. 每个可达Slot必须有且只有一个类型匹配的Profile binding。
3. binding必须属于当前Profile子资产，Slot必须属于当前Pose Graph闭包。
4. Compiler按稳定Unity对象身份确定性排序，生成Projection-local dense source index。
5. Projection source map保存只读业务名、owner与Editor定位信息；Runtime只使用dense index和Projection revision。
6. `PresentationPoseSourceSample`使用`SourceIndex + PlayerNodeIdentity + Generation + FrameLease`完成匹配。

Runtime内部若保留由Compiler按`PlayerNodeId`生成的typed provider identity，它只用于单帧demand/sample路由，不是作者字段、资源绑定键或按字符串查找动画资产的入口。

Pose不进入Rollback snapshot或网络协议，dense index只在同一Projection revision内有意义，因此不需要把作者GUID字符串带入Runtime。Projection revision变化时现有Preview/Runtime本来就必须停止或重建，不能跨revision猜测source。

## Decision 3: 机器identity与作者显示彻底分层

字段分为三类：

| 类型 | 作者控件 | 序列化/编译 |
|---|---|---|
| Unity资源关系 | 类型受限ObjectField或可搜索资源选择器 | Unity对象引用，Build后dense index |
| 领域声明关系 | 由精确上下文提供的可读Dropdown | stable typed identity |
| Graph结构identity | 不显示、不编辑 | stable node/state/edge identity |

通用Details规则：

- `AssetReference`必须声明精确Unity对象类型，不能使用`UnityEngine.Object`无约束字段。
- `IdentityReference`必须由领域option source提供选项；缺失选项时显示Unavailable并禁止编辑，不能退化为TextField。
- Popup只显示`DisplayName`，不拼接内部value。
- 当前引用丢失时显示`Missing Reference`和修复入口；原identity只进入显式Diagnostics。
- `References`显示资源对象、owner、consumer与状态，不把identity伪装成业务信息。

Pose Player选中后的Authoring与References目标为：

```text
Source Slot      Idle
Animation        Corin_Idle.anim       [Ping] [Open Source]
Profile Owner    Corin Animation Profile [Open Owner]
Duration         2.13s
Rig              Corin Rig
Loop             Enabled
Markers          Locomotion.Gait / Ready
Foot Analysis    Ready
```

节点副标题、Navigator和breadcrumb只使用Source Slot名、State显示名、资源名和owner名。内部identity只在Diagnostics折叠区按明确开发者意图显示。

## Decision 4: Profile source editor仍是唯一资源写入口

Pose Graph Details可以选择Source Slot并只读显示解析后的Profile binding。修改Clip、Blend Space、MM provider、marker、Foot Placement Weight或analysis时必须打开Profile source editor；不在节点上复制这些字段。

Profile Inspector负责：

- 创建、重命名和删除Profile binding子资产。
- 选择对应Pose Graph Source Slot。
- 编辑具体资源和source-local配置。
- 显示全部PoseState/Player consumer。
- 打开Clip、Blend Space、MM Profile和Analysis Source。

删除仍被Player引用的Slot或删除仍绑定Slot的binding必须被Mutation preflight拒绝，或者在同一显式事务中连同全部引用一起删除；不得留下字符串孤儿。

## Decision 5: Document v3使用结构化子资产引用

`editable/presentation/profile.json`和Pose Graph node property使用结构化引用：

```json
{
  "assetGuid": "...",
  "localFileId": 123456,
  "assetPath": "Assets/.../CorinAnimationPresentationProfile.asset"
}
```

规则：

- 已存在对象必须由`assetGuid + localFileId`精确解析；`localFileId`是有符号64位整数且必须非零，Unity生成的负子资产file id完全合法；`assetPath`用于可读定位并必须指向同一资产文件。
- Profile主资产与子资产共享GUID，`localFileId`区分具体Source binding。
- 新建Slot或binding使用`local:<meaningful-id>`声明计划身份；dry-run分配正式子资产创建Mutation，apply成功后reverse export写回正式结构化引用。
- editable不得直接指定任意Unity fileID作为新对象身份，也不得按`name`搜索子资产。
- asset catalog提供业务名、类型、owner path和read-only解析状态，AI无需猜GUID。
- 人工UI与Document Reconciler都调用同一Source Slot/binding typed Mutation。

Document仍需要机器identity来做hash、Conflict和精确引用，但该identity属于AI工作包和事务协议，不进入人工作者控件，也不是业务字符串配置。

## Decision 6: 一次迁移后删除旧字段

迁移顺序必须保持单线：

1. 使用旧实现对精确Corin Definition执行一次Document checkout，冻结现有source、resource、marker、curve和consumer事实。
2. 安装新Source Slot、binding子资产、typed payload、Document schema和Mutation实现。
3. 将已checkout目标转换为新规范Document：Graph声明Slot子资产，Profile声明binding子资产，Player引用Slot。
4. dry-run必须显示旧字符串binding删除、新子资产创建和Player引用替换的完整计划。
5. apply在一个资产级Undo/保存事务中创建子资产、重写引用并删除旧序列化数据。
6. reverse export后Document回到Clean；再由明确Character Build命令发布新的Projection。
7. 删除旧字段、旧constructor、旧codec、旧validator、旧Inspector和旧Document属性，不保留reader或双写。

如果实现时无法在同一个正式Mutation事务内创建、引用和回滚子资产，必须停下来扩展现有事务owner，不能改走YAML、临时菜单、ScriptedImporter或第二mutation service。

## Active Change Rebase

以下active change必须按本设计重写尚未归档的结论：

- `refactor-animation-control-boundaries`：作者层Source Id改为Source Slot对象，runtime sample改为Projection dense source index。
- `add-character-presentation-blend-space`：BlendSpacePlayer引用Sequence/BlendSpace类型匹配的Slot，不直接引用BlendSpace，也不保存字符串Source Id。
- `add-character-motion-matching-pose-source`：MM provider成为Profile binding具体类型，SelectedPosePlayer引用MM Slot，provider/sample使用dense source index。
- `fix-pose-state-machine-authoring-interactions`：只涉及layout，不改变Source模型；其未完成Document刷新不得引入旧字符串兼容。

## Risks

- Unity子资产创建、删除、Undo与Document apply回滚需要同一资产事务精确覆盖，不能只修改数组引用。
- shared Pose Graph从不同Profile打开时，Details必须依赖精确Definition/Profile上下文；无上下文只能显示Slot，不能猜binding。
- active MM与Blend Space change仍有大量Source Id代码和文档，实施范围较大；若只改Sequence会产生三套source身份，禁止部分落地。
- Unity YAML仍会显示GUID/fileID，这是对象引用的正常物理格式；验收口径是人工UI和authoring API不要求作者阅读或输入这些值。

## Validation

实现收口后必须能从代码和资产链证明：

- Pose Player authoring payload不存在`m_SourceId`和`m_ProviderId`字符串资源关系。
- Profile不存在内联`PresentationPoseSourceBinding[]`字符串映射。
- Sequence、Blend Space与MM三类source都经过Source Slot对象、Profile binding子资产和Projection dense index同一链路。
- 通用Details不会把IdentityReference退化成可编辑TextField，也不会把identity拼进显示标签。
- Profile、Navigator、breadcrumb和节点Details默认不显示GUID、哈希、revision或stable identity。
- Document创建和修改Source子资产只经过唯一Reconciler、typed Presentation Mutation和资产级事务。
- 旧Corin source字符串、旧Document字段、旧validator与旧兼容代码已删除。
- 没有selection、窗口打开、Inspector重绘或AssetDatabase refresh触发Compile、Build或Analysis。
