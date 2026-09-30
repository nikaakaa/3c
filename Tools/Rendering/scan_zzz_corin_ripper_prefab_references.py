import argparse
import json
from collections import Counter, defaultdict
from pathlib import Path

import yaml


PLACEHOLDER_GUID = "0000000deadbeef15deadf00d0000000"


def parse_documents(path):
    text = path.read_text(encoding="utf-8")
    documents = []
    for block in text.split("--- !u!")[1:]:
        header, body = block.split("\n", 1)
        class_id, local_id = header.replace("&", "").split()
        data = yaml.safe_load(body)
        type_name = next(iter(data))
        documents.append(
            {
                "classId": int(class_id),
                "localId": int(local_id),
                "type": type_name,
                "data": data[type_name],
            }
        )
    return documents


def semantic_category(field_path):
    leaf = field_path.rsplit(".", 1)[-1]
    if leaf == "m_Script":
        return "Script"
    if leaf in {"m_Material", "m_Materials"}:
        return "Material"
    if leaf in {"m_Mesh", "m_Mesh1", "m_Mesh2", "m_Mesh3"}:
        return "Mesh"
    if leaf in {"m_Texture", "m_MainTexture"}:
        return "Texture"
    if leaf in {"m_Sprite", "m_Sprites"}:
        return "Sprite"
    if leaf in {"m_Animation", "m_Animations"}:
        return "AnimationClip"
    if leaf == "m_Font":
        return "Font"
    return "Other"


def walk_references(value, path, references):
    if isinstance(value, dict):
        if "fileID" in value:
            references.append(
                {
                    "field": path,
                    "fileId": value["fileID"],
                    "guid": value.get("guid"),
                    "type": value.get("type"),
                }
            )
        for key, child in value.items():
            walk_references(child, f"{path}.{key}", references)
    elif isinstance(value, list):
        for child in value:
            walk_references(child, f"{path}[]", references)


def resolve_reference(reference, local_ids, guid_index):
    guid = reference["guid"]
    if guid is None and reference["fileId"] == 0:
        scope = "null"
        target = None
        resolved = True
    elif guid is None:
        scope = "local"
        target = local_ids.get(str(reference["fileId"]))
        resolved = target is not None
    elif guid == "00000000000000000000000000000000":
        scope = "null"
        target = None
        resolved = True
    elif guid == PLACEHOLDER_GUID:
        scope = "assetRipperPlaceholder"
        target = None
        resolved = False
    else:
        target = guid_index.get(guid)
        scope = "project" if target is not None else "unresolvedGuid"
        resolved = target is not None
    return {
        **reference,
        "scope": scope,
        "target": target,
        "resolved": resolved,
        "localTargetType": target if scope == "local" else None,
    }


def target_category(reference):
    if reference["scope"] == "local":
        if reference["target"] is None:
            return "MissingLocalObject"
        return "LocalObject"
    if reference["scope"] == "null":
        return "Null"
    if reference["scope"] == "assetRipperPlaceholder":
        if reference["field"].endswith(".m_Component[].component"):
            return "MissingComponentStub"
        return "MissingAssetStub"
    if reference["scope"] == "unresolvedGuid":
        return "UnresolvedExternal"
    suffix = Path(reference["target"]).suffix
    if suffix == ".cs":
        return "Script"
    if suffix == ".prefab":
        return "Prefab"
    if suffix == ".asset":
        return "ScriptableObjectAsset"
    return "ExternalAsset"


def load_guid_index(project_root):
    index = {}
    for meta_path in project_root.rglob("*.meta"):
        for line in meta_path.read_text(encoding="utf-8").splitlines():
            if line.startswith("guid: "):
                guid = line[6:].strip()
                target = str(meta_path.with_suffix("")).replace("\\", "/")
                if guid in index and index[guid] != target:
                    raise RuntimeError(
                        f"duplicate asset guid {guid}: {index[guid]}, {target}"
                    )
                index[guid] = target
                break
    return index


