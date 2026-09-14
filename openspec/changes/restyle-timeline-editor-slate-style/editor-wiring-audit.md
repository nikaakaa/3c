# Timeline作者编辑接线审阅与剩余设计

## 1. 范围、证据和状态口径

2026-09-14按用户要求排查“还有什么没接好”并补全文档。读取开始HEAD为e140ae68d，审阅期间其它实现任务仍在共享工作树修改；结论指本次实际读到的函数，不等于某个不可变提交或Unity实跑通过。未修改代码、资产或运行配置，未执行Unity/Build，也未增加测试。

范围为打开/选择、Track/Clip编辑、局部曲线、源运动曲线查看、Inspector、Undo/刷新和预览导航。直接Timeline Runtime继续只按tasks第12节管理，本审阅不判定其运行、网络、角色装配或快照已经实现。

本文为当前作者UI问题的集中入口，补充slate-source-decoupling第13–14节并纠正其中过时的实现状态；原UI不重做、正式数据唯一、领域分工仍有效。后续实施只更新对应tasks，不再从聊天或旧完成描述拼接需求。

源码路径缩写（均相对Assets）：

- S：ParadoxNotion/SLATE Cinematic Sequencer/Design/Editor/Windows/CutsceneEditor.cs
- T：同插件Design/Editor/TrackEditorGUI.cs
- C：同插件Design/Partial Editor/CurveEditor.cs；D：同目录DopeSheetEditor.cs
- B：GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/BtsmtlSlateTimelineBinding.cs
- P：同目录BtsmtlSlateTimelineProjection.cs
- W：同目录Tree/TimelineEditorMainWindow.cs；I：同目录Tree/TimelineInspector.cs
- M：GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/TimelineData.Runtime.cs

## 2. 本次确认已接入或已修的部分

| 能力 | 代码证据 | 不能据此推定 |
|---|---|---|
| 无隐藏组件打开 | P构造函数创建binding及CutsceneEditorSurface，InitializeEmbedded(binding)，不再BuildProjection/AddComponent | 不代表绘制与交互只剩一套 |
| 左侧局部坐标 | S正式列表在trackRect内BeginGroup并传局部Rect | 不代表Track原全部控件/高度行为已复用 |
| Clip标题去重 | FormalClipEditorBinding.DrawClipGUI为空，标题归原wrapper | 不代表底部DopeSheet已接入 |
| 曲线数组入口初始化 | c167301eb在C的两个数组构造函数补this.posRect | 尚未取得修复后真实曲线视野证据，不继续把这两行列为未修改 |
| 三条Motion局部通道 | Curve Catalog注册Weight/Ease In/Ease Out，B从descriptor复制曲线，T调用原CurveRenderer和展开区DopeSheet | 不代表源XYZ/Yaw已显示，也不代表所有参数控件已恢复 |
| 选中Clip归属收窄 | B.Track.SelectedClip已从Owner.Selected及Track关联派生，不再维护m_SelectedClip副本 | Inspector仍用serialized路径；刷新、通道和空态仍需收口，不能把之前缺SetSelectedClip的快照当现状 |
| 正式失败回滚 | M.ApplyModify已建立Undo group，catch中RevertAllDownToGroup并更新serialized引用 | 不代表所有UI修改都经过此入口，也不等于只提交了实际编辑字段 |
| 正式新增与错误传递 | B.AddTrack/AddClip消费catalog及typed创建；AuthoringIssue接P再接W；新增弹窗失败保留输入 | 资源必填字段全覆盖、过期原因精确显示和运行效果不能仅凭菜单存在判断 |

以上正确代码保持；下面只列当前实际缺口和具体修正，不重写已有成果。

## 3. 逐项接线缺口

### A01 两份绘制主体和简化拖动仍存在

S的ShowGroupsAndTracksList(Rect)、ShowTimeLines(Rect)在embeddedTimeline非空时转同名重载并return。正式重载仍有自己的列表/Clip循环。原版拖动包含多选联动、Shift/Ripple、首尾磁吸及邻居处理；正式版只计算指针减formalDragOffset、单端吸附并Clamp到maxTime-length。

影响：看起来是Slate，整块拖动行为却不是完整原版；Clip不能按原操作延伸到当前内容末端之外，多选/推移不能仅凭wrapper里有标志就认为工作。

