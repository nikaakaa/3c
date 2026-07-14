## 1. 固定依赖与现状边界

- [x] 1.1 确认 `refactor-pipeline-blackboard-owned-scopes` 已归档，或记录其最终实现作为本 change 的显式 rebase 基线
- [x] 1.2 列出 `BaseTreeInspectorView` 当前直接持有的 ExposedProperty UI 状态与刷新入口
- [x] 1.3 列出独立 Input panel 的 provider、registry、view、UXML、USS 和拖拽入口
- [x] 1.4 确认旧 Graph panel registry 没有 Input 之外的正式实现
- [x] 1.5 列出 Graph、inline graph 和 Transition selection 切换 authoring context 的入口
- [x] 1.6 确认 Blackboard local/inherited、scope、lifetime、category 和 owner 使用同一现有解析 API
- [x] 1.7 确认 Input 条目创建节点使用稳定 input/request identity 的现有正式工厂
- [x] 1.8 固定删除清单，排除 runtime、序列化和网络层文件

## 2. 建立目录领域合同

- [x] 2.1 新增编辑器专用 `GraphDataCatalogContext`，表达当前 Tree、Graph、Graph 类型与 authoring owner
- [x] 2.2 为 context 增加 generation/identity，使旧投影可在上下文切换后失效
- [x] 2.3 新增 `GraphDataCatalogEntry` 的稳定条目 identity
- [x] 2.4 为条目定义 source kind 与 entry kind
- [x] 2.5 为条目定义 display name、value type 和 category path 投影
- [x] 2.6 为条目定义 external/local/inherited ownership 投影
- [x] 2.7 为条目定义 owner label 与 source label 投影
- [x] 2.8 为条目定义 mutable/read-only 状态
- [x] 2.9 为条目定义 drag、details、edit、delete、locate 能力集合
- [x] 2.10 为不可用能力定义可展示的原因，不加入 fallback 命令
- [x] 2.11 新增目录来源合同，负责查询条目、执行来源命令和发布失效通知
- [x] 2.12 新增目录来源组合器，按当前 authoring context 聚合正式来源
- [x] 2.13 确认目录合同只存在于 Editor assembly 且不被图资产序列化

## 3. 建立唯一 Graph Data 目录外壳

- [x] 3.1 在 Tree Inspector UXML 中建立单一 `Graph Data` section
- [x] 3.2 删除独立 Input section 的标题与容器占位
- [x] 3.3 将目录 section 放置在 Graph/Selection 页面切换时均可访问的位置
- [x] 3.4 新增单一文本搜索输入
- [x] 3.5 新增 Source 过滤控件
- [x] 3.6 将现有 Blackboard ownership/context 过滤并入目录工具栏
- [x] 3.7 将现有 Blackboard scope 过滤并入目录工具栏
- [x] 3.8 为 Blackboard 专属过滤器增加明确来源语义
- [x] 3.9 新增目录级 `+` 图标按钮和 tooltip
- [x] 3.10 新增默认隐藏的 Blackboard 内联创建条
- [x] 3.11 在创建条中放置名称输入
- [x] 3.12 在创建条中放置合法 scope 选择
- [x] 3.13 在创建条中放置值类型选择
- [x] 3.14 在创建条中放置确认与取消图标命令
- [x] 3.15 建立单一 ScrollView、来源分组和层级 category 容器
- [x] 3.16 保留同一 TreeWindow 内的搜索、过滤和折叠状态

## 4. 接入 Pipeline Blackboard 来源

- [x] 4.1 从当前上下文的正式可见 declaration API 构建 Blackboard 条目
- [x] 4.2 将 declaration identity 映射为目录稳定 identity
- [x] 4.3 映射 local/inherited ownership 与实际 owner label
- [x] 4.4 映射 value type、scope、lifetime、authority、sync policy 和默认值
- [x] 4.5 映射层级 `CategoryPath`
- [x] 4.6 将空 category 归入唯一 `Uncategorized`
- [x] 4.7 只为当前 owner 的本地 declaration 授予 edit/delete 能力
- [x] 4.8 为继承 declaration 授予只读详情和定位 owner 能力
- [x] 4.9 根据当前 Graph 类型计算 Blackboard 节点拖拽能力
- [x] 4.10 将创建条命令接入现有 declaration 创建 API
- [x] 4.11 将详情编辑接入现有 declaration 更新 API
- [x] 4.12 将删除命令接入现有 declaration 删除与引用诊断链路
- [x] 4.13 在 declaration 新增、修改、删除和 owner 切换时发布目录失效

## 5. 接入 Character Input 来源

