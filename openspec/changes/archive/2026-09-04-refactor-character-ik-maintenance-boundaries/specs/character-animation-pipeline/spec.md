本差量的源码与行为对照固定为用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`；233436仅是对应回放证据，不能用当前HEAD或采样目录替代源码基线。

## ADDED Requirements

### Requirement: FBBIK初始化和清空历史必须使用正式方向准备结果

FBBIK方向初始化 MUST由现有Rig参考姿态与Profile准备过程精确取得，并保存为项目拥有、身份匹配的typed只读准备结果。准备阶段无法得到合法几何方向时 MUST明确失败，不新增默认世界轴、近似初值、Transform名称搜索或另一套初始化算法。

参考初值与已发生的运行历史 MUST分型。准备结果存在 MUST不能把Stable/Applied历史有效标记提前置为已成立，也不能让可靠动画首帧额外接受历史符号限制；只有实际正式求值产生的方向才能随根Bank成为运行历史。

明确清空Solver历史的初始化、Reset及调参清历史路径 MUST使后续方向仅由当前Pose、Goal、Profile、正式准备结果与根Bank BendHistory决定。Solver MUST在调用Vendor Update前由这些正式输入设置Vendor工作字段，不能在历史为空时读取Vendor上次留下的`bend.direction`或其它可变字段作为历史。

本要求 MUST不扩大Foot Reset与Solver Reset的现有触发范围。普通已有历史帧的方向符号、稳定化算法及权重政策 MUST保持；完全Reset后不再继承旧Vendor方向属于独立行为修正，必须与结构迁移分开记录。

普通帧基线 MUST采用233436恢复的205014保留行为：可靠动画通过原腿轴到目标腿轴的旋转运输有符号膝向，Stable保存运输前动画方向，Applied保存实际请求；退化时继续原有历史分支。初始化准备数据的校验 MUST集中在现有准备入口，不在每层重复验证相同Rig和有限值条件。

#### Scenario: 新建与完全Reset面对同一退化腿姿态

- **WHEN** 新建Runtime与已运行后完全Reset的Runtime具有同一正式准备结果、当前Pose、Goal和Profile，且当前动画腿不足以给出可靠弯曲方向
- **THEN** 两者 MUST从同一正式初始化方向建立本帧方向输入
- **AND** Reset前动作留下的Vendor方向 MUST不影响结果

#### Scenario: Pending求解后未发布

- **WHEN** Vendor已经处理Pending方向但后续阶段未能成功Seal
- **THEN** 下一次合法求解 MUST由上一Committed BendHistory或正式初始化状态重建Vendor输入
- **AND** MUST不继承被丢弃的Vendor工作字段

#### Scenario: 普通历史帧

- **WHEN** 上一Committed BendHistory合法且本帧不触发完全Reset
- **THEN** 本次所有权迁移 MUST保留已接受的有符号膝向运输、退化分支和权重数学
- **AND** MUST不恢复可靠动画半球强翻或已否决的SmoothKnee后处理

#### Scenario: 首帧动画方向可靠

- **WHEN** 正式参考初值已准备但尚无Committed运行历史，当前动画能提供可靠方向
- **THEN** Solver MUST保持原首次帧动画方向选择
- **AND** MUST不把参考初值伪装成上一帧方向后额外翻转或限制本帧方向

#### Scenario: 普通退化帧不能冒充Reset覆盖

- **WHEN** 回放中的退化腿姿态具有合法上一Committed历史
- **THEN** 该记录 MUST只用于普通历史分支的行为保持对账
- **AND** MUST不据此声称已验证空历史初始化或完全Reset边界
