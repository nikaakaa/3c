using System;
using MCPForUnity.Editor.Constants;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport.Transports;
using UnityEditor;

namespace MCPForUnity.Editor
{
    public static class McpCiBoot
    {
        public static async void StartHttpForCi()
        {
            EditorPrefs.SetBool(EditorPrefKeys.UseHttpTransport, true);
            EditorPrefs.SetBool(EditorPrefKeys.AutoStartOnLoad, true);
            if (!await MCPServiceLocator.Bridge.StartAsync())
                throw new InvalidOperationException("Unity MCP HTTP bridge failed to start.");
        }

        public static void StartStdioForCi()
        {
            try 
            { 
                EditorPrefs.SetBool(EditorPrefKeys.UseHttpTransport, false); 
            }
            catch { /* ignore */ }

            StdioBridgeHost.StartAutoConnect();
        }
    }
}
