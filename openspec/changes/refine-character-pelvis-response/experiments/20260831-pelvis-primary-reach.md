# 主支撑Reach准入实验

## 对照与改动

- 直接前驱：f2fd8a2，141106／facts66；固定193957仍是Foot效果对照。
- 历史代码：91758ff7da8bc18f8716218a1ea13ec9c000061c。仅参考Accepted主支撑硬边界、Pelvis Release无Reach；不恢复旧目标、动画弯曲余量、0.12秒退出或旧旋转。
- 当前候选：全腿请求及交集继续测量；只有Primary执行骨盆硬边界及末端Foot径向投影。Release保留真实Up和原Spring回零，无历史不启动Pelvis。每脚Landing完成按本腿实际加权位移后的几何核对，与执行权限分开。
- 不变：Contact／Swing／Anchor／FootHeight／Goal权重／旋转、3Hz、20毫米余量、共同高度公式、Bend、Solver和37质量规则。

## 事前核对

冻结141106每帧上一Spring状态的单步控制复算误差最大2.61e-8米；不是完整候选递推或Replay。219帧输出可能改变，137条非主腿会超过20毫米余量边界，其中25条超过真实腿长1毫米。420帧旧输出−1.674毫米，单步候选+33.474毫米，左腿超真实长度约10.123毫米；322仍由主脚夹到−131.074毫米，989原自由回落不变。

因此本步不能承诺只解除“多余”下压。必须同时检查实际Foot Goal残差、Heel／Toe与腿长；Solver成功不等于最终骨骼到位。若解除下压变成脚够不到、膝盖放大或已认可Foot回归，不追加降权／夹脚掩盖。

当前CSV缺同Completion完整PoseRoot矩阵，不能把组件域FinalGoal与世界域Resolved Ankle直接相减。本轮不加列：严格核对权限、逐腿几何、原Clamp事实及组件域Goal到实际Ankle，世界预后Goal通用重算明确未覆盖，不借Physical或单位scale补值。

## 运行结果

Runtime e0e9678、Diagnostics 8f8c28a（68/37，无新CSV列）。完整Editor规定flags构建57既有／依赖警告0错误、121.16秒并shutdown；显式Refresh后Console零错误。

首包152254已产生2086脚行，原始数据留在`Diagnostics/FootPlacementRuns/20260831-152254-812-a4a8198d9fef4d0eaf039ec9f1ce4fcb`。Finalizer在R21的原压缩余量检查失败：Runtime正式值为max(0,L−D)=0，旧Analyzer要求−1.351毫米；骨长只差0.596微米。5dfa6d1仅将三项期望对齐max0，1毫米容差与质量规则不变，完整Editor57既有／依赖警告0错误并shutdown。原包未重写，尚无analysis／Proof，不报正式评分通过。

原始CSV同帧／输入／OriginalSole对齐后：世界骨盆超过50毫米大步33→22，420下降80.210→49.328毫米；静止平地修正均值−22.278→0毫米、移动平地−15.355→−7.328毫米。但322／466的主支撑硬下压不变，989释放回落仍约63.261毫米；不是全链路消除突降。

真实代价：实际脚踝Goal误差超过1毫米26行、超过10毫米5行；有非零Goal的骨盆偏差超过1毫米47帧。L215目标超完整腿长37.581毫米，实际脚误差21.201毫米，骨盆相对预定位移(+21.271,+7.349,−7.696)毫米，范数23.783毫米，不能把范数称为下陷量。420目标超腿长13.934毫米，Solver让左Hip移动约16.905毫米，骨盆相对目标Y又下降3.517毫米；421实际脚误差已达1.591毫米。Goal、Resolved位置及两段骨长未被偷偷修改，原脚链不变并不保证实际脚仍达目标。

原因：PelvisPreSolveTranslation只是求解前平移；FBBIK的spine mapping之后仍重建骨盆，limb mapping按原骨长产生真实脚端。FinalIkPelvisPositionResidual由现代码固定赋0，不是最终骨盆误差；唯一Writer与Solver骨盆仅微米差，额外位移发生在Writer前。首包已反证把取消非主脚Reach直接视为安全修复，当前不接纳。Unity后来处于非本任务拥有的Play，已询问是否可结束；不擅自中断，等待正式复验与精确撤回本候选。正确的max0诊断修复单列保留。

后续只在本MD追加必要路径、关键窗口与处置；普通小步不复制原包或制作ZIP。
