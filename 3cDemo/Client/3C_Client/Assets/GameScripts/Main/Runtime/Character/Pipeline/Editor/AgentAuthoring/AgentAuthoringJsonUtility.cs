using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class AgentAuthoringJsonUtility
    {
        public static string ToJson<T>(T value)
        {
            return JsonUtility.ToJson(value, true);
        }

        public static bool TryFromJson<T>(string json, out T value, AgentCompileReport report, string path) where T : class
        {
            value = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                report?.Error(path, "empty_json", "JSON 内容为空。");
                return false;
            }

            try
            {
                value = JsonUtility.FromJson<T>(json);
                return value != null;
            }
            catch (System.Exception exception)
            {
                report?.Error(path, "json_parse_error", exception.Message);
                return false;
            }
        }

        public static bool TryReadFile<T>(string path, out T value, AgentCompileReport report) where T : class
        {
            value = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                report?.Error(path, "file_missing", "文件不存在。");
                return false;
            }

            return TryFromJson(File.ReadAllText(path, Encoding.UTF8), out value, report, path);
        }

        public static void SaveJsonPanel<T>(string title, string defaultName, T value)
        {
            string path = EditorUtility.SaveFilePanel(title, Application.dataPath, defaultName, "json");
            if (string.IsNullOrEmpty(path))
                return;

            File.WriteAllText(path, ToJson(value), Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
