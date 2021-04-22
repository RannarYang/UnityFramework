/*
 * @Author       : RannarYang
 * @Date         : 2021-04-21 15:12:46
 * @LastEditors  : RannarYang
 * @LastEditTime : 2021-04-22 15:48:30
 * @FilePath     : \Client\Assets\Editor\BundleEditor.cs
 */
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BundleEditor
{   
    private static string ABCONFIGPATH = "Assets/Config/ABConfig.asset";
    private static string m_BundleTargetPath = Application.streamingAssetsPath;
    
    // key是AB包名，value是路径，所有文件ab包dic
    private static Dictionary<string, string> m_AllFileDir = new Dictionary<string, string>();

    // 过滤的list
    private static List<string> m_AllFileAB = new List<string>(); 
    // 单个prefab的AB包
    private static Dictionary<string, List<string>> m_AllPrefabDir = new Dictionary<string, List<string>>();

    // 储存所有有效路径
    private static List<string> m_ConfigFile = new List<string>();

    [MenuItem("Tools/打包")]
    private static void Build() {
        ABConfig abConfig = AssetDatabase.LoadAssetAtPath<ABConfig>(ABCONFIGPATH); 

        m_AllFileAB.Clear();
        m_AllFileDir.Clear();
        m_AllPrefabDir.Clear();
        m_ConfigFile.Clear();
        
        foreach(ABConfig.FileDirABName fileDir in abConfig.m_AllFileDirAB) {
            if(m_AllFileDir.ContainsKey(fileDir.ABName)) {
                Debug.LogError("AB包配置名字重复，请检查！");
                continue;
            }
            m_AllFileDir.Add(fileDir.ABName, fileDir.Path);
            m_AllFileAB.Add(fileDir.Path);
            m_ConfigFile.Add(fileDir.Path);
        }

        string[] allPrefab = AssetDatabase.FindAssets("t:Prefab", abConfig.m_AllPrefabPath.ToArray());
        for(int i = 0; i < allPrefab.Length; i++) {
            string path = AssetDatabase.GUIDToAssetPath(allPrefab[i]);
            EditorUtility.DisplayProgressBar("查找Prefab", "Prefab:" + path, i * 1.0f / allPrefab.Length );
            m_ConfigFile.Add(path);
            if(!ContainAllFileAB(path)) {
                GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] allDepend = AssetDatabase.GetDependencies(path);
                List<string> allDependPath = new List<string>();

                for(int j = 0; j < allDepend.Length; j++) {
                    if(!ContainAllFileAB(allDepend[j]) && !allDepend[j].EndsWith(".cs")) {
                        m_AllFileAB.Add(allDepend[j]);
                        allDependPath.Add(allDepend[j]);
                    }
                }
                if(m_AllPrefabDir.ContainsKey(obj.name)) {
                    Debug.LogError("存在相同名字的prefab!: " + obj.name);
                } else {
                    m_AllPrefabDir.Add(obj.name, allDependPath);
                }

            }
        }

        // 清理AB包名
        string[] oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for(int i = 0; i < oldABNames.Length; i++) {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字： " + oldABNames[i], i * 1.0f/oldABNames.Length);
        }

        foreach(string name in m_AllFileDir.Keys) {
            SetABName(name, m_AllFileDir[name]);
        }

        foreach(string name in m_AllPrefabDir.Keys) {
            SetABName(name, m_AllPrefabDir[name]);
        }

        BuildAssetBundle();

        // 清理AB包名
        oldABNames = AssetDatabase.GetAllAssetBundleNames();
        for(int i = 0; i < oldABNames.Length; i++) {
            AssetDatabase.RemoveAssetBundleName(oldABNames[i], true);
            EditorUtility.DisplayProgressBar("清除AB包名", "名字： " + oldABNames[i], i * 1.0f/oldABNames.Length);
        }

        AssetDatabase.Refresh();
        EditorUtility.ClearProgressBar();

    }

    static void SetABName(string name, string path) {
        AssetImporter assetImporter = AssetImporter.GetAtPath(path);
        if(assetImporter == null) {
            Debug.LogError("不存在此路径文件:" + path);
        } else {
            assetImporter.assetBundleName = name;
        }
    }

    static void SetABName(string name, List<string> paths) {
        for(int i = 0; i < paths.Count; i++) {
            SetABName(name, paths[i]);
        }
    }

    static void BuildAssetBundle() {
        string[] allBundles = AssetDatabase.GetAllAssetBundleNames();
        // key 为全路径，value为包名
        Dictionary<string, string> resPathDic = new Dictionary<string, string>();
        for(int i = 0; i < allBundles.Length; i++) {
            string[] allBundlePath = AssetDatabase.GetAssetPathsFromAssetBundle(allBundles[i]);
            for(int j = 0; j < allBundlePath.Length; j++) {
                if(allBundlePath[j].EndsWith(".cs")) continue;
                Debug.Log("此AB包： " + allBundles[i] + "下面包含的资源文件路径: " + allBundlePath[j]);
                if(ValidPath(allBundlePath[j])) {
                    resPathDic.Add(allBundlePath[j], allBundles[i]);
                }
            }
        }
        
        // 删除没用的AB包
        DeleteAB();
        // 生成自己的配置表
        WriteData(resPathDic);

        BuildPipeline.BuildAssetBundles(m_BundleTargetPath, BuildAssetBundleOptions.ChunkBasedCompression, EditorUserBuildSettings.activeBuildTarget);
    }

    static void WriteData(Dictionary<string, string> resPathDic) {
        AssetBundleConfig config = new AssetBundleConfig();
        config.ABList = new List<ABBase>();
        foreach(string path in resPathDic.Keys) {
            ABBase abBase = new ABBase();
            abBase.Path =  path;
            abBase.Crc = Crc32.GetCrc32(path);
            abBase.ABName = resPathDic[path];
            abBase.AssetName = path.Remove(0, path.LastIndexOf("/") + 1);
            abBase.ABDependency = new List<string>();
            string[] resDependency = AssetDatabase.GetDependencies(path);
            for(int i = 0; i < resDependency.Length; i++) {
                string tempPath = resDependency[i];
                if(tempPath == path || path.EndsWith(".cs")) continue;
                
                if(resPathDic.TryGetValue(tempPath, out string abName)) {
                    if(abName == resPathDic[path]) continue;
                    if(!abBase.ABDependency.Contains(abName)) {
                        abBase.ABDependency.Add(abName);
                    }
                }
            }
            config.ABList.Add(abBase);
        }

        // 写入xml
        string xmlPath = Application.dataPath + "/AssetBundleConfig.xml";
        if(File.Exists(xmlPath)) File.Delete(xmlPath);
        FileStream fileStream = new FileStream(xmlPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        StreamWriter sw = new StreamWriter(fileStream, System.Text.Encoding.UTF8);
        XmlSerializer xs = new XmlSerializer(config.GetType());
        xs.Serialize(sw,config);
        sw.Close();
        fileStream.Close();

        // 写入二进制
        foreach(ABBase abBase in config.ABList) {
            abBase.Path = string.Empty;
        }
        string bytePath = "Assets/Res/Data/ABData/AssetBundleConfig.bytes";
        FileStream fs = new FileStream(bytePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        BinaryFormatter bf = new BinaryFormatter();
        bf.Serialize(fs, config);
        fs.Close();
    }

    /// <summary>
    /// 删除无用的AB包
    /// </summary>
    static void DeleteAB() {
        string[] allBundlesName = AssetDatabase.GetAllAssetBundleNames();
        DirectoryInfo directory = new DirectoryInfo(m_BundleTargetPath);

        FileInfo[] files = directory.GetFiles("*", SearchOption.AllDirectories);
        for(int i = 0; i < files.Length; i++) {
            if(ContainABName(files[i].Name, allBundlesName) || files[i].Name.EndsWith(".meta")) {
                continue;
            } else {
                Debug.Log("此AB包已经被删除或者改名了: " + files[i].Name);
                if(File.Exists(files[i].FullName)) {
                    File.Delete(files[i].FullName);
                }
            }
        }
    }
    /// <summary>
    /// 遍历文件夹里的文件名与设置的所有AB包进行检查判断
    /// </summary>
    /// <param name="name"></param>
    /// <param name="strs"></param>
    /// <returns></returns>
    static bool ContainABName(string name, string[] strs) {
        for(int i = 0; i < strs.Length; i++) {
            if(name == strs[i]) return true;
        }
        return false;
    }

    /// <summary>
    /// 是否包含在已有的AB包里，用来做AB包冗余清除
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ContainAllFileAB(string path) {
        for(int i = 0; i < m_AllFileAB.Count; i++) {
            if(path == m_AllFileAB[i] || (path.Contains(m_AllFileAB[i]) && (path.Replace(m_AllFileAB[i], "")[0] == '/'))) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否是有效路径
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    static bool ValidPath(string path) {
        for(int i = 0; i < m_ConfigFile.Count; i++) {
            if(path.Contains(m_ConfigFile[i])) {
                return true;
            }
        }
        return false;
    }
}