修正：原函数主体就地共用，保留原Rect、GUI.Window/DragWindow和事件算法，数据与业务谓词接正式binding/contract。Source的Reject/Parallel/Blend决定合法放置，不能用当前总时长或Slate组件类型代替。绑定StartTime/EndTime的行为也必须匹配操作：目前B.StartTime只改起点，原ActionClip起点移动会保持长度；移植原StartTime+=delta时若不处理这一差异会把移动变成裁剪。移动保持时长，裁剪才改对应边界。对应11.1/11.4。

### A02 Clip底部DopeSheet仍直接返回

S.ShowClipDopesheet先画底栏，FormalClip分支仍return；真实key只在原action路径画。正式HasActiveParameters仍会让wrapper扣出底栏高度。展开Track里的DopeSheet已经有接线，不能据此将Clip底部也标完成。

修正：底部与展开区使用原D的同一key逻辑和正式事务，分别传各自显示Rect/时间范围。无局部曲线不预留空底栏，有曲线不以标题或假key顶替。提交 `48a1d2a72` 已接通FormalClip底部DopeSheet；真实窗口验收仍未完成。

### A03 局部参数面板仍是简化版

T正式DrawParametersInfoGUI自己循环画通道、小按钮和CurrentValue标签；不像原参数工具那样提供完整Value编辑、齿轮与现有参数操作。B.Parameter.CurrentValue也只有getter。Track.FinalHeight固定展开为250，丢失原参数数量/拖动调高逻辑。通道颜色/值域/单位未完整送入CurveRenderer，C仍按曲线数组下标选RGB。

修正：参数列表、数值输入、key/切线工具与高度状态从原实现接数据，不新造面板。恢复正式支持的原操作；表达式/场景AddProperty等无正式合同的菜单不恢复。Value编辑须明确是当前key或待加key值，不偷偷给运行对象赋值。descriptor提供显示元数据，初次取景按实际key和值域；后续普通刷新不抢走作者纵向视野。对应11.2/11.6/11.16。

### A04 Root Motion只读查看缺失，不是源数据消失

B原先只枚举Timeline-local descriptor；提交 `6b35adc19` 已把MotionCurveClip现有源映射接到Timeline只读源区，包含XYZ/Yaw、源资产GUID、使用区间和Open Source入口。正式RootMotionCurveAsset与源区间仍是唯一源，真实窗口仍待验收。

本次需求收口：将之前“源曲线可选只读展示”改为必须能在Timeline查看。选中MotionCurveClip后，除了可编辑Weight/Ease，还要有明确的源XYZ/Yaw只读通道、源名称/使用区间与打开源入口。只读意味着不能在本窗口拖key写共享源，不意味着隐藏曲线。

显示使用现行正式源映射，把本次使用的源区间对应到Timeline作者时间；显示源区间终点与Clip结束的区别。源区间结束后保持累计终值，不能为了显示重新采样写回源或复制成局部四条曲线。使用原CurveRenderer的只读能力；若无法真正阻止修改，只能补原Renderer编辑资格入口，不能复制渲染器或允许改后再偷偷恢复。编辑源明确导航源owner；不重做已归档源迁移。对应新增11.18。

### A05 选择、空态和通道缓存仍需完整收口

B.SelectedClip现在已从统一选择派生；T仍把未选Clip、参数为空和零通道合并成No Clip Selected。S.formalInspectedParameters按TrackId存int下标，缺少ClipId/ChannelId，切换不同Clip可能把同一下标当作同一通道。TryGetValue直接out到初值-1的变量，失败时会变成默认0，这不等同“未选参数”。

修正：保持正式owner/ClipId为选择来源，参数选择使用稳定ChannelId；重建后解析仍有效通道，无效时明确清空。未选择、没有局部曲线、源只读、绑定失败分别提示。不能回头加m_SelectedClip第三份状态。与Inspector路径同步按同一身份解析，不用数组索引证明对象没变。对应11.15/11.16。

### A06 普通属性仍绕过正式typed提交

I.Rebuild绘制整个m_Data PropertyField，再绘制选中serialized路径的PropertyField；没有把这些字段编辑接B的Configure/Session.Apply。它能显示字段，但包含AuthoringId、OtherEase等身份/派生数据，不能作为设计中“原Slate通用控件+正式typed提交”已完成。TimelineInspectorSelection目前只有TimelineAsset Inspector消费，inline Graph owner的选中属性入口尚未在此链找到。

修正：原IN/OUT、Blend、源字段/类型字段控件写同一正式mutation，身份只读、派生字段只读或不显示。普通PropertyField若用于查看，不同时作为另一条可写保存入口。shared与inline必须落到各自真实owner，不为inline建假Unity对象；Graph Inspector共享文件保持原owner分工。对应11.7，不另加Timeline右侧面板。

