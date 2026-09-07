import argparse
import json
import math
import struct
import sys
from pathlib import Path

from build_guide import Sources, digest, read_json, write_json
from decode_native_curves import NativeReader


TYPE_INDICES = (34731, 34732, 38262, 40540, 51127, 51333, 62368, 62600, 62601, 62602, 62603, 62604, 62605, 69877, 69878, 79377, 79378, 81404)
SHOT_LAYOUTS = {
    "eb90f8d686f1a77955a9bd6f831e04dff6430a0e5d46fe4875f5a9c128ac1067": "metadata-829",
    "3e010c5d2e3f5477ca42ffb045276352fd52f01e60745ded882d9cc1d8fe3e05": "metadata-829",
    "976f813407590ed92567d5045c4620f2560d830d04792677e2829dcd4c622a9d": "compact-blend-words",
    "4028585cca845c5d6b2a5bc54d44aae6d1c40f9a4792555c92ecf57433786e63": "compact-blend-words",
    "e0adac1b9f7805d50a77f3dc0cbb1c8a67da7cd69c5eae4e1f2957a6457089e1": "compact-blend-words",
}


def export_schema(output):
    metadata = Path("D:/ZZZ_Dump/PIK分析包/元数据")
    sys.path.insert(0, str(metadata))
    from export_gameplay_metadata import GameplaySession, exported_type
    verification = read_json(metadata / "控制器与战斗/verification.json")
    checked = []
    for record in verification["sources"]:
        if Path(record["path"]).name in {"GameAssembly.dll", "metaheap_829.bin", "metaheap_829.map", "game1500_306A84000_pages.bin", "game1500_306A84000_pages.map"}:
            if digest(Path(record["path"])) != record["sha256"]:
                raise ValueError(f"元数据来源版本不一致：{record['path']}")
            checked.append(record)
    catalog_path = metadata / "控制器与战斗/catalog_829.json"
    expected = next(a["sha256"] for a in verification["artifacts"] if a["path"] == catalog_path.name)
    if digest(catalog_path) != expected:
        raise ValueError("类型目录与元数据核实记录不一致")
    catalog = {t["type_index"]: t for t in read_json(catalog_path)["types"]}
    session = GameplaySession("829")
    errors = {}
    rows = [exported_type(session, catalog, index, ["镜头原生序列化字段布局"], errors) for index in TYPE_INDICES]
    for row in rows:
        if any(not f["complete"] for f in row["fields"]):
            raise ValueError(f"镜头字段元数据未读全：{row['full_name']}")
    write_json(output / "schema.json", {"session": "829", "sources": checked, "types": rows, "typeErrors": errors,
               "note": "字段顺序来自元数据；序列化范围由原始对象边界核对。枚举常量值未展开，保留整数。"})
    from capstone import Cs, CS_ARCH_X86, CS_MODE_64
    from offline_sources import PEImage
    assembly = next(r for r in checked if Path(r["path"]).name == "GameAssembly.dll")
    image = PEImage(Path(assembly["path"]))
    disassembler = Cs(CS_ARCH_X86, CS_MODE_64)
    shot = next(r for r in rows if r["type_index"] == 62600)
    getters = []
    for name, size in (("get_duration", 6), ("get_durationByEvent", 11)):
        method = next(m for m in shot["methods"] if m["name"] == name)
        raw = image.read(method["rva"], size)
        instructions = [(i.address, i.mnemonic, i.op_str) for i in disassembler.disasm(raw, method["rva"])]
        if instructions[-1][1] != "ret":
            raise ValueError("镜头时长 getter 未结束于已确认的叶函数边界")
        getters.append({"method": method, "bytes": raw.hex(), "instructions": instructions})
    write_json(output / "duration-rule.json", {"source": assembly, "field": next(f for f in shot["fields"] if f["name"] == "_duration"),
               "getters": getters, "confirmedPredicate": "durationByEvent == (_duration < 0)",
               "scope": "确认属性判定；具体结束事件如何调度及中断路径仍需消费者证据"})
    return {r["name"]: r for r in rows if r["name"] != "KeyValueInfo"}


