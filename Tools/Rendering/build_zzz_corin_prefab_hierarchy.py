import argparse
import json
from pathlib import Path


def pptr_key(reference):
    return reference["m_FileID"], reference["m_PathID"]


def load_objects(manifest_path):
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    manifest_dir = manifest_path.parent
    objects = {}
    object_types = {}
    records = []
    for item in manifest["objects"]:
        object_types[item["cab"], item["pathId"]] = item["type"]
        if item["type"] not in {"GameObject", "Transform"}:
            records.append(item)
            continue
        path = manifest_dir / item["json"]
        decoded = json.loads(path.read_text(encoding="utf-8"))
        key = item["cab"], item["pathId"]
        if key in objects:
            raise RuntimeError(f"duplicate hierarchy object: {key}")
        objects[key] = decoded
        records.append(item)
    return manifest, objects, object_types


def build_component(cab, reference, objects, object_types):
    file_id, path_id = pptr_key(reference)
    if file_id != 0:
        return {"fileId": file_id, "pathId": path_id, "type": "External"}
    record = objects.get((cab, path_id))
    type_name = record["type"] if record is not None else object_types.get((cab, path_id))
    if type_name is None:
        return {"fileId": 0, "pathId": path_id, "type": "Missing"}
    return {"fileId": 0, "pathId": path_id, "type": type_name}


def find_transform(cab, game_object, objects, object_types):
    for component in game_object["object"]["m_Component"]:
        component_record = build_component(
            cab, component["component"], objects, object_types
        )
        if component_record["type"] != "Transform":
            continue
        transform = objects[(cab, component_record["pathId"])]
        if pptr_key(transform["object"]["m_GameObject"]) != (0, game_object["pathId"]):
            raise RuntimeError(f"transform does not own game object: {cab}/{game_object['pathId']}")
        return transform, component_record["pathId"]
    return None, None


def build_node(cab, game_object_path_id, objects, object_types, visiting):
    key = cab, game_object_path_id
    if key in visiting:
        raise RuntimeError(f"GameObject cycle: {key}")
    visiting.add(key)
    game_object = objects[key]
    transform, transform_path_id = find_transform(
        cab, game_object, objects, object_types
    )
    children = []
    if transform is not None:
        for reference in transform["object"]["m_Children"]:
            file_id, child_path_id = pptr_key(reference)
            if file_id != 0:
                children.append(
                    {
                        "fileId": file_id,
                        "pathId": child_path_id,
                        "resolved": False,
                    }
                )
                continue
            child_key = cab, child_path_id
            if child_key not in objects:
                children.append(
                    {"fileId": 0, "pathId": child_path_id, "resolved": False}
                )
                continue
            child_transform = objects[child_key]
            child_game_object = child_transform["object"]["m_GameObject"]
            children.append(
                build_node(
                    cab,
                    child_game_object["m_PathID"],
                    objects,
                    object_types,
                    visiting,
                )
            )
    visiting.remove(key)
    components = [
        build_component(cab, item["component"], objects, object_types)
        for item in game_object["object"]["m_Component"]
    ]
    node = {
        "gameObject": {"fileId": 0, "pathId": game_object_path_id},
        "name": game_object["object"]["m_Name"],
        "layer": game_object["object"]["m_Layer"],
        "tag": game_object["object"]["m_Tag"],
        "isActive": game_object["object"]["m_IsActive"],
        "components": components,
        "children": children,
    }
    if transform is not None:
        node["transform"] = {
            "pathId": transform_path_id,
            "localRotation": transform["object"]["m_LocalRotation"],
            "localPosition": transform["object"]["m_LocalPosition"],
            "localScale": transform["object"]["m_LocalScale"],
            "father": transform["object"]["m_Father"],
        }
    return node


def unresolved_links(node):
    count = 0
    for component in node["components"]:
        if component["type"] in {"External", "Missing"}:
            count += 1
    for child in node["children"]:
        if not child.get("resolved", True):
            count += 1
        else:
            count += unresolved_links(child)
    return count


def count_nodes(node):
    return 1 + sum(count_nodes(child) for child in node["children"] if child.get("resolved", True))


def build_hierarchies(manifest_path, output_path):
    manifest, objects, object_types = load_objects(manifest_path)
    roots = []
    for root in manifest["roots"]:
        key = root["cab"], root["pathId"]
        if key not in objects:
            raise RuntimeError(f"missing root GameObject: {key}")
        node = build_node(root["cab"], root["pathId"], objects, object_types, set())
        roots.append(
            {
                "source": root,
                "nodeCount": count_nodes(node),
                "unresolvedLinks": unresolved_links(node),
                "root": node,
            }
        )

    result = {
        "schema": "zzz-corin-prefab-hierarchy/v1",
        "manifest": str(manifest_path.resolve()),
        "rootCount": len(roots),
        "records": roots,
    }
    result["gameObjectCount"] = sum(item["nodeCount"] for item in roots)
    result["unresolvedLinkCount"] = sum(item["unresolvedLinks"] for item in roots)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    output_path.write_text(
        json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    print(
        json.dumps(
            {
                "manifest": str(manifest_path),
                "output": str(output_path),
                "rootCount": len(roots),
                "gameObjectCount": result["gameObjectCount"],
                "unresolvedLinkCount": result["unresolvedLinkCount"],
            }
        )
    )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--manifest", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    build_hierarchies(args.manifest, args.output)


if __name__ == "__main__":
    main()
