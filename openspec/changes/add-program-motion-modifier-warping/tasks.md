## 1. 依赖、版本与现状清单

- [x] 1.1 使用UTF-8读取本change的proposal、design、tasks和全部spec delta
- [x] 1.2 使用UTF-8读取`refactor-gameplay-runtime-and-tooling-modules`的proposal、design、tasks和剩余未完成项
- [x] 1.3 使用UTF-8读取`refactor-simulation-tick-hot-path`的proposal、design、tasks和全部spec delta
- [x] 1.4 确认`refactor-simulation-tick-hot-path`已真实完成且Operation Set正式输入版本为`/6`
- [x] 1.5 使用UTF-8读取current `character-action-activation-flow`、`btsmtl-gameplay-semantic-ir`规范与已归档Action eligibility设计
- [x] 1.6 确认Agent Validator能够解析合法owner-local ActionTargetSnapshot declaration
- [x] 1.7 记录当前Frontend、Semantic IR、Operation Set、Float32 ABI、Fixed ABI、State codec和artifact版本
- [x] 1.8 记录Float32 MotionContribution收集、channel resolve、Request构造和Solver入口链路
- [x] 1.9 记录Fixed MotionContribution收集、channel resolve、Request构造和Solver入口链路
- [x] 1.10 记录MotionCurve authoring、curve bake、Semantic emitter、Target lowering和runtime采样链路
- [x] 1.11 记录ActionTargetSnapshot从Blackboard到ActionInstance和committed state的Float32/Fixed链路
- [x] 1.12 记录ActionProfile字符串TargetPolicy的声明、Inspector、catalog和所有引用
- [x] 1.13 记录`MotionWarpTrack`、`MotionWarpClip`、`TimelineMotionWarpWindow`和`Sample()`全部引用
- [x] 1.14 记录Timeline Editor Authoring Preview、Live Debug和Preview Session的正式入口
- [x] 1.15 记录Agent v9 Timeline Snapshot、Patch command、handler、emitter、validator和MCP bridge入口
- [x] 1.16 盘点仓库内全部MotionWarpTrack、MotionWarpClip和非空TargetPolicy序列化资产
- [x] 1.17 冻结需要删除的旧Warp DTO、field、sampler、reader、Inspector和配置路径清单

## 2. MotionWarp Timeline Authoring模型

- [x] 2.1 定义类型化MotionWarp PositionMode
- [x] 2.2 将`Disabled`与`MatchTargetPlanarPosition`纳入PositionMode
- [x] 2.3 定义类型化MotionWarp RotationMode
- [x] 2.4 将`Disabled`、`FaceTarget`与`MatchTargetYaw`纳入RotationMode
- [x] 2.5 为MotionWarpClip增加稳定SourceMotionClipId
- [x] 2.6 为MotionWarpClip增加target-local平面offset
- [x] 2.7 为MotionWarpClip增加target yaw offset
- [x] 2.8 为MotionWarpClip增加position weight和yaw weight
- [x] 2.9 为MotionWarpClip增加最大总平面位置修正
- [x] 2.10 为MotionWarpClip增加最大总yaw修正
- [x] 2.11 为MotionWarpClip增加position cumulative progress curve
- [x] 2.12 为MotionWarpClip增加yaw cumulative progress curve
- [x] 2.13 删除MotionWarpClip的TargetKey字段
- [x] 2.14 删除MotionWarpClip的WeightCurve、EaseInCurve与EaseOutCurve旧Gameplay语义
- [x] 2.15 删除MotionWarpClip的Mixable capability
- [x] 2.16 保留MotionWarpClip窗口可调整能力且不建立独立runtime sampler
- [x] 2.17 删除`TimelineMotionWarpWindow`
- [x] 2.18 删除`MotionWarpTrack.Sample()`和`TrySampleClip()`
- [x] 2.19 删除旧MotionWarp curve evaluate helper
- [x] 2.20 为Timeline正式authoring API增加按stable identity绑定SourceMotionClip的方法
- [x] 2.21 让Source选择器只列出同一Timeline的MotionCurveClip
- [x] 2.22 让Track/Clip重排不改变SourceMotionClipId
- [x] 2.23 让删除被引用MotionCurve时产生明确悬空引用错误
- [x] 2.24 让复制MotionWarpClip时生成新Warp identity并保留显式source identity

## 3. MotionWarp Authoring通用校验

