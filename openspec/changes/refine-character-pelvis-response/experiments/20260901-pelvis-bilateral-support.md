# 双脚落地后的骨盆共同高度

## 结论

提交`943641f8c`通过固定Record的数据准入，保留给可视化长时双支撑测试。它修正了一个确定的职责缺口：双脚都持有正式ContactAnchor时，不再因为缺少Swing把骨盆目标直接清零。

当前Record只有1帧满足正向双支撑条件，因此不能把本轮写成“最终半蹲已经完整消失”。

## 原因与唯一变量

前版骨盆只接受“主支撑脚＋Swing落点”。1036帧双脚都为Landing／VerifiedAnchor，目标高度差1.072毫米，共同高度应为+20.628毫米；旧版却因MissingSwingLanding进入Releasing，目标为0。

新增BilateralSupport状态，仅在以下条件同时成立时使用双脚共同高度：

- 左右Resolved Foot Goal均有效；
- 左右Support位置都来自ContactAnchor，不接纳ReleasingSwing；
- 双脚目标沿ComponentUp高度差不超过1厘米；
- 共同高度请求大于0。

负请求继续回动画，因此起始平地约−9毫米的双支撑请求不会让骨盆下压。普通Releasing、Foot、Reach观察、脚锁、IK和3Hz响应均未改变。

## 回放结果

- 直接前驱：`20260901-165834-116-4f8dec5c15b8469c98f6498774091a2b`
- 候选：`20260901-173426-115-81a3c9d1689c48159cbb5a0ff14d8e8d`
- Record：`43357ff3cd384e5cba75d2c31175b116`
- facts75／diagnosis44强校验通过，1043表现帧、2086脚行
- Proof 1044输入帧差异0；Program／Projection身份按本次行为重建而变化，比较器保持identity mismatch
- Proof副本SHA256：`483519883A108223C751C159ADCE179600AEC3707D3C7C3CF0DD0E1CF2A70B7F`
- Editor构建0警告0错误，build server已shutdown；Float32／Fixed产品正式重建，Console0错误

1036帧实际变化：

| 项目 | 前驱 | 候选 |
| --- | ---: | ---: |
| 骨盆状态 | Releasing | BilateralSupport |
| 骨盆目标 | 0 | +20.628毫米 |
| 骨盆输出 | +10.382毫米 | +12.804毫米 |
| 左腿伸展率 | 0.9175 | 0.9209 |
| 右腿伸展率 | 0.9823 | 0.9855 |

全部既有质量Target计数不变：总分84.2、穿透19/84、接触未贴合13/60、Stable Swing143/344、Path199/667、Contact418/1052、腿姿态26/58、Locked水平0/15。世界骨盆超过5厘米仍7次，最大64.895毫米不变。Solved Knee步长超过10厘米144→143，最大648.299毫米不变。

## 边界

1037帧开始右脚不再使用ContactAnchor，候选立即退出BilateralSupport。当前Record没有持续数十帧的“双脚同台面且都保持ContactAnchor”尾段，所以下一步由用户可视测试确认稳定站姿；若仍半蹲，必须取得该持续尾段的数据，不能用1036单帧替代。
