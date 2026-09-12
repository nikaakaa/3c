## MODIFIED Requirements

### Requirement: Curve Key编辑必须无损且原子

Curve Lane MUST支持单选、Shift追加、框选、双击或右键新增、一个或多个key拖动、Delete或右键删除、复制粘贴、数值Inspector以及Auto、Clamped Auto、Linear、Constant、Free和Weighted tangent编辑。横轴 MUST通过descriptor在Timeline frame与curve local time之间映射，并按整数Timeline frame吸附；纵轴 MUST按typed value domain处理。一次手势或Inspector提交 MUST只修改本地完整curve草稿并通过descriptor MutationAdapter生成一个Undo事务。Pointer Cancel MUST丢弃草稿；Pointer Up或意外Capture Out MUST提交最后草稿。提交后 MUST重新读取owner并刷新Timeline、Inspector、领域validation、Projection stale状态和可用Authoring Preview。

Curve mutation MUST原子保存pre/post wrap mode及每个key的time、value、in/out tangent、in/out weight和WeightedMode。Curve key不获得持久AuthoringId；Editor MAY在当前owner revision内使用临时key index选择，跨编辑会话的C#资产修改 MUST以`OwnerAuthoringId + ChannelId + Full Curve`替换完整channel，不得按key index跨revision修改。

#### Scenario: 拖动多个curve key

- **WHEN** 作者框选多个key并拖动
- **THEN** pointer capture期间 MUST只更新本地curve草稿
- **AND** 所有key MUST按相同Timeline frame delta和值delta移动并保持合法顺序
- **AND** 释放时 MUST只产生一个Undo事务

#### Scenario: 精确编辑weighted tangent

- **WHEN** 作者在Inspector修改一个key的in/out tangent、weight与WeightedMode
- **THEN** MutationAdapter MUST原子保存完整Keyframe字段
- **AND** 未修改的key与wrap mode MUST无损保留

#### Scenario: 复制到不兼容channel

- **WHEN** 作者把unbounded Position key粘贴到bounded Weight channel
- **THEN** Editor MUST拒绝该操作并说明time/value domain不兼容
- **AND** MUST不Clamp、不换算单位也不部分写入

#### Scenario: 外部修改使key选择过期

- **WHEN** owner curve revision在编辑手势外被C#代码或其它正式入口替换
- **THEN** Editor MUST使临时key选择失效并重新读取完整curve
- **AND** MUST不按旧key index写入新revision