- [x] 3.1 拒绝空SourceMotionClipId
- [x] 3.2 拒绝source identity不存在
- [x] 3.3 拒绝source指向非MotionCurveClip
- [x] 3.4 拒绝source与Warp不属于同一Timeline owner
- [x] 3.5 拒绝source channel不是Action
- [x] 3.6 拒绝source blend mode不是Override
- [x] 3.7 拒绝Warp起点早于source StartFrame
- [x] 3.8 拒绝Warp终点晚于source CurveEndFrame
- [x] 3.9 拒绝Warp窗口为空或反向
- [x] 3.10 拒绝同一source上的Warp窗口重叠
- [x] 3.11 拒绝PositionMode与RotationMode同时Disabled
- [x] 3.12 拒绝position/yaw weight超出`[0,1]`
- [x] 3.13 拒绝负数或非有限位置修正上限
- [x] 3.14 拒绝超出`[0,180]`或非有限yaw修正上限
- [x] 3.15 拒绝非有限target offset与yaw offset
- [x] 3.16 校验两条progress curve只包含有限key
- [x] 3.17 校验两条progress curve时间域为`[0,1]`
- [x] 3.18 校验两条progress curve首值为0且末值为1
- [x] 3.19 校验两条progress curve单调不下降
- [x] 3.20 为FaceTarget的零平面方向定义明确配置或运行错误
- [x] 3.21 让Inspector、Compiler与Agent Validator复用同一校验服务
- [x] 3.22 删除Inspector、Compiler或Agent中的第二套Warp规则

## 4. Action目标要求与统一准入

- [x] 4.1 定义`ActionTargetRequirement.None`
- [x] 4.2 定义`ActionTargetRequirement.SnapshotRequired`
- [x] 4.3 将ActionProfile字符串`m_TargetPolicy`替换为typed requirement
- [x] 4.4 更新ActionProfile Inspector显示typed requirement
- [x] 4.5 更新ActionProfile配置校验拒绝未知枚举值
- [x] 4.6 更新Action catalog保存typed target requirement
- [x] 4.7 删除catalog中的TargetPolicy字符串字段
- [x] 4.8 将candidate ActionTargetSnapshot加入portable admission request
- [x] 4.9 增加typed reject reason `TargetSnapshotRequired`
- [x] 4.10 让portable admission evaluator检查target requirement
- [x] 4.11 让Float32 CanActivateAction读取候选target snapshot
- [x] 4.12 让Fixed CanActivateAction读取候选target snapshot
- [x] 4.13 让Float32 ActivateActionInstance通过同一request提交候选target snapshot
- [x] 4.14 让Fixed ActivateActionInstance通过同一request提交候选target snapshot
- [x] 4.15 让CanActivateActionInfoNode提供与Activate节点同语义的target snapshot reference
- [x] 4.16 更新CanActivateActionInfoNode Inspector和authoring API
- [x] 4.17 拒绝CanActivate与Activate引用不同target declaration的同一transition/activation链
- [x] 4.18 让缺失目标的查询与提交返回相同allowed/reason
- [x] 4.19 保持ActionInstance只保存激活时的immutable target snapshot
- [x] 4.20 禁止ActionRuntime按TargetId查scene、registry或Presentation对象
- [x] 4.21 让包含MotionWarp的Action call site必须绑定SnapshotRequired profile
- [x] 4.22 拒绝Warp Timeline从没有显式Action Context的call site启动

## 5. Numeric-Neutral Semantic IR与Operation Set

