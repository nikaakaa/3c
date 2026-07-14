## MODIFIED Requirements

### Requirement: Timeline 编辑器预览使用管线预览会话

系统 MUST 使用 editor-only TimelinePreviewSession 作为 TimelineEditorWindow 的播放、暂停、速度和游标预览控制器。TimelineEditorWindow MUST为当前绑定的 resolved TimelineData 建立唯一 preview session，并在窗口重绑或释放时正式释放旧 preview owner。TimelineEditorWindow MUST NOT直接控制 TimelinePlayer、PlayableGraph 或旧 Timeline autonomous playback。

#### Scenario: inline Timeline 窗口点击播放

- **WHEN** 用户从 TimelineNode 打开 inline Timeline 并点击播放
- **THEN** TimelineEditorWindow 的 TimelinePreviewSession MUST使用该节点的 resolved TimelineData clone
- **AND** session MUST NOT修改 TimelineNode 内的 authoring data
- **AND** page MUST NOT调用旧 TimelinePlayer

#### Scenario: shared Timeline root page 点击播放

- **WHEN** 用户直接打开 shared TimelineAsset 并点击播放
- **THEN** TimelinePreviewSession MUST使用 TimelineAsset.Data 的 runtime clone
- **AND** preview controls MUST与 inline TimelineEditorWindow 使用同一实现
- **AND** shared TimelineAsset MUST不保存 preview time 或 target

#### Scenario: TreeClip 跨窗口下钻

- **WHEN** 用户从 TimelineEditorWindow 打开 TreeClip Graph page或在 Graph 窗口返回
- **THEN** TimelineEditorWindow 的 preview session MUST保持归属当前 Timeline 窗口
- **AND** Graph 页面切换 MUST NOT创建、接管或释放 Timeline preview session

### Requirement: Timeline 资产不保存编辑器播放状态

系统 MUST 将编辑器预览播放状态保存在 TimelinePreviewSession 中。Inline TimelineData、shared TimelineAsset 及其持有的 TimelineData MUST只保存 authoring 数据，不得保存当前预览目标、session identity、runtime clone、PlayableGraph 或预览播放状态。

#### Scenario: 两个页面预览同一个 shared Timeline

- **WHEN** 两个作者页面预览同一个 shared TimelineAsset
- **THEN** 每个页面 MUST拥有自己的 preview session 时间、runtime clone 和播放状态
- **AND** 一个页面的播放、暂停、seek 或关闭 MUST NOT改写 TimelineAsset 或另一个页面状态

#### Scenario: 预览 inline Timeline

- **WHEN** TimelineNode inline TimelineData 被预览
- **THEN** preview session MUST从 authoring data 创建独立工作副本
- **AND** Track runtime、TreeClip runtime 和当前 time MUST NOT写回 RootTree asset
