# Change: 将Swing可见高度与当前脚下支撑对齐

## 当前状态

待用户确认具体取舍。只完成提案，未修改Runtime、配置、诊断或Unity，未开始候选Replay。

## Why

205014已保留有符号膝向修复，但仍有深折叠大步。R825当前Heel/Toe命中3.42米，当前支撑解析出的零净空Sole目标为3.456447米；用于Swing高度的预测包络采样点却距实际脚约0.903米、高3.762314米，诊断为OutsideGroundPathCorridor。包络加FootHeight后把脚额外抬高约441毫米。这里有空间参考不同的事实，不等于所有预测包络错误，也不能把低于该包络平面等同真实Collider穿透。

ZZZ普通非预测路径先形成当前支撑候选，再响应地形修正。本提案只对照这一输入职责，不声称复刻其完整多点查询、局部Foot pivot公式或SmoothKnee。

## What Changes

- 可见Swing高度基准及同帧安全下限统一使用现有CurrentSupport的零净空Sole目标；继续叠加正式FootHeight、保持动画XZ。
- GroundPath仍由LastLanding/NextSwingLanding构建、查询并发布规划证据，但不再直接抬高当前Swing脚，也不因路径走廊外状态临时切另一种高度算法。
- 同步迁移目标高度历史、Revision来源、状态目标及正式诊断；不保留“新目标来自CurrentSupport、旧包络又抬回去”的双重控制。
- 保持Contact完整世界残差、Anchor、已修膝向、脚旋转政策、所有速度及骨盆算法。不加入g、普通kneeState映射或SmoothKnee后处理。

## 业务取舍

可能减少与当前脚位不对应的提前抬高，缓解深折叠；同时可能减小跨台阶的预抬余量，必须验证脚尖踢阶、穿透、Landing交接和Release。脚目标改变仍可能经原双脚输入间接改变骨盆，因此“不改骨盆代码”不等于“骨盆数值一定不变”。出现已认可Foot/Pelvis行为回归则撤销本候选。

## 现行合同对照

- current spec的Ground Envelope要求保留完整查询、Edge拒绝和上侧凸包，但其当前脚地面下限职责需要收窄为规划下界；对应delta在本change内。
- active stabilize-character-foot-path-and-landing中旧Swing包络加FootHeight、包络安全下限及相关Target Height/诊断公式与本提案冲突。获批实施时必须同步这些精确条款，不让新旧MUST并存；当前没有覆盖用户正在修改的proposal.md或project.md。
- 唯一Foot/Goal/FBBIK/Writer和根Bank合同不变。缺失CurrentSupport不借旧结果或预测包络补成成功，不新增查询器、fallback或第二响应链。

## Impact

现有Swing目标Builder、State Target、Interpolation、Hard Constraint、Module调用及统一Diagnostics。范围为Corin实验，不更改TrainingEnemy作者资产，不恢复Reach，不新建测试或独立分析器。

固定比较205014及其205014/replay-proof.json，输入43357ff3cd384e5cba75d2c31175b116。诊断迁移后先建立同Runtime的新规则基线，再做同输入候选。已有数据原地保留，普通结果只更新一处实验Markdown。
