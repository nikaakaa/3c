# Tasks

## 1. 锁定依赖

- [x] 1.1 确认`refactor-animation-control-boundaries`的Action Playback、AnimationSlot、三层时间、Preview与diagnostics代码合同已经稳定。
- [x] 1.2 读取该change最终Action Playback合同。
- [x] 1.3 读取该change最终AnimationSlot合同。
- [x] 1.4 读取该change最终三层时间合同。
- [x] 1.5 读取该change最终Animation Preview合同。
- [x] 1.6 读取Action Authoring current spec。
- [x] 1.7 读取Timeline Editor current spec。
- [x] 1.8 确认旧Playback总管与兼容入口已经删除。
- [x] 1.9 确认Pose Graph重构已经安装共享Editor Shell与Capability驱动节点UI。
- [x] 1.10 确认Document v3 typed mutation与owner解析合同已经稳定。
- [x] 1.11 确认Corin最终资产迁移尚未开始。
- [x] 1.12 按`openspec/character-pipeline-serial-execution.md`确认本change处于共享UI之后、Corin迁移之前。

## 2. 定义Workspace typed合同

- [x] 2.1 定义Workspace identity。
- [x] 2.2 定义typed open request。
- [x] 2.3 定义Definition context。
- [x] 2.4 定义ActionProfile context。
- [x] 2.5 定义Action Context call-site context。
- [x] 2.6 定义finite Timeline context。
- [x] 2.7 定义Action producer context。
- [x] 2.8 定义Presentation binding context。
- [x] 2.9 定义AnimationSlot consumer context。
- [x] 2.10 定义Runtime Debug binding。
- [x] 2.11 定义Preview target context。
- [x] 2.12 定义typed failure reason。

## 3. 实现owner关系解析

- [x] 3.1 从Definition解析ActionProfile。
- [x] 3.2 从ActionProfile解析Action identity。
- [x] 3.3 从Gameplay Graph解析Action Context call site。
- [x] 3.4 从call site解析有限Action Timeline。
- [x] 3.5 从Timeline解析Action Animation producer。
- [x] 3.6 从Profile解析producer binding。
- [x] 3.7 从Pose Graph解析AnimationSlot consumer。
- [x] 3.8 校验关系唯一性。
- [x] 3.9 校验stable identity。
- [x] 3.10 禁止按显示名解析。
- [x] 3.11 禁止读取generated artifact补全authoring。
- [x] 3.12 输出可导航的typed错误。

## 4. 建立Workspace窗口

- [x] 4.1 创建Character Editor窗口。
- [x] 4.2 接收typed open request。
- [x] 4.3 创建Definition选择区。
- [x] 4.4 创建Action选择区。
- [x] 4.5 创建Timeline标题区。
- [x] 4.6 创建Preview模式入口。
- [x] 4.7 创建Live模式入口。
- [x] 4.8 保存窗口级selection。
- [x] 4.9 保存窗口级折叠状态。
- [x] 4.10 禁止保存业务配置。

## 5. 嵌入Timeline Editor Core

- [x] 5.1 构造Timeline typed session context。
- [x] 5.2 嵌入主时间轴视图。
- [x] 5.3 转发Track selection。
- [x] 5.4 转发Clip selection。
- [x] 5.5 转发TreeClip selection。
- [x] 5.6 转发Marker selection。
- [x] 5.7 转发Curve selection。
- [x] 5.8 保持Timeline mutation transaction。
- [x] 5.9 保持Timeline owner Undo。
- [x] 5.10 禁止Character Workspace复制Timeline数据。

## 6. 建立Details面板

- [x] 6.1 创建Identity页。
- [x] 6.2 创建Gameplay页。
- [x] 6.3 创建Animation页。
- [x] 6.4 创建Slot与Blend页。
- [x] 6.5 创建References页。
- [x] 6.6 显示ActionProfile owner。
- [x] 6.7 显示Gameplay Graph owner。
- [x] 6.8 显示Timeline owner。
- [x] 6.9 显示Presentation Profile owner。
- [x] 6.10 显示Pose Graph owner。

## 7. 建立typed mutation路由

- [x] 7.1 路由ActionProfile mutation。
- [x] 7.2 路由Gameplay Graph mutation。
- [x] 7.3 路由Timeline mutation。
- [x] 7.4 路由Presentation Profile mutation。
- [x] 7.5 路由Pose Graph mutation。
- [x] 7.6 保持各owner独立Undo。
- [x] 7.7 禁止Workspace镜像写入。
- [x] 7.8 禁止跨owner隐式批量修改。

