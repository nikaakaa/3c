using System;
using System.Linq;
using System.Text;
using ThirdPersonCharacter.Pipeline.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ThirdPerson.Development.Gm
{
    [DefaultExecutionOrder(-11000)]
    public sealed class GmConsoleView : MonoBehaviour
    {
        GmConsoleModel m_Model;
        CharacterDeviceInputFocus m_Focus;
        GmDevelopmentProfile m_Profile;
        InputActionAsset m_Actions;
        InputAction m_Toggle;
        IDisposable m_FocusCapture;
        Font m_Font;
        GUIStyle m_TextStyle;
        GUIStyle m_InputStyle;
        GUIStyle m_ButtonStyle;
        string m_Command = string.Empty;
        string m_Output = string.Empty;
        string m_Connection = string.Empty;
        string m_Identity = string.Empty;
        ulong m_OutputRevision = ulong.MaxValue;
        Vector2 m_Scroll;
        bool m_Open;
        bool m_FocusField;
        bool m_Submit;
        bool m_ChangeConnection;
        bool m_Clear;
        CursorLockMode m_PreviousCursorLock;
        bool m_PreviousCursorVisible;

        public void Initialize(GmConsoleModel model, CharacterDeviceInputFocus focus, GmDevelopmentProfile profile)
        {
            if (!Debug.isDebugBuild)
                throw new InvalidOperationException("GM 控制台只能装配到 Development Player。");
            m_Model = model;
            m_Focus = focus;
            m_Profile = profile;
            if (!Font.GetOSInstalledFontNames().Contains(profile.FontFamily, StringComparer.Ordinal))
                throw new InvalidOperationException($"GM 配置的字体不可用：{profile.FontFamily}");
            m_Font = Font.CreateDynamicFontFromOSFont(profile.FontFamily, profile.FontSize);
            m_Actions = Instantiate(profile.Actions);
            m_Toggle = m_Actions.FindAction(profile.ToggleActionId, true);
            m_Toggle.performed += Toggle;
            m_Toggle.Enable();
            m_Model.Connect();
        }

        void Toggle(InputAction.CallbackContext context) => SetOpen(!m_Open);

        void SetOpen(bool open)
        {
            if (m_Open == open)
                return;
            m_Open = open;
            if (open)
            {
                m_FocusCapture = m_Focus.Acquire();
                m_PreviousCursorLock = Cursor.lockState;
                m_PreviousCursorVisible = Cursor.visible;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_FocusField = true;
            }
            else
            {
                m_FocusCapture.Dispose();
                m_FocusCapture = null;
                Cursor.lockState = m_PreviousCursorLock;
                Cursor.visible = m_PreviousCursorVisible;
            }
        }

        void Update()
        {
            if (m_Model == null)
                return;
            m_Model.Pump(Time.realtimeSinceStartupAsDouble);
            if (m_ChangeConnection)
            {
                m_ChangeConnection = false;
                if (m_Model.ConnectionState == GmConnectionState.Disconnected)
                    m_Model.Connect();
                else
                    m_Model.Disconnect();
            }
            if (m_Clear)
            {
                m_Clear = false;
                m_Model.ClearOutput();
            }
            if (m_Submit)
            {
                m_Submit = false;
                if (m_Model.Submit(m_Command, Time.realtimeSinceStartupAsDouble))
                    m_Command = string.Empty;
                m_FocusField = true;
            }
            m_Connection = $"{m_Model.ConnectionState} · {m_Model.Endpoint} · 在途 {m_Model.PendingCount} · {m_Model.ConnectionMessage}";
            GmServiceDescription service = m_Model.Service;
            m_Identity = service == null ? "尚未绑定服务实例" : $"Session: {service.sessionId}\nService: {service.serviceInstanceId}";
            if (m_OutputRevision == m_Model.OutputRevision)
                return;
            m_OutputRevision = m_Model.OutputRevision;
            var text = new StringBuilder();
            foreach (GmConsoleOutput output in m_Model.Output)
            {
                text.Append('>').Append(output.CommandLine).Append("  [").Append(output.State).Append("] ")
                    .Append(output.RequestId).Append('\n').Append(output.Text).Append("\n\n");
            }
            m_Output = text.ToString();
            m_Scroll.y = float.MaxValue;
        }

        void OnGUI()
        {
            if (!m_Open || m_Model == null)
                return;
            GUI.depth = -100;
            EnsureStyles();
            Event input = Event.current;
            if (input.type == EventType.KeyDown)
            {
                if (input.keyCode == KeyCode.Escape)
                {
                    SetOpen(false);
                    input.Use();
                    return;
                }
                if (input.keyCode == KeyCode.Return || input.keyCode == KeyCode.KeypadEnter)
                {
                    m_Submit = true;
                    input.Use();
                }
                else if (input.keyCode == KeyCode.UpArrow)
                {
                    m_Command = m_Model.PreviousHistory(m_Command);
                    input.Use();
                }
                else if (input.keyCode == KeyCode.DownArrow)
                {
                    m_Command = m_Model.NextHistory();
                    input.Use();
                }
            }
            GUILayout.BeginArea(new Rect(12, 12, Mathf.Max(320, Screen.width - 24), Mathf.Max(260, Screen.height - 24)), GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Rollback GM · ` 打开/关闭 · Esc 关闭 · ↑↓ 历史", m_TextStyle);
            if (GUILayout.Button(m_Model.ConnectionState == GmConnectionState.Disconnected ? "连接" : "断开", m_ButtonStyle, GUILayout.Width(64)))
                m_ChangeConnection = true;
            if (GUILayout.Button("清屏", m_ButtonStyle, GUILayout.Width(64)))
                m_Clear = true;
            GUILayout.EndHorizontal();
            GUILayout.Label(m_Connection, m_TextStyle);
            GUILayout.Label(m_Identity, m_TextStyle);
            m_Scroll = GUILayout.BeginScrollView(m_Scroll, GUILayout.ExpandHeight(true));
            GUILayout.Label(m_Output, m_TextStyle);
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("GmCommand");
            m_Command = GUILayout.TextField(m_Command, GmCommandLineParser.MaximumLineLength, m_InputStyle, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("发送", m_ButtonStyle, GUILayout.Width(64)))
                m_Submit = true;
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (m_FocusField && input.type == EventType.Repaint)
            {
                GUI.FocusControl("GmCommand");
                m_FocusField = false;
            }
        }

        void EnsureStyles()
        {
            if (m_TextStyle != null)
                return;
            m_TextStyle = new GUIStyle(GUI.skin.label) { font = m_Font, fontSize = m_Profile.FontSize, wordWrap = true, richText = false };
            m_InputStyle = new GUIStyle(GUI.skin.textField) { font = m_Font, fontSize = m_Profile.FontSize, richText = false };
            m_ButtonStyle = new GUIStyle(GUI.skin.button) { font = m_Font, fontSize = m_Profile.FontSize };
        }

        void OnEnable() => m_Toggle?.Enable();

        void OnDisable()
        {
            SetOpen(false);
            m_Toggle?.Disable();
        }

        void OnDestroy()
        {
            SetOpen(false);
            if (m_Toggle != null)
            {
                m_Toggle.performed -= Toggle;
                m_Toggle.Disable();
            }
            if (m_Actions)
                Destroy(m_Actions);
            if (m_Font)
                Destroy(m_Font);
            m_Model?.Dispose();
        }
    }
}
