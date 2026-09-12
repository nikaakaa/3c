## Why

Skill运行观察已有代码，但实例选择、共享子图调用隔离、暂停/退出释放与采样开销仍缺完整接入和证据。这些工作独立于原生FSM资产迁移，拆出后作者层变更不再长期承担观察系统收尾。

## What Changes

- 接收旧变更6.4.2、7.2.2、7.3、7.4、7.5.2，继续补齐真实Session/Actor/Skill释放/generation/调用路径的选择与导航。
- 保留瞬时经过、持续等待和终态；释放结束不混入下一次释放，共享子图不同调用不得串线。
- 补齐实际值采集、有界缓存、覆盖/缺失标记、暂停保留与关闭/退出解绑；记录关闭、节点观察、值观察三档实际开销。
- 图只读取正式Program诊断；原生FSM来源映射由integrate-native-fsm-skill-authoring提供，不启动插件图、任务或预览执行器。
- 普通Play观察可独立使用；受控场景启动/停止/重建仍由rebuild-btsmtl-preview-with-scene-play拥有，本change不接管场景生命周期。

## Capabilities

### New Capabilities

- `btsmtl-flowcanvas-runtime-observation`：接收旧change未安装的完整观察合同，保持原稳定能力路径与唯一运行源。

### Modified Capabilities

无。

## Impact

只影响Skill观察适配、实例选择、原生图显示和既有诊断订阅。网络Pass/Adapter及Corin运行/Replay证据由integrate-corin-dump-authoring-replay提供；这里消费纠正后的正式身份，不实现网络传输。旧实现和历史证据保留，未完成项保持未勾选；本次不新增测试、不开Unity、不修改代码。
