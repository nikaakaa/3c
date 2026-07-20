## ADDED Requirements

### Requirement: Timeline Field内部交互、几何与渲染必须分属明确模块

Timeline Editor MUST保留现有TimelineEditorWindow、TimelineField、Inspector和Authoring Preview/Live Debug入口，但selection/drag/move/resize交互状态、time/frame/clip geometry与hit-test、track/clip/playhead/overlay rendering、preview/live binding MUST由职责独立的内部模块拥有。selection MUST只读暴露，外部 MUST通过selection命令修改；interaction MUST只依赖窄host port，不得持有完整TimelineField；rendering MUST显式消费frame range、viewport、playhead或overlay输入，不得反向读取完整TimelineField。interaction模块 MUST通过唯一authoring mutation/Undo入口修改Timeline；geometry与rendering MUST是输入驱动且不得写asset；preview/live binding MUST继续由窗口本地session adapter拥有。拆分 MUST不改变Timeline/Track/Clip identity、Source Map、右侧Inspector selection或双窗口页签行为。

#### Scenario: Resize一个Animation Clip

- **WHEN** 作者拖动Clip边缘改变范围
- **THEN** interaction模块 MUST使用geometry模块的frame结果创建唯一mutation
- **AND** mutation MUST在一个Undo边界更新原Clip identity对应的数据
- **AND** rendering模块 MUST只根据新数据重绘

#### Scenario: 点击右侧Inspector设置

- **WHEN** 作者选择Clip后操作右侧Inspector字段
- **THEN** selection owner MUST在字段提交期间保持同一Clip authoring identity
- **AND** TimelineField重绘 MUST不把selection清空或切换到其它Clip

#### Scenario: Authoring Preview切换Live Debug

- **WHEN** TimelineEditor从Authoring Preview切换到Live Debug
- **THEN** window/session adapter MUST停止preview binding并建立该窗口本地runtime binding
- **AND** interaction模块 MUST进入只读状态
- **AND** geometry与rendering模块 MUST复用同一authoring Timeline identity显示真实overlay

#### Scenario: 多个playback overlay

- **WHEN** 同一Timeline source存在多个runtime playback
- **THEN** runtime overlay模块 MUST呈现各playback identity并服从Follow/Pin选择
- **AND** rendering模块 MUST不按列表顺序静默选择赢家或调用preview evaluator
