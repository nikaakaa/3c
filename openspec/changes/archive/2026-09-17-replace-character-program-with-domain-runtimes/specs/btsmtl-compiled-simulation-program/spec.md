## ADDED Requirements

### Requirement: 技能产物与角色领域状态必须独立发布和绑定

技能 MUST独立发布；Control、BodyMotion、Input、Effect、Equipment 配置与状态 MUST由各自模块拥有，角色绑定在 Session Active 前检查完整引用和能力。技能局部状态 MUST包含图流程、Blackboard、调用帧与计时；Timeline播放状态由正式Timeline Runtime独立拥有并参与同一角色恢复；角色快照 MUST组合所有领域的同次提交状态。纯 Pose 或资源问题 MUST不阻止合法技能构建，但非法角色资源绑定 MUST阻止对应角色运行。

#### Scenario: Pose配置缺失但技能合法
- **WHEN** 作者构建一个依赖完整的技能，而某角色的动画配置无效
- **THEN** 技能构建 MUST可以独立完成，角色实例准备仍 MUST报告动画绑定错误

#### Scenario: 多角色使用不同装备配置
- **WHEN** 相同技能分别绑定两个合法角色装备目录
- **THEN** 技能数据 MUST复用，装备状态和整体玩法一致性身份 MUST分别归各角色正式模块


### Requirement: Timeline内容必须作为独立直接数据交付

Timeline发布 MUST只保存正式轨道／片段字段、时间区间、稳定身份和资源／TreeClip图引用；内容校验、portable导出和目标数值准备 MUST不生成时间轴IR或操作表。技能发布只记录实际内容依赖，独立Timeline调用使用同一数据与运行入口。旧独立Timeline IR／Program构建入口 MUST迁移为内容发布，不能因此删除独立调用能力。

#### Scenario: 普通DotNet加载独立Timeline
- **WHEN** 调用方提供合法portable时间轴内容、资源和上下文
- **THEN** 正式Timeline Runtime MUST直接调度该内容，不加载Unity对象或生成临时Program
