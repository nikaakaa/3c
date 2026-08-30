## Decision 1: 一个质量维度只有一个计分Target

| 维度 | 权重 | 唯一质量Target |
|---|---:|---|
| 下陷／穿透 | 20% | final-contact-plane-penetration |
| 接触未贴合 | 20% | contact-support-gap |
| 普通Swing平顺度 | 15% | stable-swing-output-jump |
| Path变化连续性 | 15% | path-revision-output-jump |
| 接触状态交接 | 15% | contact-state-output-jump |
| 腿部姿态／可达性 | 10% | landing-leg-extension |
| 锁脚水平稳定性 | 5% | locked-horizontal-drift |

这是初版参考权值，不宣称客观最优。Accuracy占40%。原动画侵入、Foot新增/加重侵入、Floor交接、Plant、Contact Acquisition、Release Correction回拉、Query复用、Residual、State/Anchor合同和反事实只提供原因证据，不参与加权。保留独立问题和全部Facts，不用删除事件改善分数。

## Decision 2: 样本域与去重

最终可见输出为同空间的`PhysicalProbe - AnimatedSourceProbe`；相邻差的Ankle/Heel/Toe最大值每个Side/相邻帧对只计一次。连续Accepted无Anchor Swing帧对按既有语义Path修订规则分为PathRevision或StableSwing；包含Landing/Locked/Releasing的帧对统一归ContactState，绝不再进入Swing两个域。ContactState保留具体状态转换分类；Plant/Floor/Acquisition作为阶段证据附属。

ContactState计分进一步使用真实物理位移P到动画位移线段`[0,S]`的距离：`t=clamp(dot(P,S)/dot(S,S),0,1)`，额外位移为`|P-t*S|`。世界位置保持P=0与正常动画跟随P=S都不因此扣分，避免把锁脚抵消动画的Correction变化误报跳变；原始Offset步长仍作为证据保留。

最终穿透以同一ContactPlanePenetration段的Heel/Toe最大深度计一次，Heel/Toe细分及新增/加重归因不再次扣分。接触未贴合继续只测同Event Verified Anchor平面的Landing/Locked，Releasing不适用；整脚间隙为两接触点正间隙的最小值。连续超过1厘米至少100毫秒或Locked超过1厘米才进入计分间隙，短时Landing收敛留在原始间隙分布。未贴合不能证明有限Surface脚下有地。

锁脚质量只消费FullAnchor最终物理Sole相对Anchor的水平偏差；Sliding水平移动不按固定Anchor漂移判错，垂直问题只作为穿透链相关证据，不重复增加权重。腿部继续使用现有Landing域，不扩大为未经验证的全时段膝盖判定；样本量不足必须显示。

## Decision 3: 透明而浅层的评分

沿用单项公开发生率和互斥严重度档位；米制主项使用1/2/5/10厘米，档位负担为0/0.1/0.35/0.7/1，严重尾部单项上限为100/95/89/74/49。腿部使用既有明确规则发生率。每项保留原始次数、分母、幅度/时间分布、扣分与代表帧，不把分数命名为Pass/Fail。

总分为7个单项Health按固定权重加权和，只作`ProvisionalReference`。不把Evidence乘进Health，不重分配缺失项权重。零eligible或必需可见事实缺失时该维度Unavailable；总分为空，同时发布可计算权重、已知贡献、可能区间与缺失维度。低样本维度可以保留观测分，但必须同时显示Evidence及弱证据列表，不能把少量零命中描述为质量已经证明。

Path最终可见跳变质量和首个放大阶段归因分离：可见事实完整即可统计质量，阶段缺失只使原因证据Unavailable，不凭未知原因否认已经观测到的跳变。

删除旧文件级无权平均总分，避免两套汇总。唯一`quality-score.json`列出权重、维度Target链接、Health、Evidence、加权贡献、最差项、限制、规则版本与facts哈希。总分只允许在同评分规则、同输入和表现调度的样本间作粗略比较。
