# Foot Placement关键经验

Git记录保存逐轮成功与失败。本文只保留数据阶段仍成立的规则。

## 数据与行为边界

1. 先证明动画数据，再修改Runtime消费者。
2. Step Time、Step Distance、Foot Height与Lock Scenario必须能在原生AnimationClip直接检查。
3. Contact、Lock与Support不是同一曲线的不同名字。
4. Foot Forward来自原动画骨骼，不创建第二位置数据源。
5. Prediction Point、Ground Path、Contact Anchor和Pelvis只属于Runtime，不得写入AnimationClip。

## 实验纪律

1. 一轮只接入一类数据消费者。
2. 修改前写出可证伪预测和唯一指标。
3. 数据候选必须显式Apply，不自动改变行为。
4. Goal、FBBIK Solved和Physical Writer必须分别观察。
5. TrainingEnemy不参与当前数据生成和验收。