def scan_prefab(path, guid_index):
    documents = parse_documents(path)
    local_ids = {str(document["localId"]): document["type"] for document in documents}
    object_counts = Counter(document["type"] for document in documents)
    references = []
    for document in documents:
        document_references = []
        walk_references(document["data"], document["type"], document_references)
        for reference in document_references:
            resolved = resolve_reference(reference, local_ids, guid_index)
            resolved["semanticCategory"] = semantic_category(reference["field"])
            resolved["targetCategory"] = target_category(resolved)
            references.append(resolved)
    reference_counts = Counter(item["targetCategory"] for item in references)
    semantic_counts = Counter(item["semanticCategory"] for item in references)
    placeholder_fields = Counter(
        item["field"]
        for item in references
        if item["scope"] == "assetRipperPlaceholder"
    )
    script_guids = Counter(
        item["guid"]
        for item in references
        if item["semanticCategory"] == "Script"
    )
    root = next(document for document in documents if document["type"] == "GameObject")
    return {
        "prefab": str(path),
        "name": root["data"]["m_Name"],
        "documentCount": len(documents),
        "objectCounts": dict(sorted(object_counts.items())),
        "referenceCount": len(references),
        "targetCategories": dict(sorted(reference_counts.items())),
        "semanticCategories": dict(sorted(semantic_counts.items())),
        "placeholderFields": dict(sorted(placeholder_fields.items())),
        "scripts": [
            {
                "guid": guid,
                "count": count,
                "target": guid_index.get(guid),
            }
            for guid, count in sorted(script_guids.items())
        ],
        "references": references,
    }


def aggregate(records):
    object_counts = Counter()
    target_categories = Counter()
    semantic_categories = Counter()
    placeholder_fields = Counter()
    unresolved_local_fields = Counter()
    scripts = Counter()
    unresolved_guids = Counter()
    by_name = defaultdict(list)
    for record in records:
        by_name[record["name"]].append(Path(record["prefab"]).name)
        object_counts.update(record["objectCounts"])
        target_categories.update(record["targetCategories"])
        semantic_categories.update(record["semanticCategories"])
        placeholder_fields.update(record["placeholderFields"])
        unresolved_local_fields.update(
            item["field"]
            for item in record["references"]
            if item["targetCategory"] == "MissingLocalObject"
        )
        for script in record["scripts"]:
            scripts[(script["guid"], script["target"])] += script["count"]
    return {
        "prefabCount": len(records),
        "uniqueRootNames": len(by_name),
        "objectCounts": dict(sorted(object_counts.items())),
        "targetCategories": dict(sorted(target_categories.items())),
        "semanticCategories": dict(sorted(semantic_categories.items())),
        "placeholderFields": dict(sorted(placeholder_fields.items())),
        "unresolvedLocalFields": dict(sorted(unresolved_local_fields.items())),
        "scripts": [
            {"guid": guid, "target": target, "count": count}
            for (guid, target), count in sorted(scripts.items())
        ],
        "unresolvedGuids": [
            {"guid": guid, "count": count}
            for guid, count in sorted(unresolved_guids.items())
        ],
        "copiesByRootName": {
            name: sorted(copies) for name, copies in sorted(by_name.items())
        },
    }


def count_expected_components(node, counts):
    for component in node["components"]:
        if component["type"] in {"ParticleSystem", "ParticleSystemRenderer"}:
            counts[component["type"]] += 1
    for child in node["children"]:
        if child.get("resolved", True):
            count_expected_components(child, counts)