- [x] 5.1 为`TimelineMotionWarp`分配稳定operation code
- [x] 5.2 为MotionWarp声明稳定gameplay capability
- [x] 5.3 为MotionWarp source分配typed Program reference kind
- [x] 5.4 为MotionWarp position mode定义numeric-neutral payload
- [x] 5.5 为MotionWarp rotation mode定义numeric-neutral payload
- [x] 5.6 为target offset和yaw offset定义numeric-neutral literal
- [x] 5.7 为position/yaw weight和clamp定义numeric-neutral literal
- [x] 5.8 为两条累计progress curve定义portable curve constant
- [x] 5.9 在Timeline emitter registry登记唯一MotionWarpTrack emitter
- [x] 5.10 在Timeline emitter registry登记唯一MotionWarpClip emitter
- [x] 5.11 让Emitter保存Warp operation到source MotionCurve operation的typed reference
- [x] 5.12 让Emitter保存Timeline owner和Action Context provenance
- [x] 5.13 让SourceMap覆盖Warp Track、Clip、source reference和每个字段
- [x] 5.14 让Semantic validation调用正式Warp authoring校验
- [x] 5.15 让Semantic validation拒绝source operation不唯一
- [x] 5.16 让Semantic validation拒绝MotionWarp capability缺失
- [x] 5.17 将Operation Set从`/6`提升到`/7`
- [x] 5.18 提升Semantic IR payload与artifact版本
- [x] 5.19 提升Frontend compiler identity
- [x] 5.20 将MotionWarp operation、reference和curve编入SemanticHash
- [x] 5.21 删除旧Operation Set reader和兼容分派
- [x] 5.22 更新portable Semantic IR Reader读取和显示MotionWarp
- [x] 5.23 更新Semantic IR Inspector显示MotionWarp source、模式、参数和source map

## 6. Float32与Fixed Target Program降低

- [x] 6.1 在Float32 Program schema增加MotionWarp operation payload
- [x] 6.2 在Fixed Program schema增加同语义MotionWarp payload
- [x] 6.3 将SourceMotionClip reference降低为Float32 source operation index
- [x] 6.4 将SourceMotionClip reference降低为Fixed source operation index
- [x] 6.5 将PositionMode与RotationMode降低到两个Target
- [x] 6.6 将target offset、weight、clamp和progress curve降低到Float32
- [x] 6.7 将同一字段按Fixed规则降低到Fixed
- [x] 6.8 让两个Target再次校验source operation类型和owner
- [x] 6.9 让两个Target再次校验Warp窗口和source CurveEndFrame
- [x] 6.10 让两个Target拒绝不支持的模式或字段
- [x] 6.11 为Program layout增加按channel的modifier descriptor span
- [x] 6.12 为descriptor保存Warp operation、source operation、Timeline owner和state slot range
- [x] 6.13 按Operation Set canonical顺序生成descriptor
- [x] 6.14 将descriptor和Warp constants编入Float32 ProgramHash与LayoutHash
- [x] 6.15 将descriptor和Warp constants编入Fixed ProgramHash与LayoutHash
- [x] 6.16 提升Float32 Target ABI和Program artifact版本
- [x] 6.17 提升Fixed Target ABI和Program artifact版本
- [x] 6.18 删除两个Target的旧Program reader和兼容payload路径
- [x] 6.19 更新普通DotNet Program Reader显示两个Target的MotionWarp descriptor
- [x] 6.20 更新Program Inspector显示modifier range、source和state layout

## 7. Motion Channel解析与Modifier阶段

- [x] 7.1 定义numeric-neutral Motion Modifier eligibility和canonical sequence合同
- [x] 7.2 定义Float32 `ResolvedMotionChannel`
- [x] 7.3 定义Fixed `ResolvedMotionChannel`
- [x] 7.4 让ResolvedMotionChannel保存channel displacement和yaw
- [x] 7.5 让ResolvedMotionChannel保存claim和ConsumeLowerChannels结果
- [x] 7.6 让ResolvedMotionChannel保存resolved owner source identity
- [x] 7.7 让ResolvedMotionChannel保存参与source的紧凑provenance
- [x] 7.8 将Float32 channel仲裁从最终Request构造中拆出
- [x] 7.9 将Fixed channel仲裁从最终Request构造中拆出
- [x] 7.10 保持Additive现有计算顺序
- [x] 7.11 保持WeightedBlend现有计算顺序
- [x] 7.12 保持Override priority与稳定遍历顺序
- [x] 7.13 保持零delta claim与ConsumeLowerChannels语义
- [x] 7.14 增加Action channel Modifier调用点
- [x] 7.15 增加GameplayResult channel预留Modifier调用点但不安装虚假Modifier
- [x] 7.16 保持Locomotion channel第一版无Modifier
- [x] 7.17 在Modifier完成后按原固定channel顺序合成
- [x] 7.18 从最终合成结果计算request velocity
- [x] 7.19 只在最终一步构造唯一CharacterMotionRequest
- [x] 7.20 删除旧accumulator内直接ResolveChannel到Request的耦合实现
- [x] 7.21 删除动态Modifier registry、反射发现和Network Model分派可能性
- [x] 7.22 为无Modifier Program建立零额外业务分支的等价执行路径

