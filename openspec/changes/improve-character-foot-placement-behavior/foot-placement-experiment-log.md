# Foot Placement行为改进关键经验

每次成功或失败实验由Git提交记录。本文只维护跨实验仍成立的规则，不保存逐轮帧段和完整指标。

## 目标数学

1. Swing FootPath只使用非负动画高度增量：`max(0, Envelope - Baseline)`。
2. Swing不得直接追未来Landing高度。
3. 未来地面目标不能在接触前把腿向下拉到不可达位置。
4. `8fc704a`仍使用Phase和`landingConstraintWeight * baselineHeightError`，不能作为最终空间进度或纯非负FootPath已经完成的证据。

## 状态与控制权

1. PlantConfidence只可作为Editor接触分析证据，不能成为Runtime Trigger或连续插值Alpha。
2. 实时Path Target不得拥有最终硬约束权限。
3. Prediction Point、Contact Patch与Contact Anchor必须分型。
4. 平滑结果是否Settled不能决定Plant事件是否成立。
5. Locked Anchor必须只有一个位置Owner，且锁定后不可被Path更新。
6. Landing与Locked之间不得重复捕获起点或叠加多次平滑。
7. Contact Patch必须有有限SupportDomain；无限平面会把错误踏面伪装成合法Anchor。

## 腿与骨盆

1. Landing腿可达和Pelvis支持不能依赖Locked先成立，否则会形成循环依赖。
2. Goal正确不代表物理脚正确，必须分别检查Goal、FBBIK Solved和Physical Writer。
3. 腿目标伸展率超过1时，不得继续用平滑掩盖不可达目标。

## 实验纪律

1. 一轮只修改一个主要行为变量。
2. 修改前写出可证伪预测和唯一验收指标。
3. 失败实验先提交为失败记录或直接回退，不继续叠加修改。
4. 引用动画权重作为进度前，必须先用CSV证明它在同一Event内真实单调。
