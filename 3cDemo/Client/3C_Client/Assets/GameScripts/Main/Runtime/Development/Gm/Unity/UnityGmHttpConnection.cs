using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ThirdPerson.Development.Gm
{
    public sealed class UnityGmHttpConnection : IGmCommandConnection
    {
        readonly GmClientManifest m_Manifest;
        readonly List<PendingHttp> m_Pending = new List<PendingHttp>();
        readonly Queue<GmCommandResponse> m_Responses = new Queue<GmCommandResponse>();

        public UnityGmHttpConnection(GmClientManifest manifest)
        {
            manifest.RequireValid();
            m_Manifest = manifest;
        }

        public GmConnectionState State { get; private set; }
        public string Endpoint => m_Manifest.endpoint;
        public string StatusMessage { get; private set; } = "尚未连接";
        public GmServiceDescription Service { get; private set; }

        public void Connect()
        {
            if (State != GmConnectionState.Disconnected)
                throw new InvalidOperationException("GM 连接正在使用，须先断开。");
            State = GmConnectionState.Connecting;
            StatusMessage = "正在校验服务、构建和会话身份";
            Send(null);
        }

        public void Disconnect()
        {
            foreach (PendingHttp pending in m_Pending)
            {
                pending.Request.Abort();
                pending.Request.Dispose();
            }
            m_Pending.Clear();
            m_Responses.Clear();
            Service = null;
            State = GmConnectionState.Disconnected;
            StatusMessage = "已断开";
        }

        public bool TrySend(GmCommandRequest request, out string error)
        {
            error = string.Empty;
            if (State != GmConnectionState.Connected || m_Pending.Count + m_Responses.Count >= m_Manifest.maximumPendingRequests)
            {
                error = "GM 未连接或在途请求达到容量，未发送命令。";
                return false;
            }
            Send(request);
            return true;
        }

        void Send(GmCommandRequest command)
        {
            bool describe = command == null;
            var request = new UnityWebRequest(new Uri(new Uri(Endpoint),
                describe ? GmHttpProtocol.ServicePath : GmHttpProtocol.CommandsPath), describe ? "GET" : "POST");
            var download = new GmBoundedDownloadHandler(m_Manifest.maximumMessageBytes);
            request.downloadHandler = download;
            request.redirectLimit = 0;
            request.timeout = Mathf.CeilToInt(m_Manifest.requestTimeoutMilliseconds / 1000f);
            request.SetRequestHeader("Authorization", "Bearer " + m_Manifest.accessToken);
            if (!describe)
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonUtility.ToJson(command));
                if (payload.Length > m_Manifest.maximumMessageBytes)
                {
                    request.Dispose();
                    throw new InvalidOperationException("GM 请求超过消息容量。");
                }
                request.uploadHandler = new UploadHandlerRaw(payload);
                request.SetRequestHeader("Content-Type", "application/json");
            }
            m_Pending.Add(new PendingHttp(request, download, request.SendWebRequest(), command));
        }

        public void Pump()
        {
            for (int i = m_Pending.Count - 1; i >= 0; i--)
            {
                PendingHttp pending = m_Pending[i];
                if (!pending.Operation.isDone)
                    continue;
                m_Pending.RemoveAt(i);
                using (pending.Request)
                {
                    try
                    {
                        if (pending.Download.CapacityExceeded)
                            throw new InvalidOperationException("GM 响应超过消息容量。");
                        if (pending.Request.result == UnityWebRequest.Result.ConnectionError)
                            throw new InvalidOperationException("GM 网络连接失败或等待超时。");
                        if (pending.Request.responseCode != 200)
                        {
                            if (pending.Command == null)
                                throw new InvalidOperationException($"GM 连接校验失败：HTTP {pending.Request.responseCode}。");
                            GmResultCode code = pending.Request.responseCode == 401 ? GmResultCode.Unauthorized :
                                pending.Request.responseCode == 504 ? GmResultCode.TimedOut : GmResultCode.TargetUnavailable;
                            m_Responses.Enqueue(new GmCommandResponse
                            {
                                requestId = pending.Command.requestId, serviceInstanceId = pending.Command.serviceInstanceId,
                                sessionId = pending.Command.sessionId, code = code,
                                message = $"服务未返回命令结果，HTTP {pending.Request.responseCode}。"
                            });
                            continue;
                        }
                        if (pending.Command == null)
                        {
                            GmServiceDescription service = JsonUtility.FromJson<GmServiceDescription>(pending.Download.Text);
                            ValidateService(service);
                            Service = service;
                            State = GmConnectionState.Connected;
                            StatusMessage = "已连接，身份校验通过";
                        }
                        else
                        {
                            GmCommandResponse response = JsonUtility.FromJson<GmCommandResponse>(pending.Download.Text);
                            ValidateResponse(response, pending.Command.requestId);
                            m_Responses.Enqueue(response);
                        }
                    }
                    catch (Exception exception)
                    {
                        Disconnect();
                        StatusMessage = exception.Message;
                        return;
                    }
                }
            }
        }

        public bool TryDequeueResponse(out GmCommandResponse response)
        {
            response = m_Responses.Count == 0 ? null : m_Responses.Dequeue();
            return response != null;
        }

        void ValidateService(GmServiceDescription service)
        {
            if (service == null || service.protocolVersion != GmHttpProtocol.Version || service.buildId != m_Manifest.buildId ||
                service.sessionId != m_Manifest.sessionId || string.IsNullOrWhiteSpace(service.serviceInstanceId) ||
                service.commands == null || service.commands.Length == 0 || service.commands.Length > 64)
                throw new InvalidOperationException("GM 服务协议、构建或目标会话不匹配。");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (GmCommandDefinition command in service.commands)
            {
                if (command == null || !GmCommandSyntax.IsValidCommandId(command.id) || command.version <= 0 || !ids.Add(command.id))
                    throw new InvalidOperationException("GM 服务命令目录无效。");
            }
        }

        static void ValidateResponse(GmCommandResponse response, string requestId)
        {
            if (response == null || response.requestId != requestId || response.code == GmResultCode.Unspecified ||
                !Enum.IsDefined(typeof(GmResultCode), response.code) || response.sections == null || response.sections.Length > 64)
                throw new InvalidOperationException("GM 请求关联或结果格式无效。");
            foreach (GmResultSection section in response.sections)
            {
                if (section == null || section.fields == null || section.fields.Length > 128)
                    throw new InvalidOperationException("GM 结果段格式无效。");
                foreach (GmResultField field in section.fields)
                {
                    if (field == null || !Enum.IsDefined(typeof(GmValueKind), field.kind))
                        throw new InvalidOperationException("GM 结果字段类型无效。");
                }
            }
        }

        public void Dispose() => Disconnect();

        sealed class PendingHttp
        {
            public PendingHttp(UnityWebRequest request, GmBoundedDownloadHandler download,
                UnityWebRequestAsyncOperation operation, GmCommandRequest command)
            {
                Request = request; Download = download; Operation = operation; Command = command;
            }
            public UnityWebRequest Request { get; }
            public GmBoundedDownloadHandler Download { get; }
            public UnityWebRequestAsyncOperation Operation { get; }
            public GmCommandRequest Command { get; }
        }
    }
}
