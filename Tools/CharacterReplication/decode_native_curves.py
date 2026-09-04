import argparse
import importlib.util
import math
import struct
from pathlib import Path

from build_guide import digest, read_json, write_json


class NativeReader:
    def __init__(self, data, position):
        self.data = data
        self.position = position

    def unpack(self, fmt):
        result = struct.unpack_from("<" + fmt, self.data, self.position)
        self.position += struct.calcsize("<" + fmt)
        return result[0] if len(result) == 1 else result

    def count(self):
        value = self.unpack("i")
        if value < 0 or value > len(self.data) - self.position:
            raise ValueError(f"非法集合长度 {value}，位置 {self.position - 4}")
        return value

    def string(self):
        length = self.count()
        value = self.data[self.position:self.position + length].decode("utf-8")
        self.position = (self.position + length + 3) & ~3
        return value

    def curve(self):
        start = self.position
        keys = []
        for _ in range(self.count()):
            values = self.unpack("ffffiff")
            key = dict(zip(("time", "value", "inSlope", "outSlope", "weightedMode", "inWeight", "outWeight"), values))
            for field, value in key.items():
                if isinstance(value, float) and not math.isfinite(value):
                    key[field] = "NaN" if math.isnan(value) else "Infinity" if value > 0 else "-Infinity"
            if not isinstance(key["time"], float) or not isinstance(key["value"], float) or key["weightedMode"] not in (0, 1, 2, 3):
                raise ValueError(f"非法曲线关键帧，位置 {self.position}")
            if keys and key["time"] < keys[-1]["time"]:
                raise ValueError("曲线时间没有按原序递增")
            keys.append(key)
        pre, post, rotation = self.unpack("iii")
        return {"keys": keys, "preInfinity": pre, "postInfinity": post, "rotationOrder": rotation,
                "byteOffset": start, "byteLength": self.position - start}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path("D:/ZZZ_Dump/output/corin_replication/replication-guide/analysis/camera-resource-search"))
    args = parser.parse_args()
    spec = importlib.util.spec_from_file_location("odin", Path("D:/ZZZ_Dump/kern_tools/OdinBinaryDecoder.py"))
    odin = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(odin)
    manifest = read_json(args.root / "raw-curves/manifest.json")
    results = []
    for entry in manifest["results"]:
        path = Path(entry["RawPath"])
        raw = path.read_bytes()
        if digest(path) != entry["Sha256"]:
            raise ValueError(f"源文件哈希不一致：{path}")
        header = odin.extract_payload(raw)
        if header["name"] != "AnimationCurveLibrary" or header["payloadBytes"] != 0:
            raise ValueError("需要 AnimationCurveLibrary 的空 Odin、原生列表格式")
        position = header["payloadOffset"]
        if raw[position:position + 32] != bytes(32):
            raise ValueError("未支持的非空 SerializationData 引用或编辑器字段")
        reader = NativeReader(raw, position + 32)
        is_main = reader.unpack("I")
        if is_main not in (0, 1):
            raise ValueError("非法 isMainLibrary")
        groups = {}
        for group in ("StandardCurveDatas", "BaseCurveDatas", "CustomCurveDatas"):
            rows = []
            for _ in range(reader.count()):
                name = reader.string()
                rows.append({"name": name, "curve": reader.curve()})
            groups[group] = rows
        if reader.position != len(raw):
            raise ValueError(f"原生曲线未完整消费：{reader.position}/{len(raw)}")
        result = {"schema": "zzz-native-animation-curve-library/1", "source": entry,
                  "isMainLibrary": bool(is_main), "groups": groups, "bytesConsumed": reader.position,
                  "formatEvidence": "AnimeStudio/Classes/AnimationClip.cs:Keyframe<T>,AnimationCurve<T>; 元数据 StringAnimationCurve 与 AnimationCurveLibrary"}
        target = args.root / "native-curves" / path.with_suffix(".json").name
        write_json(target, result)
        result_count = sum(len(v) for v in groups.values())
        results.append({"path": target.as_posix(), "sourceSha256": entry["Sha256"], "curves": result_count})
    write_json(args.root / "native-curves/manifest.json", {"results": results})
    print(results)


if __name__ == "__main__":
    main()
