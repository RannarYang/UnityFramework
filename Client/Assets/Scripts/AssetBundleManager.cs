using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

class AssetBundleManager : Singleton<AssetBundleManager>{
    // 资源关系依赖配置表，可以根据crc来找到对应的资源块
    protected Dictionary<uint, ResourceItem> m_ResourceItemDic = new Dictionary<uint, ResourceItem>();
    // 储存已经加载的AB包，key为crc
    protected Dictionary<uint, AssetBundleItem> m_AssetBundleItemDic = new Dictionary<uint, AssetBundleItem>();
    // AssetBundleItem类对象池
    protected ClassObjectPool<AssetBundleItem> m_AssetBundleItemPool = ObjectManager.Instance.GetOrCreateClassPool<AssetBundleItem>(500);
    /// <summary>
    /// 加载AB配置表
    /// </summary>
    /// <returns></returns>
    public bool LoadAssetBundleConfig() {

        m_ResourceItemDic.Clear();

        string configPath = Application.streamingAssetsPath + "/assetbundleconfig";
        AssetBundle configAB = AssetBundle.LoadFromFile(configPath);
        TextAsset textAsset = configAB.LoadAsset<TextAsset>("assetbundleconfig");
        if(null == textAsset) {
            Debug.LogError("AssetBundleConfig is not exist!");
            return false;
        }

        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig config = (AssetBundleConfig)bf.Deserialize(stream);
        stream.Close();

        for(int i = 0; i < config.ABList.Count; i++) {
            ABBase abBase = config.ABList[i];
            ResourceItem item = new ResourceItem();
            item.m_Crc = abBase.Crc;
            item.m_AssetName = abBase.AssetName;
            item.m_ABName = abBase.ABName;
            item.m_DependAssetBundle = abBase.ABDependency;

            if(m_ResourceItemDic.ContainsKey(item.m_Crc)) {
                Debug.LogError("重复的Crc 资源名: " + item.m_AssetName + " ab包名：" + item.m_ABName);
            } else {
                m_ResourceItemDic.Add(item.m_Crc, item);
            }
        }

        return true;
    }
    /// <summary>
    /// 根据路径的Crc加载中间类ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem LoadResourceAssetBundle(uint crc) {
        if(!m_ResourceItemDic.TryGetValue(crc, out ResourceItem item) || item == null) {
            Debug.LogError($"LoadResourceAssetBundle error: can not find crc {crc.ToString()} in AssetBundleConfig");
            return item;
        }
        if(item.m_AssetBundle != null) {
            return item;
        }

        item.m_AssetBundle = LoadAssetBundle(item.m_ABName);
        if(item.m_DependAssetBundle != null) {
            for(int i = 0; i < item.m_DependAssetBundle.Count; i++) {
                LoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        return item;
    }
    /// <summary>
    /// 根据名字加载单个AssetBundle
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    private AssetBundle LoadAssetBundle(string name) {
        uint crc = Crc32.GetCrc32(name);

        if(!m_AssetBundleItemDic.TryGetValue(crc, out AssetBundleItem item)) {
            AssetBundle assetBundle = null;
            string fullPath = Application.streamingAssetsPath + "/" + name;
            if(File.Exists(fullPath)) {
                assetBundle = AssetBundle.LoadFromFile(fullPath);
            }

            if(assetBundle == null) {
                Debug.LogError("Load AssetBundle Error: " + fullPath);
            }

            item = m_AssetBundleItemPool.Spawn(true);
            item.assetBundle = assetBundle;
            item.RefCount++;
            m_AssetBundleItemDic.Add(crc, item);
        } else {
            item.RefCount++;
        }
        return item.assetBundle;
    }
    /// <summary>
    /// 释放资源
    /// </summary>
    /// <param name="item"></param>
    public void ReleaseAsset(ResourceItem item) {
        if(null == item) {
            return;
        }
        if(item.m_DependAssetBundle != null && item.m_DependAssetBundle.Count > 0) {
            for(int i = 0; i < item.m_DependAssetBundle.Count; i++) {
                UnLoadAssetBundle(item.m_DependAssetBundle[i]);
            }
        }
        UnLoadAssetBundle(item.m_ABName);
    }

    private void UnLoadAssetBundle(string name) {
        uint crc = Crc32.GetCrc32(name);
        if(m_AssetBundleItemDic.TryGetValue(crc, out AssetBundleItem item) && item != null) {
            item.RefCount--;
            if(item.RefCount <= 0 && item.assetBundle != null) {
                item.assetBundle.Unload(true);
                item.Reset();
                m_AssetBundleItemPool.Recycle(item);
                m_AssetBundleItemDic.Remove(crc);
            }
        }
    }
    /// <summary>
    /// 根据crc查找ResourceItem
    /// </summary>
    /// <param name="crc"></param>
    /// <returns></returns>
    public ResourceItem FindResourceItem(uint crc) {
        return m_ResourceItemDic[crc];
    }
}



class AssetBundleItem {
    public AssetBundle assetBundle = null;
    public int RefCount;
    public void Reset() {
        assetBundle = null;
        RefCount = 0;
    }
}

class ResourceItem {
    // 资源路径的Crc
    public uint m_Crc = 0;
    // 该资源的文件名
    public string m_AssetName = string.Empty;
    // 该资源所在的AssetBundle 名字
    public string m_ABName = string.Empty;
    // 该资源所依赖的AssetBundle
    public List<string> m_DependAssetBundle = null;
    // 该资源加载完的AB包
    public AssetBundle m_AssetBundle = null; 

    // -------------------------------------------
    // 资源对象
    public Object m_Obj = null;
    // 资源唯一标识
    public int m_Guid = 0;
    // 资源最后所使用的时间
    public float m_LastUseTime = 0.0f;
    // 引用计数
    protected int m_RefCount = 0;
    // 是否跳场景清掉
    public bool m_Clear = true;
    public int RefCount {
        get {
            return m_RefCount;
        }
        set {
            m_RefCount = value;
            if(m_RefCount < 0) {
                Debug.LogError("ref count < 0" + m_RefCount + "," + (m_Obj != null ? m_Obj.name : "name is null"));
            }
        }
    }
}