## 8. 建立导航

- [x] 8.1 从Workspace打开ActionProfile。
- [x] 8.2 从Workspace打开Gameplay call site。
- [x] 8.3 从Workspace定位Timeline Track。
- [x] 8.4 从Workspace定位Timeline Clip。
- [x] 8.5 从Workspace定位TreeClip Window。
- [x] 8.6 从Workspace打开producer binding。
- [x] 8.7 从Workspace打开AnimationSlot。
- [x] 8.8 从Workspace打开Blend Policy。
- [x] 8.9 从ActionProfile增加Workspace入口。
- [x] 8.10 从Gameplay Graph增加Workspace入口。
- [x] 8.11 从Timeline增加Workspace入口。
- [x] 8.12 从Pose Graph增加Workspace入口。

## 9. 显示三层时间

- [x] 9.1 定义Action Logic Time view model。
- [x] 9.2 定义committed raw sample view model。
- [x] 9.3 定义Projected Presentation Time view model。
- [x] 9.4 定义Marker Effective Time view model。
- [x] 9.5 显示Simulation Tick。
- [x] 9.6 显示Presentation Frame identity。
- [x] 9.7 显示sample provenance。
- [x] 9.8 显示interpolation或extrapolation状态。
- [x] 9.9 显示Marker relation。
- [x] 9.10 禁止合并为可写Playback Position。

## 10. 接入表现Preview

- [x] 10.1 复用正式Animation Preview Runtime。
- [x] 10.2 构造Action Playback fixture。
- [x] 10.3 构造Base Pose fixture。
- [x] 10.4 解析AnimationSlot plan。
- [x] 10.5 解析Transition Routing plan。
- [x] 10.6 解析完整Pose Plan。
- [x] 10.7 解析Rig。
- [x] 10.8 按Presentation Delta推进Preview。
- [x] 10.9 显示Action Pose。
- [x] 10.10 显示Slot输出。
- [x] 10.11 显示Blend或Inertialization状态。
- [x] 10.12 显示Final Pose。
- [x] 10.13 禁止Preview创建Simulation Session。
- [x] 10.14 禁止Preview提交Gameplay输出。

## 11. 接入Live Debug

- [x] 11.1 读取匹配revision的RuntimeDebugSession。
- [x] 11.2 显示ActionInstance。
- [x] 11.3 显示Action lifecycle。
- [x] 11.4 显示committed Timeline sample。
- [x] 11.5 显示projected presentation sample。
- [x] 11.6 显示Marker effective sample。
- [x] 11.7 显示Playback lifecycle。
- [x] 11.8 显示AnimationSlot route。
- [x] 11.9 显示BlendStack或Stored Pose。
- [x] 11.10 显示Inertialization residual。
- [x] 11.11 显示Final Pose贡献。
- [x] 11.12 显示当前Numeric Target identity。
- [x] 11.13 拒绝stale Trace关联。
- [x] 11.14 禁止Live模式mutation。

## 12. 保持显式Build边界

- [x] 12.1 复用现有Dry Run显式命令。
- [x] 12.2 复用现有Build显式命令。
- [x] 12.3 显示精确Definition。
- [x] 12.4 显示请求Numeric Target。
- [x] 12.5 禁止打开窗口触发Build。
- [x] 12.6 禁止切换Action触发Build。
- [x] 12.7 禁止mutation触发Build。
- [x] 12.8 禁止selection触发Build。
- [x] 12.9 禁止Preview触发Build。
- [x] 12.10 禁止Live Debug触发Build。
- [x] 12.11 禁止asset import触发Build。
- [x] 12.12 禁止Build后自动选中生成资产。

## 13. 收口旧入口与文档

- [x] 13.1 删除重复有限Action动画导航入口。
- [x] 13.2 删除Workspace私有业务配置类型。
- [x] 13.3 更新ActionProfile导航说明。
- [x] 13.4 更新Timeline领域上下文说明。
- [x] 13.5 更新AnimationSlot引用说明。
- [x] 13.6 更新三层时间调试说明。
- [x] 13.7 更新Float32与Fixed Live身份说明。
- [x] 13.8 确认没有新增运行时播放器。
- [x] 13.9 确认没有修改Program ABI。
- [x] 13.10 确认没有角色资产迁移。
