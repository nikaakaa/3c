# Swing当前支撑高度实验设计

## 唯一变量

将实际Swing的地形高度职责从预测路径采样转给同帧CurrentSupport。目标和安全下限必须一起迁移，不能通过取消下限、临时夹值或只覆盖最终输出制造效果。

## 输入、处理、输出

输入为同一Foot事务的动画Sole/Ankle、正式FootHeight、ComponentUp及带Frame/Completion/Side/World身份的CurrentSupport。

```text
zeroClearanceSole = CurrentSupport.Target.Position
baseHeight = dot(zeroClearanceSole, ComponentUp)
rawHeight = baseHeight + FormalFootHeight
rawCorrection = ComponentUp * max(0, rawHeight - animatedSoleHeight)
```

CurrentSupport.Target.Position是现有Rig/两Probe解析的Sole目标，不是selected hit.point的改名。本轮保留该解析算法及其已知局限，不把它称为ZZZ多点几何复原。

可见Swing继续动画XZ。普通Swing安全下限采用同一个zeroClearanceSole，不能继续使用另一个位置处的预测包络；正常动画FootHeight不是新增滤波目标。当前支撑不完整时发布typed unavailable，不把旧支撑、默认Up、另一脚或预测包络补成当前支撑。

## 目标历史与身份

目标高度历史跟随当前支撑基准，而不是未来落点高度。表面/支撑几何换代沿同一Owner发布Revision；正常动画XZ位移和纯规划Path身份更新不单独重捕可见Residual。既有Target Height模式、World Residual同帧推进及位置/方向响应参数不变，但其输入与诊断必须完整改为新来源。

GroundPath的事件与查询结果仍独立保存并用于未来Landing规划及质量定位，不伪装为当前Support的位置lineage。现有当前接触拥有目标的判定、Contact/Locked处理与Release完整世界交接保持原有归属。Release末端接入同一新的Swing目标；缺失来源按现有typed不可用合同处理，不造成功数据。

## 不混入的变量

不加入ZZZ的g、k、W或kneeState，不改位置响应轴，不改变FootHeight曲线、预测速度、查询形状、Slope、脚旋转权重、Bend权重和骨盆响应。不恢复强制膝角后处理，也不为了保住某个窗口重新安装Reach硬夹紧。

## 诊断与验证

统一Diagnostics明确分开规划Envelope和可见Swing Height Reference；目标、安全下限、捕获/推进及可用性公式都读同一正式来源。旧包不补列、不换标签重发；既有质量阈值与评分规则不变，必要版本迁移独立提交。

同输入先核对Body、动画、事件、时钟、查询与旧规则基线；再核对R825–827及全部深折叠窗口、膝侧、脚底实际高度、穿透、跨阶、接触交接、Release、FullAnchor/Sliding及骨盆。不能只看最高膝步下降，也不能把“脚低于规划平面”当成已测Collider穿透。

本轮目标只是判断这一职责迁移是否改善柔和度而没有新增已认可结果的回归，不宣称完成ZZZ整体迁移。失败时精确撤销本候选及其诊断迁移，保留膝向修复和所有原始证据。
