## 1. Ref 源码移植盘点
- [x] 1.1 列出 Ref Timeline editor 类与当前项目 Timeline editor 类的职责映射。
- [x] 1.2 列出 Ref TreeDesigner / GraphView 类与当前项目 Behavior / Branch editor 类的职责映射。
- [x] 1.3 标记不能进入正式 runtime 的 Ref 类型、namespace、asset 和 runner。
- [x] 1.4 标记当前需要删除或替换的半移植 / 自研 editor path。

## 2. Timeline Editor 源码级替换
- [x] 2.1 移植或重写项目命名的 timeline drag manipulator。
- [x] 2.2 移植或重写项目命名的 timeline drag line manipulator。
- [x] 2.3 将 Timeline field 拆成 Ref-equivalent field view，持有 frame position map、scale、offset、locator、pan、zoom 和 rectangle selection。
- [x] 2.4 将 Track view / track handle 拆成 Ref-equivalent 结构，支持 track selection、add、delete、reorder 和空轨道展示。
- [x] 2.5 将 Clip view 替换为 Ref-equivalent 结构，clip move 只通过 move drag 委托给 field view。
- [x] 2.6 将 left resize / right resize 替换为独立 drag line manipulator，不再通过 root pointer mode 推断。
- [x] 2.7 将 move leader / apply move / invalid preview / same-track overlap validation 移植到 field view，并适配项目正式 clip 规则。
- [x] 2.8 将 locator click / drag、F 定位、mouse pan、wheel zoom 和 rectangle selection 对齐 Ref 行为。
- [x] 2.9 将 Timeline Inspector 绑定到正式 TimelineNode serialized adapter，不保留 Dodge-only Directional / Backstep 保存入口。
- [x] 2.10 删除旧 `CommittedActionRefPortedTimelineView` 中被替代的半自研交互 path 或使其不可达。

## 3. Behavior / Branch Graph 源码级替换
- [x] 3.1 移植或重写 Ref TreeDesigner 风格 GraphView shell、node view、port view、edge view 和 search window。
- [x] 3.2 固定 root 节点只映射到项目正式 root node id，不允许普通创建或删除。
- [x] 3.3 Node property panel 通过 stable node id 和项目 adapter 写回 selector、condition、timeline node payload。
- [x] 3.4 Character Behavior source graph 与 Committed Action branch graph 保持数据边界，不复制对方数据。
- [x] 3.5 删除 card/list branch editor、重复 branch editor 窗口或伪图入口。
- [x] 3.6 TimelineNode 只提供打开或聚焦独立 Timeline Editor 的入口，不嵌入 timeline field。

## 4. 资源与命名清理
- [x] 4.1 导入 Ref UXML / USS / 图标时移除 Ref 项目路径引用和 Ref `.meta` 依赖。
- [x] 4.2 统一窗口标题、菜单和测试命名为 Character Behavior Editor / Committed Action Timeline Editor。
- [x] 4.3 删除或改名误导性的 Skill Editor、half-port、temporary、fallback 命名。
- [x] 4.4 确认 runtime source 不引用 Editor-only view、GraphView、TimelinePlayer、PlayableGraph 或 Taco runner。

## 5. 自动测试与验证
- [x] 5.1 增加 timeline manipulator EditMode 测试，覆盖 move drag、left resize、right resize 不互相抢占。
- [x] 5.2 增加 timeline field EditMode 测试，覆盖 frame position map、zoom、pan、locator 和 rectangle selection。
- [x] 5.3 增加 clip 多选移动测试，覆盖 move leader、apply move、到 0 帧边界和 invalid preview。
- [x] 5.4 增加 TimelineNode 写回测试，覆盖保存、重载和 `CharacterActionDefinitionSO.ToDefinition()` 编译结果。
- [x] 5.5 增加 branch GraphView 测试，覆盖 fixed root、SearchWindow、edge writeback、node panel payload writeback 和 selection stable id。
- [x] 5.6 增加静态边界测试，确认 runtime 不引用 Ref runner、UnityEditor view、GraphView、TimelinePlayer 或 PlayableGraph。
- [x] 5.7 运行 `openspec validate refactor-ref-editor-source-port --strict --no-interactive`。
- [x] 5.8 运行相关 Unity EditMode 定向测试。