### A07 每次提交回写全部Clip和曲线，混合字段也混淆

B.CommitEdit仍遍历binding集合做差异检查，但提交 `48a1d2a72` 已让HasChanges先过滤空手势，CommitSource只写变化的Start/End、SelfEase和曲线；构造binding改取SelfEase，不再把OtherEase当作者草稿。未改Clip只读比较，不回写字段或曲线。

影响：不是只有性能问题，可能改动作者没碰的混合字段，并让所有曲线做秒域往返。M的新增回滚机制无法解决“合法提交了不该写的字段”。

修正：上述差异门和SelfEase分离已经落地；原有失败回滚保留。曲线更新回调仍复用现有ApplyEmbeddedCommand入口，但实际提交由曲线等价比较限制到当前变化，真实窗口需继续确认没有额外Undo。

### A08 Track启停在正式Undo之前改源，锁定未贯通

B.Track.IsActive的直接源写入已从Slate按钮移除，提交 `48a1d2a72` 通过binding的SetTrackActive走ApplyImmediate和正式Undo；旧的属性setter仅保留为binding实现细节。Track锁定已合并到Clip有效锁定判断，但锁按钮/状态持久语义仍需继续收口。

Track.IsLocked是独立窗口字段，Clip.IsLocked也是独立字段；FormalClipEditorBinding只读Clip.IsLocked，未合并Track锁定。当前正式列表也没有完整恢复原禁用/锁定菜单，因此某入口即使能改状态，也不能推定Clip交互遵守。

修正：Muted走现有ApplyImmediate等正式命令，或先改草稿再一次提交，不能先改Source后验revision；Track锁定作为编辑资格贯穿该轨道Clip选择/拖动/裁剪与菜单，不产生新的业务锁字段。对应新增11.20。

### A09 Split/Trim/Copy不是只调整起止帧

B.SplitClip复制整个Clip并改Start/End，没有处理Animation ClipIn、运动源区间或局部曲线的左右段映射；原UpdateClipAdjustContents对子素材偏移的处理仍要求action is ISubClipContainable，正式binding没有该输入。提交 `48a1d2a72` 已把Copy改为复制瞬间捕获正式Clip副本，剩余Split/ClipIn/源区间语义仍未完成。

修正：在现有正式编辑合同明确移动/裁剪/缩放/切分对应的源区间、ClipIn与局部曲线含义；使用既有源映射，不能新写采样公式。涉及Motion/Warp共享字段由原owner提供必要操作，本任务接原手势和命令。一次Split完整创建两个合法使用区间并保留引用/新身份，未支持类型明确拒绝而非只改帧伪装成功。Copy在命令时捕获正式内容，后续Paste基于该副本并生成新身份；不保持失效UI binding当剪贴板。对应新增11.21。

### A10 刷新状态保存不完整，缓存释放仍有全局操作

P.CaptureViewState只保存第一个ShowCurves轨道，RestoreViewState把其它轨道全部折叠；Section选择不在该view state字段里。B.BuildBindings虽保存所有展开TrackId，P随后恢复单轨道状态仍会覆盖它。C/D cache key含scope，但S.ClearEmbedded调用无scope的ClearEmbeddedCache清空全部embedded缓存。W.OnEditorUpdate无条件MarkDirtyRepaint，B.RequestRepaint为空。

修正：现有view state保存全部必要展开与稳定通道/Section选择，避免两处恢复互相覆盖；缓存清理限定当前Surface。正常关闭/重开不影响其它真实Slate使用者。数据/选择/视野变化请求重绘，runtime观察按实际变化刷新；不要依赖全窗口持续重绘掩盖缺失通知，不额外建轮询系统。对应11.11及新增11.23。

### A11 时间尺已帧化，事件和工具栏仍有缺口

S正式拖动分支在isDragging时会计算并在数值变化后e.Use，没有先限定当前事件为MouseDrag，存在Layout/Repaint也走到消费的路径；这与截图警告一致，但未取得实时堆栈，不能声称它是唯一调用点。

工具栏仍是ShowEmbeddedAuthoringToolbar，当前有Add Track、前后帧、Fit和帧输入；没有原播放工具条，Space在embedded时明确消费后返回。W另有Document、Source与Timeline Ownership重复信息，窄布局没有完整收口。F既被窗口用作Fit Clip快捷键，也被曲线区标为Frame Selection，必须按焦点/事件归属区分，不能一次按键触发错误内容修改。

