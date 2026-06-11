Haste Humanoid 转换

已添加两个 Editor 工具：

Assets/Editor/HasteHumanoidAvatarBuilder.cs
Assets/Editor/HasteHumanoidClipBaker.cs

使用顺序：
1. 等 Unity 编译完成。
2. 菜单点击 Tools/Haste/Build Courier Humanoid Avatar。
3. 检查输出：
   Assets/Art/Animation/Haste/HasteMainCharacter/Humanoid/HasteCourier_HumanoidAvatar.asset
   Assets/Art/Animation/Haste/HasteMainCharacter/Humanoid/HasteCourier_Humanoid.prefab
4. 把 HasteCourier_Humanoid.prefab 拖进场景，确认子物体 Courier 的 Animator 里 Avatar 是 Valid/Human。

预览动作：
1. 菜单点击 Tools/Haste/Bake Preview Humanoid Clips。
2. 输出位置：
   Assets/Art/Animation/Haste/HasteMainCharacter/Humanoid/HumanoidClips
   Assets/Art/Animation/Haste/HasteMainCharacter/Humanoid/HumanoidPreviewPrefabs
3. 搜索 Preview_Humanoid，拖预览 Prefab 到场景，点 Play。

全量动作：
确认预览动作能播以后，再点 Tools/Haste/Bake All Courier Humanoid Clips。

说明：
Build Courier Humanoid Avatar 是把 Courier 模型转成 Unity Humanoid Avatar。
Bake Humanoid Clips 是把 Haste 的 Generic Transform 动画采样成 Humanoid muscle 曲线。
如果 Avatar 无效，先不要批量烘焙，需要调整骨骼映射。
