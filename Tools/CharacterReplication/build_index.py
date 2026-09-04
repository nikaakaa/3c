from pathlib import Path

from build_guide import read_json, table


def main():
    rows = []
    for path in sorted((Path(__file__).parent / "sources").glob("*.json")):
        settings = read_json(path)
        directory = Path(settings["output"])
        summary = read_json(directory / "data/summary.json")
        rows.append((f"[{settings['character']}]({directory.as_posix()}/README.md)", summary["focusedStates"], summary["states"],
                     summary["transitions"], summary["windows"], sum(summary["cameraResources"].values()), summary["skills"]))
    root = Path("D:/ZZZ_Dump/output/character_replication")
    root.mkdir(exist_ok=True)
    text = "# 角色复刻资料入口\n\n从角色入口进入动作、混合、输入窗口、事件、镜头、技能和来源对账。所有角色共用一个生成器；公共资源只按精确键关联，不复制角色参数。\n\n"
    text += table(["角色", "重点动作页", "全部状态", "全部转场", "时间区域", "镜头配置", "已纳入技能"], rows)
    text += "\n\nnull 表示本包尚未纳入相应资料，不表示角色没有配置。切人不进入当前重点动作页；完整原控制器和事件仍保存。\n"
    text += "\n动画按 CAB/PathID 对账，事件按角色的正式映射关联。输入缓存、枚举和特殊时间规则没有用经验值补齐。\n"
    (root / "README.md").write_text(text, encoding="utf-8")
    print(root / "README.md")


if __name__ == "__main__":
    main()
