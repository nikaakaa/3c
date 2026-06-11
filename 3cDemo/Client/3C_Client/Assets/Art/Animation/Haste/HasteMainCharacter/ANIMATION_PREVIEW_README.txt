Haste 动画预览

我已经生成了 3 个直接可播的预览 Prefab：

GameObject/Preview_Courier_Idle.prefab
GameObject/Preview_Courier_Board_Fly_0.prefab
GameObject/Preview_Courier_Board_JumpType1.prefab

使用方式：
1. 回到 Unity，等资源刷新完成。
2. Project 面板搜索 Preview_Courier。
3. 把其中一个 Preview_Courier_*.prefab 拖进场景。
4. 点 Play。

这些 Prefab 已经把子物体 Courier 的 Animator 指向对应的 Preview Controller。
原始 New_Animator_Courier.controller 默认状态是 Neutral，而且 Neutral 没有 Motion，
所以直接用原始 Controller 按 Play 往往看起来不会动。

如果手动测试：
Animator 要挂在 Courier_Retake/Courier 上，不要挂最外层 Courier_Retake。
因为动画曲线路径是 Armature/...，Armature 在 Courier 下面。