## 8. MotionWarp生命周期与typed state

- [x] 8.1 定义MotionWarp state semantic identity
- [x] 8.2 定义active和initialized state slot
- [x] 8.3 定义Timeline playback generation state slot
- [x] 8.4 定义ActionInstance identity state slot
- [x] 8.5 定义window start body position和yaw state slot
- [x] 8.6 定义clamped total planar correction state slot
- [x] 8.7 定义clamped total yaw correction state slot
- [x] 8.8 定义last position progress state slot
- [x] 8.9 定义last yaw progress state slot
- [x] 8.10 在Frontend为每个MotionWarp operation声明固定state layout
- [x] 8.11 在Float32 Program降低Warp state slots
- [x] 8.12 在Fixed Program降低同语义Warp state slots
- [x] 8.13 让Timeline首次进入窗口建立新的Warp generation
- [x] 8.14 让同一generation后续Tick复用已计算总修正
- [x] 8.15 让Timeline stop清理active Warp state
- [x] 8.16 让Action terminal清理对应Warp state
- [x] 8.17 让Timeline seek或cycle generation变化重建Warp state
- [x] 8.18 拒绝Warp state中的ActionInstance与当前Context不一致
- [x] 8.19 拒绝Snapshot恢复出的非法progress或非有限correction
- [x] 8.20 保持raw contributions、resolved channel和Request不进入committed state

## 9. Float32 MotionWarp Target实现

- [x] 9.1 从resolved Action owner判断source eligibility
- [x] 9.2 从显式Action Context读取当前ActionInstance
- [x] 9.3 从ActionInstance读取immutable Float32 target snapshot
- [x] 9.4 在窗口首次进入时读取committed body pose
- [x] 9.5 采样source MotionCurve窗口起点到CurveEndFrame的剩余累计轨迹
- [x] 9.6 计算nominal authored end planar pose
- [x] 9.7 按PositionMode计算desired planar position
- [x] 9.8 按RotationMode计算desired yaw
- [x] 9.9 按position/yaw weight缩放总修正
- [x] 9.10 按最大位置和yaw上限clamp总修正
- [x] 9.11 按当前canonical Timeline fraction采样position progress
- [x] 9.12 按当前canonical Timeline fraction采样yaw progress
- [x] 9.13 以current-last progress计算本Tickposition correction delta
- [x] 9.14 以current-last progress计算本Tickyaw correction delta
- [x] 9.15 将correction delta应用到resolved Action channel
- [x] 9.16 在source未成为resolved owner时记录SourceNotResolved且不修改channel
- [x] 9.17 在同Tick多个eligible Warp时fail-stop
- [x] 9.18 在缺失Action Context或target snapshot时fail-stop
- [x] 9.19 禁止Float32 Warp访问Unity Transform、GameObject或Solver

## 10. Fixed MotionWarp Target实现

- [x] 10.1 从resolved Action owner判断Fixed source eligibility
- [x] 10.2 从显式Action Context读取Fixed ActionInstance
- [x] 10.3 从ActionInstance读取immutable Fixed target snapshot
- [x] 10.4 在窗口首次进入时读取Fixed committed body pose
- [x] 10.5 以Fixed累计曲线计算source剩余轨迹
- [x] 10.6 以Fixed运算计算nominal authored end planar pose
- [x] 10.7 以Fixed运算计算PositionMode目标
- [x] 10.8 以Fixed运算计算RotationMode目标
- [x] 10.9 以Fixed运算应用weight和clamp
- [x] 10.10 以Fixed运算采样两条progress curve
- [x] 10.11 以Fixed累计progress差计算position/yaw delta
- [x] 10.12 将Fixed correction应用到resolved Action channel
- [x] 10.13 保持SourceNotResolved、歧义和缺目标reason与Float32一致
- [x] 10.14 禁止Fixed Warp回退到float、Unity对象或Float32 Program
- [x] 10.15 删除Float32/Fixed之间重复的eligibility与lifecycle业务分支

## 11. State Codec、Snapshot、Hash与Rollback边界

