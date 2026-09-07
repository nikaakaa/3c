using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor
{
    public static class ProjectEditorPreferences
    {
        static readonly string Prefix =
            $"ThirdPerson.Project.{Application.dataPath.Replace('\\', '/')}.";

        public static string GetString(string key, string defaultValue) =>
            EditorPrefs.GetString(Prefix + key, defaultValue);

        public static void SetString(string key, string value) =>
            EditorPrefs.SetString(Prefix + key, value);

        public static bool GetBool(string key, bool defaultValue) =>
            EditorPrefs.GetBool(Prefix + key, defaultValue);

        public static void SetBool(string key, bool value) =>
            EditorPrefs.SetBool(Prefix + key, value);

        public static void DeleteKey(string key) =>
            EditorPrefs.DeleteKey(Prefix + key);
    }
}
