# Change: 使用可达Ground Envelope驱动Swing脚垂直运动

## Why

当前Foot Placement已经实现`LastLanding -> NextSwingLanding`、Raw Ground Contacts与连续Ground Envelope，但上游Ground Path仍缺少GDC 35页位于Hull之前的Reachability，因此该change已经重新打开。只有补齐Edge可达性后，Ground Envelope才可以作为正式Foot Motion输入；否则墙体或过高障碍也可能被当成正常脚部下界。

Reachability完成后，Ground Envelope仍只进入diagnostics，Pelvis与双脚Goal权重全部为零。用户只能确认世界查询和包络几何，不能确认这些事实是否能沿唯一Goal链正确改变角色脚部Pose。

GDC参考在Ground Envelope之后的下一阶段是`Animation Foot Motion + Ground Envelope`。Ground Envelope不是最终脚轨迹，也不能直接替代动画落点；它应作为地形造成的垂直增量叠加到原生动画脚运动上。本change只完成这一层，让结果在Unity中可直接观察，同时保持Pelvis、Foot Lock、Constraint和脚底旋转不参与。

## What Changes

- 增加独立Swing Foot Motion纯计算模块，输入当前原生动画Ankle/Sole、同一Landing Event且全部Edge可达的Accepted Ground Envelope、`Component Up`和现有`animation.foot-placement-weight`，输出垂直修正与最终Foot Goal事实。
- 使用原生动画Sole在`LastLanding -> NextSwingLanding`水平轴上的进度采样Ground Envelope，并同时采样两个落点之间的直线基线。
- 使用`Ground Envelope高度 - 落点基线高度`作为非负地形增量，沿`Component Up`平移原生动画Ankle与Sole；保留动画水平位置、动画抬脚高度和原始旋转。
- 同一`LandingEventIdentity`在摆动阶段接受实时成功预测；小于更新死区时复用路径。预测点漂移不降低Foot Goal权重；不可走只由Ground Path拒绝。
- Ground Envelope与落点基线重合或垂直增量处于数值容差内时，保留Accepted Foot Motion诊断但发布零位置权重，避免为原生Ankle重复执行FBBIK。
- 只有权威Current Step处于Swing、Landing Event与Ground Path目标一致、全部Edge通过Reachability且Ground Envelope Accepted时，该脚才发布非零位置权重；PreSwing、支撑脚、Invalid Segment脚、其它失败脚和Pelvis继续发布零权重。
- Foot Goal以现有Foot Placement Weight为上限；不增加Landing Confidence、摆动相位、预测误差权重、输入方向、跨帧弹簧或跨帧平滑层。
- 唯一FinalIK FBBIK继续只消费统一GoalSet；不修改其世界查询、规划或后处理职责。
- 扩展成功Seal后的只读diagnostics与采样器，记录原生Sole/Ankle、路径进度、基线采样点、Envelope采样点、垂直修正、最终Goal和实际Goal权重。
- Scene Gizmo在现有落点与Ground Envelope上增加原生Sole、修正后Sole及二者之间的细垂直线，不显示文字，不重新采样或重算。

## Impact

- Affected specs: `character-foot-placement-presentation`
- Affected code: Swing Foot Motion纯计算模块、`CharacterFootPlacementRuntime` Goal生成、Foot Placement只读diagnostics、Scene Gizmo与CSV采样器。
- 不改变Pose Graph拓扑、Goal ABI、FinalIK FBBIK实现、Gameplay State、Network、KCC或Physics查询。
- 不增加Pelvis修正、Foot Lock、Constraint、Anchor、脚底旋转、第二Reachability、第二Grounding、fallback、兼容路径或FBBIK后处理。

## Dependency

本change依赖重新打开的`add-character-foot-ground-path-detection`。必须先完成该change中的`Edge -> Reachability -> Hull -> Accepted Ground Envelope`，并由用户确认普通楼梯保持Accepted、过高垂直面稳定发布Invalid Segment；之后才能实施本change。

实施时必须直接消费上游已经建立的`LastLanding + NextSwingLanding -> Reachable Ground Envelope`唯一链，不得复制Ground Path、Reachability或临时Envelope。归档时必须先归档Ground Path change，再归档本change，使current spec依次从可达包络诊断推进到Swing Foot Goal。

## Current Spec Comparison

- current `character-foot-placement-presentation`仍禁止Ground Envelope并要求三个Goal全部为零；重新打开且未归档的`add-character-foot-ground-path-detection`负责开放完整Reachability与Ground Envelope，仍明确要求Pose恒等。本change只在该前置change完成后进一步开放当前Swing脚的位置Goal。
- current `character-presentation-pose-graph`已经要求Foot Placement通过统一GoalSet连接唯一FullBodyIK，并允许有效Goal触发一次FBBIK；无需修改Goal ABI或增加第二solver。
- active `replace-animation-sequence-with-clip-authoring`会迁移Foot Placement Weight的作者来源，但保持`animation.foot-placement-weight`运行时参数合同；本change只消费该现有参数，不创建第二份权重配置。

## Reference Alignment

- GDC 2016《Fitting the World: A Biomechanical Approach to Foot IK》第11页要求左右脚独立、脚的前进来自动画、动画高度解释为Foot Path之上的高度，并且最终脚不得低于Foot Path。
- 第31与33-36页要求Ground Envelope检测两次落步之间的表面，按Ground Path排序与Edge事实执行Reachability，再用Convex Hull得到仅供脚使用的连续路径。
- 仓库`predict-foot-ik-implementation-summary.md`进一步明确Foot Path是相对两次落点基线的增量路径；本change因此保留`Envelope Sample - Baseline Sample`，不把Ground Envelope世界高度直接覆盖原生脚踝。
- GDC第13-16页的Foot Locking、第17页的Hips稳定、第19页的Foot Orientation和第21-28页的转向支点属于后续独立阶段，不在本change内以临时状态、平滑或Pelvis补洞。