- [x] 11.1 将Float32 Warp state写入Character State codec
- [x] 11.2 将Float32 Warp state读回Character State codec
- [x] 11.3 将Fixed Warp state写入Character State codec
- [x] 11.4 将Fixed Warp state读回Character State codec
- [x] 11.5 将Warp state纳入Float32 StateHash
- [x] 11.6 将Warp state纳入Fixed StateHash
- [x] 11.7 提升两个Target的State codec identity
- [x] 11.8 让World Snapshot与History自然包含Warp state
- [x] 11.9 让restore后下一Tick从last progress继续而不重复修正
- [x] 11.10 让rollback replay对同一input、target snapshot和world result产生同一request
- [x] 11.11 删除任何Session-local或MonoBehaviour Warp cache
- [x] 11.12 拒绝旧State codec、Snapshot和History payload

## 12. WorldSolver边界与Structured Trace

- [x] 12.1 保持CharacterMotionRequest schema不包含MotionWarp业务字段
- [x] 12.2 保持ICharacterWorldSolver接口不包含Action、Timeline或target参数
- [x] 12.3 保持Unity CharacterController Solver只消费最终request
- [x] 12.4 保持Deterministic KCC Solver只消费最终request
- [x] 12.5 保持其它authority solver通过同一request接入
- [x] 12.6 删除Solver后位置补偿或Presentation Warp入口
- [x] 12.7 扩展Trace记录raw MotionContribution
- [x] 12.8 扩展Trace记录每个resolved channel与owner
- [x] 12.9 扩展Trace记录Warp operation、source operation和ActionInstance
- [x] 12.10 扩展Trace记录target snapshot、nominal end和desired pose
- [x] 12.11 扩展Trace记录total correction与current progress delta
- [x] 12.12 扩展Trace记录最终request与actual solver result
- [x] 12.13 为SourceNotResolved、TargetSnapshotRequired、AmbiguousModifier和InvalidState定义稳定diagnostic code
- [x] 12.14 禁止Diagnostics读取mutable accumulator或Solver私有对象

## 13. Timeline Inspector、Authoring Preview与Live Debug

- [x] 13.1 在MotionWarpClip Inspector显示Source MotionCurve选择器
- [x] 13.2 在Inspector按PositionMode显示位置字段
- [x] 13.3 在Inspector按RotationMode显示yaw字段
- [x] 13.4 在Inspector显示position/yaw weight与clamp
- [x] 13.5 在Inspector显示两条累计progress curve
- [x] 13.6 在Timeline窗口标记悬空source和非法窗口
- [x] 13.7 在Timeline窗口显示Warp窗口与source MotionCurve的视觉关联
- [x] 13.8 为Preview Session输入增加editor-only ActionTargetSnapshot
- [x] 13.9 保证preview snapshot只属于窗口session且不写Timeline资产
- [x] 13.10 让完整Gameplay Preview通过正式Program编译和Action admission
- [x] 13.11 让Preview通过正式Motion channel resolver和Modifier
- [x] 13.12 让Preview通过正式Preview WorldSolver取得actual body result
- [x] 13.13 让缺少Preview target时显示TargetSnapshotRequired并停止Gameplay预览
- [x] 13.14 让纯Animation Preview明确不执行MotionWarp
- [x] 13.15 禁止Preview直接写目标GameObject Transform模拟Warp
- [x] 13.16 在Live Debug显示当前Warp source和ActionInstance
- [x] 13.17 在Live Debug显示target、nominal end和desired pose
- [x] 13.18 在Live Debug显示position/yaw progress与累计修正
- [x] 13.19 在Live Debug显示final request和actual solver result差异
- [x] 13.20 保持Authoring Preview与Live Debug session隔离

## 14. Agent v9 Authoring闭环