def aggregate_hierarchy_completeness(records, hierarchy):
    expected = {}
    for record in hierarchy["records"]:
        counts = {"ParticleSystem": 0, "ParticleSystemRenderer": 0}
        count_expected_components(record["root"], counts)
        expected[record["source"]["name"]] = counts

    records_by_name = defaultdict(list)
    for record in records:
        records_by_name[record["name"]].append(record)

    details = []
    best_exported_renderers = 0
    complete_best_renderer_roots = 0
    complete_slot_copies = 0
    incomplete_slot_copies = 0
    for name, root_records in sorted(records_by_name.items()):
        required = expected[name]
        copies = []
        exported_counts = []
        for record in root_records:
            exported = record["objectCounts"].get("ParticleSystemRenderer", 0)
            missing_components = record["placeholderFields"].get(
                "GameObject.m_Component[].component", 0
            )
            accounted = exported + missing_components
            complete_slots = accounted == (
                required["ParticleSystem"] + required["ParticleSystemRenderer"]
            )
            complete_slot_copies += int(complete_slots)
            incomplete_slot_copies += int(not complete_slots)
            exported_counts.append(exported)
            copies.append(
                {
                    "prefab": Path(record["prefab"]).name,
                    "exportedRendererCount": exported,
                    "missingComponentStubCount": missing_components,
                    "accountedRendererSlots": accounted,
                    "completeRendererSlots": complete_slots,
                }
            )
        best = max(exported_counts)
        best_exported_renderers += best
        complete_best = best == required["ParticleSystemRenderer"]
        complete_best_renderer_roots += int(complete_best)
        details.append(
            {
                "name": name,
                "expectedParticleSystemCount": required["ParticleSystem"],
                "expectedRendererCount": required["ParticleSystemRenderer"],
                "bestExportedRendererCount": best,
                "bestExportRendererComplete": complete_best,
                "copies": copies,
            }
        )
    return {
        "expectedParticleSystemCount": sum(
            item["ParticleSystem"] for item in expected.values()
        ),
        "expectedRendererCount": sum(
            item["ParticleSystemRenderer"] for item in expected.values()
        ),
        "bestExportedRendererCount": best_exported_renderers,
        "rootsWithCompleteBestRenderers": complete_best_renderer_roots,
        "rootsWithMissingBestRenderers": len(details) - complete_best_renderer_roots,
        "completeRendererSlotCopies": complete_slot_copies,
        "incompleteRendererSlotCopies": incomplete_slot_copies,
        "roots": details,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, required=True)
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--hierarchy", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    manifest = json.loads(args.manifest.read_text(encoding="utf-8"))
    hierarchy = json.loads(args.hierarchy.read_text(encoding="utf-8"))
    manifest_root_names = {item["name"] for item in manifest["roots"]}
    prefab_paths = sorted(args.project_root.glob("Eff_Corin*.prefab"))
    guid_index = load_guid_index(args.project_root)
    records = [scan_prefab(path, guid_index) for path in prefab_paths]
    record_names = {record["name"] for record in records}
    missing_roots = sorted(manifest_root_names - record_names)
    extra_roots = sorted(record_names - manifest_root_names)
    if missing_roots or extra_roots:
        raise RuntimeError(
            f"prefab root identity mismatch: missing={missing_roots}, extra={extra_roots}"
        )
    result = {
        "schema": "zzz-corin-ripper-prefab-reference-scan/v1",
        "projectRoot": str(args.project_root.resolve()),
        "manifest": str(args.manifest.resolve()),
        "placeholderGuid": PLACEHOLDER_GUID,
        "summary": aggregate(records),
        "hierarchyCompleteness": aggregate_hierarchy_completeness(records, hierarchy),
        "records": records,
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "manifest": str(args.manifest),
                "output": str(args.output),
                "prefabCount": result["summary"]["prefabCount"],
                "uniqueRootNames": result["summary"]["uniqueRootNames"],
                "objectCounts": result["summary"]["objectCounts"],
                "targetCategories": result["summary"]["targetCategories"],
                "rendererCompleteness": {
                    key: result["hierarchyCompleteness"][key]
                    for key in (
                        "expectedRendererCount",
                        "bestExportedRendererCount",
                        "rootsWithCompleteBestRenderers",
                        "rootsWithMissingBestRenderers",
                        "completeRendererSlotCopies",
                        "incompleteRendererSlotCopies",
                    )
                },
            },
            ensure_ascii=False,
        )
    )


if __name__ == "__main__":
    main()
