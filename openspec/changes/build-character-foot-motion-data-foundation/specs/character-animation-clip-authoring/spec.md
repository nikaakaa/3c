## MODIFIED Requirements

### Requirement: 项目注册表现Curve必须使用唯一channel catalog

唯一channel catalog MUST登记`presentation.locomotion-phase`、`presentation.foot-placement-weight`及左右脚各11条Foot Motion Data Curve的完整Unity Curve Binding、Clip秒域、单位、值域、切线约束、必填条件和当前消费阶段。全部注册Curve key time MUST使用秒并完整覆盖`[0, SourceDurationSeconds]`。

Animation Window接收器 MUST显示为短名`Clip Curves`，property MUST显示为：

```text
Gait Phase / Foot IK
L/R Step Time / Step Dist / Foot Height
L/R Toe Height / Toe Speed
L/R Pos Error / Rot Error
L/R Contact / Lock Mode / Lock Weight / Support
```

稳定channel identity MUST使用完整领域名称；可见短名不得成为查找identity。`Step Time` MUST使用秒且非负，`Step Dist`、`Foot Height`、`Toe Speed`与Pos/Rot Error MUST非负，Contact、Lock Weight与Support MUST位于`[0,1]`，Lock Mode MUST只取`0/1/2`并使用Constant切线。Step Time与Step Dist的Event边界 MUST按规范离散规则表达，不得用平滑曲线跨越事件跳变。

Direct Clip、Action、Blend Space、Motion Matching、Agent Document与Foot Analysis Apply MUST消费同一catalog，MUST不按Runtime参数名、可见短名或仅按`propertyName`查找第二条Clip Curve。缺失、重复、旧property binding或非法Curve MUST阻止正式Apply或依赖该数据的后续Build，不得生成默认Curve。

本change内只有`Foot IK`继续降低为Runtime `animation.foot-placement-weight`，`Gait Phase`只供正式Sync Group；新增22条Foot Motion Curve MUST进入Registered Curve Hash但不得生成Runtime payload。

#### Scenario: 打开RunLoop脚步数据

- **WHEN** 作者在Unity Animation Window打开已经Apply完整候选的RunLoop
- **THEN** `Clip Curves` MUST显示左右脚22条完整秒域曲线及Gait Phase/Foot IK
- **AND** MUST不显示旧长Receiver名称、旧property binding或隐藏Sequence曲线副本

#### Scenario: Lock Mode使用平滑切线

- **WHEN** Lock Mode曲线在相邻key之间产生非`0/1/2`中间值
- **THEN** Catalog validation MUST拒绝该Curve并报告Clip、脚侧、时间和值
- **AND** MUST不把中间值四舍五入后继续Apply

#### Scenario: 数据阶段执行Projection Build

- **WHEN** 全部Foot Motion Curve合法但后续Runtime消费者尚未实施
- **THEN** Registered Curve Hash MUST覆盖这些Curve
- **AND** Projection MUST不发布未消费的Foot Motion runtime payload

## ADDED Requirements

### Requirement: GDC脚步数据必须原子Apply到同一AnimationClip

Foot Analysis MUST为一个精确Target AnimationClip生成同时包含左右脚22条Foot Motion Curve的不可变候选。候选 MUST携带Target对象身份、完整dependency baseline、Target AnimationClipAnalysisInputHash、显式Motion Reference对象身份与AnimationClipAnalysisInputHash、生成前Registered Curve Hash、Artifact identity/content hash、Rig、Sampling Rig、Calibration、Geometry Validation、format和algorithm version。

作者显式Apply时 MUST重新校验全部lineage，并在一个Undo事务中替换全部22条完整Curve。系统 MUST不允许只Apply单脚、单Event或单Curve，不得在失败后留下部分新binding。Apply MUST只改变Registered Curve Hash和相关Projection stale状态；骨骼、Root、Loop与Source Duration不变时，AnimationClipAnalysisInputHash和同输入Artifact MUST保持Ready。

#### Scenario: 完整候选Apply

- **WHEN** 候选lineage未过期且22条Curve全部合法
- **THEN** Apply MUST在同一个原生AnimationClip和一个Undo事务中替换全部22条Curve
- **AND** 重新读取Catalog MUST逐值获得候选Curve

#### Scenario: Apply前已有单条曲线变化

- **WHEN** 作者在候选生成后修改任一注册Curve
- **THEN** Registered Curve Hash MUST使整个候选Stale并拒绝Apply
- **AND** MUST不以旧候选覆盖作者的新修改或只写剩余21条Curve

### Requirement: 单Clip Foot Motion Bake必须先Diff并保护已有作者曲线

系统 MUST提供唯一精确单Clip Bake Session，以Analysis Source与Target原生AnimationClip为输入，并从Source正式配对只读解析Motion Reference。Inspector、批处理与Unity自定义工具 MUST只调用该Session，不得各自生成Candidate、比较Curve或写入AnimationClip。

Analyze MUST重建当前Artifact、生成完整22条Candidate，把现有Curve组分类为`Empty / Same / Different / Partial`，并返回Target Registered Curve Hash、Artifact identity/content hash、changed channel列表与稳定plan hash。`Empty` MAY首次Apply，`Same` MUST报告No Change；`Different`与`Partial` MUST默认拒绝覆盖。Apply MUST提交精确expected plan hash；任一输入、Artifact或注册Curve变化后 MUST拒绝旧plan。只有显式`replaceExisting=true`才可原子覆盖已有不同数据，成功后 MUST逐Key验证22条正式Curve与Candidate完全相同。

#### Scenario: 作者手调后重新Analyze

- **WHEN** Target已有完整22条Curve且任一Curve与新Candidate不同
- **THEN** Analyze MUST列出changed channel并把plan标记为Different
- **AND** 普通Apply MUST拒绝覆盖，只有作者显式Replace才可继续

#### Scenario: Candidate生成后作者继续修改

- **WHEN** Analyze返回plan后作者修改任一注册Curve
- **THEN** Apply MUST因plan hash或Registered Curve Hash不匹配而拒绝
- **AND** MUST不重新生成Candidate并把修改后的Hash静默吸收为新基线

#### Scenario: 批处理包含已有差异

- **WHEN** 批处理Analyze发现一个或多个Clip为Different或Partial
- **THEN** 系统 MUST在写入任一Clip前一次报告全部覆盖目标并等待显式确认
- **AND** 取消确认 MUST保持全部AnimationClip不变
