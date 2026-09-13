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

Direct Clip、Action、Blend Space、Motion Matching、C#作者API与Foot Analysis Apply MUST消费同一catalog，MUST不按Runtime参数名、可见短名或仅按`propertyName`查找第二条Clip Curve。缺失、重复、旧property binding或非法Curve MUST阻止正式Apply或依赖该数据的后续Build，不得生成默认Curve。

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