- [x] 5.1 在 Character Pipeline editor 中实现 Input 目录来源，不让 BTSMTL core 引用 Character 类型
- [x] 5.2 只从当前正式 `CharacterPipelineDefinition.InputProfile` 读取条目
- [x] 5.3 将 input value stable id 映射为目录稳定 identity
- [x] 5.4 将 action request stable id 映射为目录稳定 identity
- [x] 5.5 将 input value 归入 `Input / Values`
- [x] 5.6 将 action request 归入 `Input / Requests`
- [x] 5.7 将 Input 条目标记为 external read-only
- [x] 5.8 为 Input 条目提供 Profile source label 与定位来源能力
- [x] 5.9 禁止 Input 来源提供 create declaration、edit 或 delete 命令
- [x] 5.10 将 input value 拖拽接回正式 typed info node 工厂
- [x] 5.11 将 action request 拖拽接回正式 request info node 工厂
- [x] 5.12 按普通 Graph 与 ConditionRuleGraph 的节点规则计算拖拽能力
- [x] 5.13 在缺少 definition/profile 上下文时提供明确 unavailable 状态
- [x] 5.14 禁止缺少上下文时搜索场景、猜测 Profile 或创建空绑定节点
- [x] 5.15 在 Profile 定义变化或 authoring context 变化时发布目录失效

## 6. 实现共享条目与详情交互

- [x] 6.1 建立 Input 与 Blackboard 共用的紧凑条目 VisualElement
- [x] 6.2 固定条目高度、图标区域、名称区域、类型区域和尾部命令区域尺寸
- [x] 6.3 使用现有类型系统生成统一类型颜色/图标
- [x] 6.4 将名称保持为纯名称，不拼接 local/inherited 或 owner 文本
- [x] 6.5 在独立元数据位置显示来源与所有权
- [x] 6.6 为 external/inherited read-only 条目显示统一锁定状态
- [x] 6.7 为过长名称、类型、category 和 owner 添加截断与完整 tooltip
- [x] 6.8 根据能力显示或隐藏定位、展开、编辑和删除命令
- [x] 6.9 实现 Blackboard 本地条目的内联可编辑详情
- [x] 6.10 实现 Input 与继承 Blackboard 条目的内联只读详情
- [x] 6.11 确保详情展开不改变其它条目的固定头部尺寸
- [x] 6.12 确保拖拽 handle 与展开/菜单点击区域不争抢手势

## 7. 统一查询、分组与上下文刷新

- [x] 7.1 让文本搜索覆盖名称、类型、category、owner 和 source
- [x] 7.2 实现 `All/Input/Blackboard` 来源过滤
- [x] 7.3 让 Blackboard ownership/context 过滤只作用于 Blackboard 条目
- [x] 7.4 让 Blackboard scope 过滤只作用于 Blackboard 条目
- [x] 7.5 在启用 Blackboard 专属过滤时排除 Input，而不是为 Input 构造虚假字段
- [x] 7.6 按固定来源层级和 `CategoryPath` 稳定排序条目
- [x] 7.7 让空分类只生成一个 `Uncategorized` 分组
- [x] 7.8 在 Graph tab 切换时重建当前上下文投影
- [x] 7.9 在 inline/shared Graph 下钻和返回时重建当前上下文投影
- [x] 7.10 在 Transition selection 切换时重建当前上下文投影
- [x] 7.11 在重建前清除上一 context generation 的条目引用和能力
- [x] 7.12 在同一 TreeWindow 上下文切换后恢复适用的查询与折叠状态

## 8. 删除旧路径并收敛入口

- [x] 8.1 删除独立 `CharacterInputGraphPanelProvider`
- [x] 8.2 删除独立 `CharacterInputDefinitionView`
- [x] 8.3 删除 Input panel 专用 UXML/USS 选择器和无引用资源
- [x] 8.4 删除旧 `graph-extension-container` 注入路径
- [x] 8.5 删除只有旧 Input panel 使用的 `ITreeInspectorGraphPanelProvider`
- [x] 8.6 删除只有旧 Input panel 使用的 `TreeInspectorGraphPanelRegistry`
- [x] 8.7 删除旧 `ExposedPropertyView` 大卡片模板与专用样式
- [x] 8.8 删除 `BaseTreeInspectorView` 中旧 ExposedProperty 双份列表、过滤和创建状态
- [x] 8.9 将所有正式 Graph 数据创建入口指向统一目录
- [x] 8.10 搜索并移除旧 Input 素材区、旧 ExposedProperty panel 和旧 registry 的剩余引用
- [x] 8.11 确认没有 hidden legacy UI、兼容 adapter、双写刷新或第二份 Input 定义

## 9. 自动化校验与规范收尾

- [x] 9.1 运行仓库既有 Editor assembly 编译检查，不运行 Unity batchmode
- [x] 9.2 检查新增 Editor 类型未进入 runtime assembly 或序列化字段
- [x] 9.3 检查 Input 与 Blackboard runtime、网络和资产格式没有非预期改动
- [x] 9.4 检查已删除类型、UXML name 和 USS class 不再被引用
- [x] 9.5 运行仓库既有静态格式与引用检查
- [x] 9.6 运行 `openspec validate unify-graph-data-catalog-authoring --strict --no-interactive`
- [x] 9.7 确认所有任务真实完成后统一更新本文件为 `- [x]`