修正：回到原控件布局和事件入口，只替换正式命令。编辑帧/步进保留；Play具体是正式预览命令还是本地游标推进未明确的部分单列决策，不能恢复Slate播放或声称按钮已具备。先修确实错误的Layout消费和快捷键抢占，不改动运行范围。对应11.17与布局原任务。

### A12 Section与创建/导航只能算部分接通

S正式Section菜单当前只有移到当前帧和删除，缺少原Edit名称/帧输入；创建固定名Section，不能把完整属性合同等同已有Add/Delete。拖动仍限制在当前length，边界延长语义需服从正式内容规则。

新增Track/Clip确有正式typed入口和回滚，但CreateTrack/CreateClip的过期分支仅Rebuild并false，弹窗显示“可能owner已更新”等泛化信息，字段缺项与过期原因不精确。表单只支持当前写出的类型字段分支，不能因catalog枚举到类型就声称所有必填字段已覆盖；新增类型不允许靠默认值凑可创建。

W的“运行控制：Skill Graph / Graph Shell”是标签，当前工具栏没有返回精确预览来源的按钮；OpenClip源导航是Selection/Ping，TreeClip部分入口仍走BaseTreeWindow。保留已有正式导航和runtime observation，但不能将文字说明或旧树窗口入口称为已完成新SkillGraph预览联动。

修正：Section姓名/帧用原编辑控件接正式ConfigureSection；创建错误按实际typed原因返回，输入保留；允许类型的表单字段按现行contract逐项接齐，缺公共字段接口只记录精确owner需求。预览导航用既有真实来源，不造角色/播放器，不为新领域Runtime复制预览协调器。对应11.5、6.3、7.3/9.3，新增11.22仅收口Section编辑。

### A13 手势取消的两层状态没有完全统一

S同时有editTransactionActive与B.m_EditActive。DopeSheet通过BeginEmbeddedEdit设置Surface状态；部分Clip手势/菜单却直接调用binding.BeginEdit。ClearEmbedded/OnDisable中的CancelEditTransaction只有Surface状态为true才转发Cancel，未覆盖所有直接开始的binding手势；当前窗口键盘处理也未找到Escape取消路径。

修正：在现有事务入口统一开始/提交/取消关联，让失去有效目标、明确取消和窗口关闭都丢弃未提交草稿，不能有一层认为已结束、另一层仍在编辑。只修现有状态接线，不新建事务管理器；需要按最终实际输入事件确认该路径，不推断它已经造成资产写坏。对应11.8/11.24。

## 4. 统一实施原则与依赖顺序

1. 同一原函数承载绘制与手势，先固定坐标、选择及编辑字段的真实含义，再接原控件；不能继续给正式简化版追补一串功能。
2. 已有正式mutation/回滚保持，只收口漏过它的直接写源与全量回写；数据正确性不能靠禁用Undo、重新载入全部资产或双写修补。
3. 局部曲线编辑和源曲线只读查看同时纳入作者体验，初次取景/通道切换/刷新各自含义明确。原Renderer不重做，源时间映射由正式owner提供。
4. Inspector、Section、复制/切分和原菜单使用同一正式字段/命令；没有真实合同的能力不能靠默认值或空实现冒充。
5. 最后收口布局、事件、重绘和预览导航，保留已完成UI与源迁移。直接Runtime仍按tasks第12节接公共核心，不由本轮作者审阅扩大到玩法算法。

顺序是依赖说明，不替用户重新判定功能优先级。本次只更新文档，不自动向实现任务派发新工作，不新增验证tasks。已存在的源码修复、规范检查与真实窗口结果是不同证据，必须分别报告；本表未实跑项目不以“所有接口已齐”作承诺。

## 5. 与现行规范的对账

本次读取current btsmtl-timeline-editor-preview第261行附近：源XYZ/Yaw已归RootMotionCurveAsset，局部通道仅Weight/Ease，源迁移条款已经同步，不再是“等待曲线任务更新旧规范”。本次新增的是Timeline内必须只读查看源曲线及使用区间，强化查看体验但不改变写入owner；delta将此前MAY查看改为MUST。

current原子Curve mutation、完整key/切线、正式owner、仅本次草稿写入的要求继续保留。A06–A08属于这些既有要求的实现缺口，不是另起新数据模型的理由。current旧预览/总Program条款仍按原领域运行change处理，本轮不覆盖其规范。此前缺少btsmtl前缀的作者能力路径已纠正为btsmtl-timeline-animation-authoring-surface。
