using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace QFramework
{
    public class AssetBundleSettings
    {
        private static IResDatas mAssetBundleConfigFile = null;

        /// <summary>
        /// 默认
        /// </summary>
        private static Func<IResDatas> mAssetBundleConfigFileFactory = () => new ResDatas();

        public static Func<IResDatas> AssetBundleConfigFileFactory
        {
            set { mAssetBundleConfigFileFactory = value; }
        }

        /// <summary>
        /// 获取自定义的 资源信息
        /// </summary>
        /// <returns></returns>
        public static IResDatas AssetBundleConfigFile
        {
            get
            {
                if (mAssetBundleConfigFile == null)
                {
                    mAssetBundleConfigFile = mAssetBundleConfigFileFactory.Invoke();
                }

                return mAssetBundleConfigFile;
            }
            set { mAssetBundleConfigFile = value; }
        }


        public static bool LoadAssetResFromStreammingAssetsPath
        {
            get { return PlayerPrefs.GetInt("LoadResFromStreammingAssetsPath", 1) == 1; }
            set { PlayerPrefs.SetInt("LoadResFromStreammingAssetsPath", value ? 1 : 0); }
        }


        #region AssetBundle 相关

        public static string AssetBundleUrl2Name(string url)
        {
            string retName = null;
            string parren = FromUnityToDll.Setting.StreamingAssetsPath + "AssetBundles/" +
                            FromUnityToDll.Setting.GetPlatformName() + "/";
            retName = url.Replace(parren, "");

            parren = FromUnityToDll.Setting.PersistentDataPath + "AssetBundles/" +
                     FromUnityToDll.Setting.GetPlatformName() + "/";
            retName = retName.Replace(parren, "");
            return retName;
        }

        public static string AssetBundleName2Url(string name)
        {
            string retUrl = FromUnityToDll.Setting.PersistentDataPath + "AssetBundles/" +
                            FromUnityToDll.Setting.GetPlatformName() + "/" + name;

            if (File.Exists(retUrl))
            {
                return retUrl;
            }

            return FromUnityToDll.Setting.StreamingAssetsPath + "AssetBundles/" +
                   FromUnityToDll.Setting.GetPlatformName() + "/" + name;
        }

        //导出目录

        /// <summary>
        /// AssetBundle存放路径
        /// </summary>
        public static string RELATIVE_AB_ROOT_FOLDER
        {
            get { return "/AssetBundles/" + FromUnityToDll.Setting.GetPlatformName() + "/"; }
        }

        #endregion


        public static string GetPlatformForAssetBundles(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                    return "WSAPlayer";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                case RuntimePlatform.WindowsPlayer:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                    return "OSX";
                case RuntimePlatform.LinuxPlayer:
                    return "Linux";
                // Add more build targets for your own.
                // If you add more targets, don't forget to add the same platforms to GetPlatformForAssetBundles(RuntimePlatform) function.
                default:
                    return null;
            }
        }
    }
}