class CameraReader(NativeReader):
    def __init__(self, data, position, schema, layout):
        super().__init__(data, position)
        self.schema = schema
        self.layout = layout
        self.ranges = []

    def value(self, kind, field):
        start = self.position
        try:
            result = self.read_value(kind, field)
        except (ValueError, KeyError, struct.error) as error:
            raise ValueError(f"{field} 在字节 {start} 无法按 {kind} 读取：{error}") from error
        self.ranges.append({"field": field, "type": kind, "byteOffset": start, "byteLength": self.position - start})
        return result

    def read_value(self, kind, field):
        if kind == "string":
            return self.string()
        if kind in ("int", "uint"):
            return self.unpack("i" if kind == "int" else "I")
        if kind == "float":
            value = self.unpack("f")
            if not math.isfinite(value):
                raise ValueError("非有限浮点数")
            return value
        if kind == "bool":
            value = self.unpack("I")
            if value not in (0, 1):
                raise ValueError(f"非布尔原值 {value}")
            return bool(value)
        if kind in ("Vector2", "Vector3", "Quaternion"):
            names = {"Vector2": "xy", "Vector3": "xyz", "Quaternion": "xyzw"}[kind]
            return {name: self.value("float", field + "." + name) for name in names}
        if kind == "AnimationCurve":
            return self.curve()
        if kind.startswith("List<"):
            return [self.value(kind[5:-1], f"{field}[{i}]") for i in range(self.count())]
        definition = self.schema[kind]
        if definition["enum_type"]:
            return self.unpack("i")
        result = {}
        for entry in definition["fields"]:
            if entry["static"] or (kind == "CameraShotData" and self.layout == "compact-blend-words" and entry["ordinal"] >= 50):
                continue
            name = entry["name"]
            result[name] = self.value(entry["type"]["display"], field + "." + name)
        if kind == "CameraShotData" and self.layout == "compact-blend-words":
            result["unmappedBetweenBlends"] = [self.value("uint", field + ".unmappedBetweenBlends[0]")]
            result["_blendDefinitionExit"] = self.value("CinemachineBlendDefinition", field + "._blendDefinitionExit")
            result["unmappedAfterExitBlend"] = [self.value("uint", f"{field}.unmappedAfterExitBlend[{i}]") for i in range(4)]
        return result


def decode(entry, schema, decoder):
    path = Path(entry["RawPath"])
    if digest(path) != entry["Sha256"]:
        raise ValueError(f"原始对象哈希不一致：{path}")
    raw = path.read_bytes()
    header = decoder.extract_payload(raw)
    name = entry["Plan"]["Name"]
    if header["name"] != name:
        raise ValueError("对象名称与精确来源身份不符")
    position = (header["payloadOffset"] + header["payloadBytes"] + 3) & ~3
    if raw[position:position + 32] != bytes(32):
        raise ValueError("SerializationData 中有尚未支持的非空引用或编辑器字段")
    layout = SHOT_LAYOUTS[entry["Sha256"]] if name == "CameraCutscenes" else "metadata-829"
    reader = CameraReader(raw, position + 32, schema, layout)
    rows = []
    if name == "Pipeline_Camera_Avatar_Config":
        fields = {f["name"]: reader.value(f["type"]["display"], f["name"]) for f in schema["PipelineCameraAvatarConfigData"]["fields"]
                  if not f["type"]["display"].startswith("Dictionary<")}
    else:
        for i in range(reader.count()):
            key = reader.value("string", f"KeyValueInfoList[{i}].keyInst")
            if name == "CameraCutscenes":
                value = reader.value("CameraShotData", f"KeyValueInfoList[{i}].valueInst")
                rows.append({"keyInst": key, "valueInst": value})
            else:
                value_type = reader.value("string", f"KeyValueInfoList[{i}].valueType")
                text = reader.value("string", f"KeyValueInfoList[{i}].valueInst")
                rows.append({"keyInst": key, "valueType": value_type, "valueInst": json.loads(text), "serializedJson": text})
        if len({r["keyInst"] for r in rows}) != len(rows):
            raise ValueError("列表中存在重复键，需要先确认字典还原规则")
        fields = {"KeyValueInfoList": rows}
    if reader.position != len(raw):
        raise ValueError(f"原始对象未完整消费：{reader.position}/{len(raw)}")
    return {"schema": "zzz-native-camera/1", "source": entry, "layout": layout, "nativeFields": fields,
            "bytesConsumed": reader.position, "nativeBodyOffset": position + 32, "fieldRanges": reader.ranges,
            "unmappedWordCount": len(rows) * 5 if layout == "compact-blend-words" else 0,
            "note": "compact-blend-words 的混合附近 5 个 uint 保留原值和位置，尚未赋予字段语义；其它字段按元数据和记录边界读取。"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/camera-resource-search"))
    args = parser.parse_args()
    output = args.root / "native-camera"
    schema = export_schema(output)
    settings = read_json(Path(__file__).parent / "sources/corin.json")
    sources = Sources(settings)
    records = []
    for entry in read_json(args.root / "raw-camera/manifest.json")["results"]:
        name = entry["Plan"]["Name"]
        if name not in ("CameraCutscenes", "CameraLockDatas", "Pipeline_Camera_Avatar_Config"):
            continue
        result = decode(entry, schema, sources.decoder)
        path = output / (Path(entry["RawPath"]).stem + ".json")
        write_json(path, result)
        records.append({"name": name, "path": path.as_posix(), "sourceSha256": entry["Sha256"],
                        "rawPath": entry["RawPath"], "layout": result["layout"], "bytesConsumed": result["bytesConsumed"],
                        "entries": len(result["nativeFields"].get("KeyValueInfoList", [])), "unmappedWordCount": result["unmappedWordCount"]})
    write_json(output / "manifest.json", {"schema": "zzz-native-camera-manifest/1", "results": records,
               "decoder": {"path": Path(__file__).resolve().as_posix(), "sha256": digest(Path(__file__))},
               "schemaSha256": digest(output / "schema.json")})
    print(records)


if __name__ == "__main__":
    main()
