using System;
using System.Collections.Generic;
using UnityEngine;

namespace TEngine
{
    /// <summary>
    /// 强制更新类型。
    /// </summary>
    public enum UpdateStyle
    {
        /// <summary>
        /// 强制更新(不更新无法进入游戏。)
        /// </summary>
        Force = 1,

        /// <summary>
        /// 非强制(不更新可以进入游戏。)
        /// </summary>
        Optional = 2,
    }

    /// <summary>
    /// 是否提示更新。
    /// </summary>
    public enum UpdateNotice
    {
        /// <summary>
        /// 更新存在提示。
        /// </summary>
        Notice = 1,

        /// <summary>
        /// 更新非提示。
        /// </summary>
        NoNotice = 2,
    }
    /// <summary>
    /// WebGL平台下，
    /// StreamingAssets：跳过远程下载资源直接访问StreamingAssets
    /// Remote：访问远程资源
    /// </summary>
    public enum LoadResWayWebGL
    {
        Remote,
        StreamingAssets,
    }
    
    [CreateAssetMenu(menuName = "TEngine/UpdateSetting", fileName = "UpdateSetting")]
    public class UpdateSetting : ScriptableObject
    {
        public bool Enable
        {
            get
            {
#if ENABLE_HYBRIDCLR
                return true;
#else
                return false;
#endif
            }
        }

        [Header("Auto sync with [HybridCLRGlobalSettings]")]
        public List<string> HotUpdateAssemblies = new List<string>() { "GameBase.dll", "GameProto.dll", "BattleCore.dll", "GameLogic.dll" };

        [Header("Need manual setting!")]
        public List<string> AOTMetaAssemblies = new List<string>() { "mscorlib.dll", "System.dll", "System.Core.dll", "TEngine.Runtime.dll", "UniTask.dll", "YooAsset.dll", "Fantasy.Unity.dll", "Newtonsoft.Json.dll", "UnityEngine.CoreModule.dll" };

        /// <summary>
        /// Dll of main business logic assembly
        /// </summary>
        public string LogicMainDllName = "GameLogic.dll";

        /// <summary>
        /// 程序集文本资产打包Asset后缀名
        /// </summary>
        public string AssemblyTextAssetExtension = ".bytes";

        /// <summary>
        /// 程序集文本资产资源目录
        /// </summary>
        public string AssemblyTextAssetPath = "AssetRaw/HotUpdate/DLL";

        [Header("更新设置")]
        public UpdateStyle UpdateStyle = UpdateStyle.Force;

        public UpdateNotice UpdateNotice = UpdateNotice.Notice;

        /// <summary>
        /// WebGL平台加载本地资源/加载远程资源。
        /// </summary>
        [Header("WebGL设置")]
        [SerializeField]
        private LoadResWayWebGL LoadResWayWebGL = LoadResWayWebGL.Remote;
        /// <summary>
        /// 是否使用可寻址资源代替资源路径
        /// 说明：开启此项可以节省运行时清单占用的内存！
        /// </summary>
        [SerializeField, Tooltip("是否使用可寻址资源代替资源路径 说明：开启此项可以节省运行时清单占用的内存！")]
        private bool ReplaceAssetPathWithAddress = false;
        /// <summary>
        /// 获取是否使用可寻址资源代替资源路径
        /// </summary>
        /// <returns></returns>
        public bool GetReplaceAssetPathWithAddress()
            => ReplaceAssetPathWithAddress;
        
        /// <summary>
        /// 是否加载远程资源
        /// </summary>
        /// <returns></returns>
        public LoadResWayWebGL GetLoadResWayWebGL()
        {
            return LoadResWayWebGL;
        }
    }
}
