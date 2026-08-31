# 有效动画膝盖方向符号实验

## 起点与唯一变量

本次从e6ca016封口的155326恢复版开始，Runtime、Configs与c519865/130545一致，facts52/diagnosis21/quality-score1不变。位置轴和加权Goal参考两轮已拒绝，不参与本候选。输入继续使用43357ff3cd384e5cba75d2c31175b116。

唯一变量是现有`CharacterFinalIkFullBodySolver.ApplyLegBendStabilization`的有效动画方向归属：本帧Hip/Knee/Ankle足以给出可靠弯曲方向时，保留这个有符号向量，不为了与历史dot非负而将其取反。既有Target腿轴投影、Profile权重、动态权重、退化判定、Vendor GetDir、Foot Goal、位置Interpolation、Contact与Writer均不改变。动画几何退化时继续原有历史接管和相对上一Applied方向的符号处理，不新增历史、配置或Solver。

这不是ZZZ SmoothKnee复刻；ZZZ的相关开关及权重链没有闭合。本轮依据项目实际消费者和可重复Raw中的确定矛盾实施。

## 证据与假设

`IKConstraintBend.GetDir`在部分权重时混合带长度的本帧FK方向与单位请求方向，满权时直接使用请求方向。`FBIKChain.GetDirToBendPoint`使用`Quaternion.LookRotation(direction,bendDirection)`生成Knee位置。非退化情况下，最终bend向量反号会选到腿轴另一侧，d与-d不是同一无符号plane。

130545的2082个可靠动画行中，327行Recorded Effective与本帧动画经现行Target投影的方向dot约-1。第一处AnimatedPreviousDot负值为0；实际倒置来自第二处Applied历史符号门。R932–935的可靠动画弯曲高度远超退化门，但有效方向仍与当前动画反向；R933/R934已出现接近相消临界和Solved Knee翻侧。327行仅表示请求倒置，不等于327次真实镜像。

| 可证伪的原因 | 预期 | 本轮边界 |
| --- | --- | --- |
| 历史符号门将可靠动画方向翻成反侧 | 移除有效动画分支的翻号后，Recorded Effective与现行动画Target投影一致；R932–935的对应相消/翻侧应减轻 | 本轮唯一行为修改 |
| 零BendWeight使方向约束不参与 | 权重为0的原问题不能由本次方向修正保证消失 | 保留原权重，不加入epsilon或满权 |
| 大腿轴角下Project与旋转运输不同 | 原/目标腿轴大于约90度时，删除历史翻号仍不能保证请求与Vendor运输后的FK同侧 | 已知6行风险原样保留，后续独立实验 |

R822原/目标腿轴夹角约98.794度，动画投影与最短腿轴旋转运输方向dot约-0.892909且BendWeight约0.121723。R823同型但权重0。此候选明确不声称排除所有相消；不能用R932–935的小角样本推广到这些大角窗口。业务取舍是恢复动画作者的本帧膝盖侧向，允许真实方向跨过历史半球；若当前输入/投影本身跳变，本步不添加低通掩盖。

## 历史与诊断边界

原动画退化仅有L113/114、L986/987四行，继续既有Retained Previous语义。第一处翻号负分支没有运行覆盖，删除该逻辑只完成新的方向归属定义，不能称该负分支已动态验证。Pending BendHistory仍随唯一根Bank提交/丢弃。

AnimatedPreviousDot继续记录本帧动画方向与上一Stable方向的真实dot；EffectivePreviousDot记录实际Applied与上一Applied的dot，不取Abs。既有Sampler/Analyzer接受有限负值，不需要API/schema迁移。窄Landing腿诊断仍会将负dot发布为反向证据，规则和评分不改；必须同时检查Solved Knee侧向与位移，不能用这个标签单独判定视觉回归。

## 回放裁决

先核对同一输入、Body、时钟、Original Sole、Foot目标、Profile/权重和查询未变，再逐行重算有效动画方向、Target投影和实际dot。检查327个原倒置行、R714、R819–826、R932–935以及四个退化行；按同侧相邻帧比较Original/Solved Knee相对Hip位移、腿轴侧向、额外offset步与峰值，保留零权重和大腿轴角分组。

全部37项Foot诊断、真实Heel/Toe、Contact间隙/穿透、Anchor、Reach、Pelvis和Goal残差仍须对账。CSV只公开Solved Knee而非最终Physical Knee，不扩大测量结论。仅在已覆盖范围内决定保留或撤销；不凭方向dot变正或总分上升宣布膝盖全部稳定。

本记录对应61615d4的文档提案；该提交不含Runtime代码。随后用户要求优先处理踏空，此路线停止，临时Runtime尝试没有形成正式候选Replay，也没有保留为运行修改。故不发布效果结论，不把“有界静态准入”写成修复通过或真实Replay失败。后续signed pole运输讨论同样不应凭此文档宣称已运行，独立证据出现前保持未验证。