- [x] 14.1 更新Agent Snapshot识别MotionWarpTrack subtype
- [x] 14.2 更新Agent Snapshot识别MotionWarpClip subtype
- [x] 14.3 输出MotionWarp stable track和clip identity
- [x] 14.4 输出SourceMotionClipId和resolved source path
- [x] 14.5 输出PositionMode与RotationMode
- [x] 14.6 输出target offset与yaw offset
- [x] 14.7 输出position/yaw weight和clamp
- [x] 14.8 输出两条canonical progress curve
- [x] 14.9 为Patch DTO增加typed ensure MotionWarp track命令
- [x] 14.10 为Patch DTO增加typed ensure MotionWarp clip命令
- [x] 14.11 为Patch DTO增加typed configure MotionWarp source命令
- [x] 14.12 为Patch DTO增加typed configure MotionWarp parameters命令
- [x] 14.13 复用现有delete Timeline clip命令删除MotionWarpClip
- [x] 14.14 让lowerer按stable identity解析source MotionCurve
- [x] 14.15 让handler只调用Timeline正式authoring API
- [x] 14.16 让dry-run与apply消费同一immutable command plan
- [x] 14.17 更新Agent node/timeline emitter registry白名单
- [x] 14.18 更新Validator校验source identity、owner与window
- [x] 14.19 更新Validator校验mode、weight、clamp和curve
- [x] 14.20 更新Validator校验Action Context和SnapshotRequired
- [x] 14.21 更新Validator校验同source Warp窗口唯一性
- [x] 14.22 更新Agent snapshot/patch schema identity或确认v9可无歧义扩展
- [x] 14.23 若schema升版则删除旧reader和兼容分支
- [x] 14.24 更新Agent MCP bridge暴露新typed operation且不增加资产直写入口
- [x] 14.25 删除Agent对旧TargetPolicy字符串和旧MotionWarp字段的读写

## 15. 资产迁移、生成产物与旧路径删除

- [x] 15.1 根据基线清单确认仓库内MotionWarpClip实际数量
- [x] 15.2 若存在MotionWarpClip则在删除旧类型前完整迁移stable identity和曲线key
- [x] 15.3 若不存在MotionWarpClip则不创建一次性migrator
- [x] 15.4 将每个ActionProfile的TargetPolicy显式迁为typed requirement
- [x] 15.5 保持当前无目标Corin动作显式为None
- [x] 15.6 不为Corin创建假目标、scene target lookup或临时Blackboard writer
- [x] 15.7 重新生成Corin Semantic IR
- [x] 15.8 重新生成Corin Float32 Program和Projection
- [x] 15.9 重新生成Corin Fixed Program
- [x] 15.10 重新生成Corin Local Program/Projection与Unity Authority、DotRecast、Rollback产品manifest中受版本影响的identity
- [x] 15.11 删除旧`TimelineMotionWarpWindow`
- [x] 15.12 删除旧MotionWarp sampler和未消费字段
- [x] 15.13 删除字符串TargetPolicy reader、writer和Inspector
- [x] 15.14 删除旧Semantic IR、Program、State和产品manifest reader
- [x] 15.15 搜索并删除MotionWarp的scene Transform、Presentation或Solver旁路
- [x] 15.16 搜索并确认不存在BBB WarpedMotionData、MotionProposal或旧MotionStage回流

## 16. 架构文档、编译与OpenSpec收口

- [x] 16.1 更新`openspec/project.md`的Motion链路为Contribution、Channel Resolve、Modifier、Request和Solver
- [x] 16.2 删除`openspec/project.md`中“当前无独立MotionWarp runtime”的过时描述
- [x] 16.3 更新current specs中Operation Set、Target ABI、State codec和Agent schema最终identity
- [x] 16.4 更新正式实现清单，记录MotionWarp能力与target provider缺口
- [x] 16.5 对照全部current specs删除与旧TargetPolicy、旧Warp sampler或直接Request构造矛盾的描述
- [x] 16.6 确认不包含fallback、compatibility reader、migrator残留、双写或第二Warp路径
- [x] 16.7 确认不包含Network Model、Solver或Presentation专用MotionWarp实现
- [x] 16.8 确认Float32与Fixed operation、state、codec、trace和artifact版本同步
- [x] 16.9 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的正式命令编译portable Core
- [x] 16.10 编译后立即执行`dotnet build-server shutdown`
- [x] 16.11 使用同样参数编译Assembly-CSharp
- [x] 16.12 编译后立即执行`dotnet build-server shutdown`
- [x] 16.13 使用同样参数编译Assembly-CSharp-Editor
- [x] 16.14 编译后立即执行`dotnet build-server shutdown`
- [x] 16.15 使用正式portable Reader读取Corin Semantic IR、Float32 Program和Fixed Program
- [x] 16.16 确认Reader能够解释最终schema、版本、hash及Corin显式零Motion Modifier结果；不伪造Warp实例
- [x] 16.17 运行`openspec validate add-program-motion-modifier-warping --strict --no-interactive`
- [x] 16.18 确认没有运行Unity batchmode且没有新增测试或人工验证task
- [x] 16.19 全部实现与清理完成后将本tasks checklist逐项更新为真实`[x]`
