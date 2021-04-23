using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;

public enum LoadResPriority {
    RES_HIGHT = 0, // 最高优先级
    RES_MIDDLE, // 一般优先级
    RES_SLOW, // 低优先级
    RES_NUM
}

public class AsyncLoadResParam {
    public List<AsyncCallBack> m_CallBackList = new List<AsyncCallBack>();
    public uint m_Crc;
    public string m_Path;
    public bool m_Sprite = false;
    public LoadResPriority m_Priority = LoadResPriority.RES_SLOW;

    public void Reset() {
        m_CallBackList.Clear();
        m_Crc = 0;
        m_Path = string.Empty;
        m_Sprite = false;
        m_Priority = LoadResPriority.RES_SLOW;
    }
}

public class AsyncCallBack {
    // 加载完成的回调
    public OnAsyncObjFinish m_DealFinish = null;
    // 回调参数
    public object m_Param1 = null, m_Param2 = null, m_Param3 = null;

    public void Reset() {
        m_DealFinish = null;
        m_Param1 = null;
        m_Param2 = null;
        m_Param3 = null;
    }
}

public delegate void OnAsyncObjFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null);
class ResourceManager: Singleton<ResourceManager> {
    public bool m_LoadFromAssetBundle = true;
    // 缓存使用的资源列表
    protected Dictionary<uint, ResourceItem> AssetDic {get; set;} = new Dictionary<uint, ResourceItem>();
    // 缓存引用计数为0的列表(不是实例化的资源，而是在内存中的，eq:图片，材质，未实例化的prefab)，达到缓存最大的时候，释放这个列表里最早没用的资源
    protected CMapList<ResourceItem> m_NoReferenceAssetMapList = new CMapList<ResourceItem>();
    // 中间回调类的类对象池
    protected ClassObjectPool<AsyncLoadResParam> m_AsyncLoadResParamPool = new ClassObjectPool<AsyncLoadResParam>(50);
    protected ClassObjectPool<AsyncCallBack> m_AsyncCallBackPool = new ClassObjectPool<AsyncCallBack>(100);
    // Mono脚本
    protected MonoBehaviour m_StartMono;
    // 正在异步加载的资源列表
    protected List<AsyncLoadResParam>[] m_LoadingAssetList = new List<AsyncLoadResParam>[(int)LoadResPriority.RES_NUM];
    // 正在异步加载的Dic
    protected Dictionary<uint, AsyncLoadResParam> m_LoadingAssetDic = new Dictionary<uint, AsyncLoadResParam>();

    // 最长连续卡着加载资源的时间，单位微秒
    private const long MAXLOADRESTIME = 20;
    public void Init(MonoBehaviour mono) {
        for(int i = 0; i < (int)LoadResPriority.RES_NUM; i++) {
            m_LoadingAssetList[i] = new List<AsyncLoadResParam>();
        }
        this.m_StartMono = mono;
        m_StartMono.StartCoroutine(AsyncLoadCor());
    }

    /// <summary>
    /// 清空缓存
    /// </summary>
    public void ClearCache() {
        List<ResourceItem> tempList = new List<ResourceItem>();
        foreach(ResourceItem item in AssetDic.Values) {
            if(item.m_Clear) {
                tempList.Add(item); 
            }
        }
        foreach(ResourceItem item in tempList) {
            DestroyResourceItem(item, true);
        }
        tempList.Clear();
        // while(m_NoReferenceAssetMapList.Size() > 0) {
        //     ResourceItem item = m_NoReferenceAssetMapList.Back();
        //     DestroyResourceItem(item, true);
        //     m_NoReferenceAssetMapList.Pop();
        // }
    }

    /// <summary>
    /// 预加载资源 
    /// </summary>
    /// <param name="path"></param>
    public void PreloadRes(string path) {
        if(string.IsNullOrEmpty(path)) return;

        uint crc = Crc32.GetCrc32(path);

        ResourceItem item = GetCacheResourceItem(crc, 0);
        if(item != null) {
            return;
        }

        Object obj = null;
#if UNITY_EDITOR
        if(!m_LoadFromAssetBundle) {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if(item.m_Obj != null) {
                obj = item.m_Obj;
            } else {
                obj = LoadAssetByEditor<Object>(path);
            }
        }
#endif
        if(obj == null) {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if(item != null && item.m_AssetBundle != null) {
                if(item.m_Obj != null) {
                    obj = item.m_Obj;
                } else {
                    obj = item.m_AssetBundle.LoadAsset<Object>(item.m_AssetName);
                }
            } 
        }

        CacheResource(path, ref item, crc, obj);
        // 跳场景不清空缓存
        item.m_Clear = false;
        ReleaseResource(obj, false);
    }

    /// <summary>
    /// 同步资源加载，外部直接调用，仅加载不需要实例化的资源，例如Texture、图片、音频等等
    /// </summary>
    /// <param name="path"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T LoadResource<T> (string path) where T: UnityEngine.Object {
        if(string.IsNullOrEmpty(path)) return null;
        uint crc = Crc32.GetCrc32(path);

        ResourceItem item = GetCacheResourceItem(crc);
        if(item != null) {
            return item.m_Obj as T;
        }

        T obj = null;
#if UNITY_EDITOR
        if(!m_LoadFromAssetBundle) {
            item = AssetBundleManager.Instance.FindResourceItem(crc);
            if(item.m_Obj != null) {
                obj = item.m_Obj as T;
            } else {
                obj = LoadAssetByEditor<T>(path);
            }
        }
#endif
        if(obj == null) {
            item = AssetBundleManager.Instance.LoadResourceAssetBundle(crc);
            if(item != null && item.m_AssetBundle != null) {
                if(item.m_Obj != null) {
                    obj = item.m_Obj as T;
                } else {
                    obj = item.m_AssetBundle.LoadAsset<T>(item.m_AssetName);
                }
            } 
        }

        CacheResource(path, ref item, crc, obj);
        return obj;
    }   

    /// <summary>
    /// 不需要实例化的资源的卸载，根据对象
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResource(Object obj, bool destroyObj = false) {
        if(obj == null) {
            return false; 
        }
        ResourceItem item = null;
        foreach(ResourceItem res in AssetDic.Values) {
            if(res.m_Guid == obj.GetInstanceID()) {
                item = res;
            }
        }

        if(item == null) {
            Debug.LogError("AssetDic里不存在该资源：" + obj.name + " 可能释放了多次");
            return false;
        }

        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return true;
    }
    
    /// <summary>
    /// 不需要实例化的资源卸载，根据路径
    /// </summary>
    /// <param name="path"></param>
    /// <param name="destroyObj"></param>
    /// <returns></returns>
    public bool ReleaseResource(string path, bool destroyObj = false) {
        if(string.IsNullOrEmpty(path)) return false;
        uint crc = Crc32.GetCrc32(path);

        ResourceItem item = null;
        if(!AssetDic.TryGetValue(crc, out item) || item == null) {
            Debug.LogError("AssetDic里不存在该资源：" + path + " 可能释放了多次");
        }

        item.RefCount--;
        DestroyResourceItem(item, destroyObj);
        return true;
    }
    /// <summary>
    /// 缓存太多，清除最早没有使用的资源
    /// </summary>
    protected void WashOut() {
        // 当前内存使用大于80%的时候，清除最早没用的资源
        // {
        //     if(m_NoReferenceAssetMapList.Size() <= 0) {
        //         break;
        //     }

        //     ResourceItem item = m_NoReferenceAssetMapList.Back();
        //     DestroyResourceItem(item, true);
        //     m_NoReferenceAssetMapList.Pop();
        // }
    }

    /// <summary>
    /// 回收资源
    /// </summary>
    /// <param name="item"></param>
    /// <param name="destroy"></param>
    protected void DestroyResourceItem(ResourceItem item, bool destroyCache = false) {
        if(item == null || item.RefCount > 0) return;
        if(!destroyCache) {
            // m_NoReferenceAssetMapList.InsertToHead(item);
            return;
        }

        if(!AssetDic.Remove(item.m_Crc)) {
            return;
        }

        // 释放AssetBundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        if(item.m_Obj != null) {
#if UNITY_EDITOR
            Resources.UnloadAsset(item.m_Obj);
#endif
            item.m_Obj = null;
        }

    }

#if UNITY_EDITOR
    protected T LoadAssetByEditor<T>(string path) where T: UnityEngine.Object {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
    }
#endif
    /// <summary>
    /// 缓存加载的资源
    /// </summary>
    /// <param name="path"></param>
    /// <param name="item"></param>
    /// <param name="crc"></param>
    /// <param name="obj"></param>
    /// <param name="addRefCount"></param>
    void CacheResource(string path, ref ResourceItem item, uint crc, Object obj, int addRefCount = 1) {
        // 缓存太多，清除最早没有使用的资源
        WashOut();

        if(item == null) {
            Debug.LogError("ResourceItem is null, path: " + path);
        }

        if(obj == null) {
            Debug.LogError("ResourceLoad Fail: " + path);
        }

        item.m_Obj = obj;
        item.m_Guid = obj.GetInstanceID();
        item.m_LastUseTime = Time.realtimeSinceStartup;
        item.RefCount += addRefCount;

        if(AssetDic.TryGetValue(item.m_Crc, out ResourceItem oldItem)) {
            AssetDic[item.m_Crc] = item;
        } else {
            AssetDic.Add(item.m_Crc, item);
        }
    }


    ResourceItem GetCacheResourceItem(uint crc, int addRefCount = 1) {
        ResourceItem item = null;
        if(AssetDic.TryGetValue(crc, out item)) {
            if(item != null) {
                item.RefCount += addRefCount;
                item.m_LastUseTime = Time.realtimeSinceStartup; 
                // if(item.RefCount <= 1) {
                //     m_NoReferenceAssetMapList.Remove(item);
                // }
            }
        }
        return item;
    }

    /// <summary>
    /// 异步加载资源，仅仅是不需要实例化的资源，比如音频、图片等
    /// </summary>
    public void AsyncLoadResource(string path, OnAsyncObjFinish dealFinish, LoadResPriority priority, object param1 = null, object param2 = null, object param3 = null, uint crc = 0) {
        if(crc == 0) {
            crc = Crc32.GetCrc32(path);
        }
        ResourceItem item = GetCacheResourceItem(crc);
        if(item != null) {
            if(dealFinish != null) {
                dealFinish(path, item.m_Obj, param1, param2, param3);
            }
            return;
        }

        if(!m_LoadingAssetDic.TryGetValue(crc, out AsyncLoadResParam para) || para == null) {
            para = m_AsyncLoadResParamPool.Spawn(true);
            para.m_Crc = crc;
            para.m_Path = path;
            para.m_Priority = priority;
            m_LoadingAssetDic.Add(crc, para);
            m_LoadingAssetList[(int)priority].Add(para);
        }
        // 往回调列表里面加回调
        AsyncCallBack callBack = m_AsyncCallBackPool.Spawn(true);
        callBack.m_DealFinish = dealFinish;
        callBack.m_Param1 = param1;
        callBack.m_Param2 = param2;
        callBack.m_Param3 = param3;
        para.m_CallBackList.Add(callBack);
    }

    /// <summary>
    /// 异步加载
    /// </summary>
    /// <returns></returns>
    IEnumerator<object> AsyncLoadCor() {
        List<AsyncCallBack> callBackList = null;
        long lastYieldTime = System.DateTime.Now.Ticks;
        while(true) {
            bool haveYield = false;
            for(int i = 0; i < (int)LoadResPriority.RES_NUM; i++) {
                List<AsyncLoadResParam> loadingList = m_LoadingAssetList[i];
                if(loadingList.Count <= 0) {
                    continue;
                }
                AsyncLoadResParam loadingItem = loadingList[0];
                loadingList.RemoveAt(0);
                callBackList = loadingItem.m_CallBackList;

                Object obj = null;
                ResourceItem item = null;
#if UNITY_EDITOR
                if(!m_LoadFromAssetBundle) {
                    obj = LoadAssetByEditor<Object>(loadingItem.m_Path);
                    // 模拟异步加载
                    yield return new WaitForSeconds(0.5f);
                    item = AssetBundleManager.Instance.FindResourceItem(loadingItem.m_Crc);
                }
#endif
                if(obj == null) {
                    item = AssetBundleManager.Instance.LoadResourceAssetBundle(loadingItem.m_Crc);
                    if(item != null && item.m_AssetBundle != null) {
                        AssetBundleRequest abRequest = null;
                        if(loadingItem.m_Sprite) {
                            abRequest = item.m_AssetBundle.LoadAssetAsync<Sprite>(item.m_AssetName);
                        } else {
                            abRequest =item.m_AssetBundle.LoadAssetAsync(item.m_AssetName);
                        }
                        yield return abRequest;
                        if(abRequest.isDone) {
                            obj = abRequest.asset;
                        }

                        lastYieldTime = System.DateTime.Now.Ticks;
                    }
                }
                CacheResource(loadingItem.m_Path, ref item, loadingItem.m_Crc, obj, callBackList.Count);
                for(int j = 0; j < callBackList.Count; j++) {
                    AsyncCallBack callBack = callBackList[j];
                    if(callBack != null && callBack.m_DealFinish != null) {
                        callBack.m_DealFinish(loadingItem.m_Path, obj, callBack.m_Param1, callBack.m_Param2, callBack.m_Param3);
                        callBack.m_DealFinish = null;
                    }

                    callBack.Reset();
                    m_AsyncCallBackPool.Recycle(callBack);
                }

                obj = null;
                callBackList.Clear();
                m_LoadingAssetDic.Remove(loadingItem.m_Crc);

                loadingItem.Reset();
                m_AsyncLoadResParamPool.Recycle(loadingItem);

                if(System.DateTime.Now.Ticks - lastYieldTime > MAXLOADRESTIME) {
                    yield return null;
                    lastYieldTime = System.DateTime.Now.Ticks;
                    haveYield = true;
                }
            }

            if(!haveYield || System.DateTime.Now.Ticks - lastYieldTime > MAXLOADRESTIME) {
                lastYieldTime = System.DateTime.Now.Ticks;
                yield return null;
            }
        }
    }
}

// 双向链表结构结点
class DoubleLinkListNode<T> where T: class, new() {
    // 前一个结点
    public DoubleLinkListNode<T> prev = null;
    // 后一个结点
    public DoubleLinkListNode<T> next = null;
    // 当前结点
    public T t = null;
}

// 双向链表结构
class DoubleLinkList<T> where T: class, new() {
    public DoubleLinkListNode<T> Head = null;
    public DoubleLinkListNode<T> Tail = null;
    public ClassObjectPool<DoubleLinkListNode<T>> m_DoubleLinkNodePool = ObjectManager.Instance.GetOrCreateClassPool<DoubleLinkListNode<T>>(500);
    protected int m_Count = 0;
    public int Count {
        get {
            return m_Count;
        }
    }
    /// <summary>
    /// 添加一个结点到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToHeader(T t) {
        DoubleLinkListNode<T> pNode = m_DoubleLinkNodePool.Spawn(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return AddToHeader(pNode);
    }
    /// <summary>
    /// 添加一个结点到头部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToHeader(DoubleLinkListNode<T> pNode) {
        if(null == pNode) {
            return null;
        }
        pNode.prev = null;
        if(Head == null) {
            Head = Tail = pNode;
        } else {
            pNode.next = Head;
            Head.prev = pNode;
            Head = pNode;
        }
        m_Count++;
        return Head;
    }

    /// <summary>
    /// 添加结点到尾部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToTail(T t) {
        DoubleLinkListNode<T> pNode = m_DoubleLinkNodePool.Spawn(true);
        pNode.next = null;
        pNode.prev = null;
        pNode.t = t;
        return AddToTail(pNode);
    }

    /// <summary>
    /// 添加结点到尾部
    /// </summary>
    /// <param name="pNode"></param>
    /// <returns></returns>
    public DoubleLinkListNode<T> AddToTail(DoubleLinkListNode<T> pNode) {
        if(null == pNode) return null;
        pNode.next = null;
        if(Tail == null) {
            Head = Tail = pNode;
        } else {
            pNode.prev = Tail;
            Tail.next = pNode;
            Tail = pNode;
        }
        m_Count++;
        return Tail;
    }
    /// <summary>
    /// 移除结点
    /// </summary>
    /// <param name="pNode"></param>
    public void RemoveNode(DoubleLinkListNode<T> pNode) {
        if(null == pNode) return;
        if(pNode == Head) {
            Head = pNode.next;
        }
        if(pNode == Tail) {
            Tail = pNode.prev;
        }

        if(pNode.prev != null) {
            pNode.prev.next = pNode.next;
        }

        if(pNode.next != null) {
            pNode.next.prev = pNode.prev;
        }

        pNode.next = pNode.prev = null;
        pNode.t = null;
        m_DoubleLinkNodePool.Recycle(pNode);
        m_Count--;
    }

    /// <summary>
    /// 把某个结点移动到头部
    /// </summary>
    /// <param name="pNode"></param>
    public void MoveToHead(DoubleLinkListNode<T> pNode) {
        if(null == pNode || pNode == Head) {
            return;
        }
        if(null == pNode.prev && null == pNode.next) return;
        if(pNode == Tail) {
            Tail = pNode.prev;
        }
        if(null != pNode.prev) {
             pNode.prev.next = pNode.next;
        }

        if(pNode.next != null) {
            pNode.next.prev = pNode.prev;
        }

        pNode.prev = null;
        pNode.next = Head;
        Head.prev = pNode;
        Head = pNode;

        if(Tail == null) {
            Tail = Head;
        }
    }
}

class CMapList<T> where T: class, new() {
    DoubleLinkList<T> m_DLink = new DoubleLinkList<T>();
    Dictionary<T, DoubleLinkListNode<T>> m_FindMap = new Dictionary<T, DoubleLinkListNode<T>>();

    ~CMapList() {
        Clear();
    }
    /// <summary>
    /// 清空列表
    /// </summary>
    public void Clear() {
        while(m_DLink.Tail != null) {
            Remove(m_DLink.Tail.t);
        }
        this.m_FindMap.Clear();
    }
    /// <summary>
    /// 插入一个结点到表头 
    /// </summary>
    /// <param name="t"></param>
    public void InsertToHead(T t) {
        if(m_FindMap.TryGetValue(t, out DoubleLinkListNode<T> node) && node != null) {
            m_DLink.AddToHeader(node);
            return;
        }
        m_DLink.AddToHeader(t);
        m_FindMap.Add(t, m_DLink.Head);
    }

    /// <summary>
    /// 从表弹出一个结点
    /// </summary>
    public void Pop() {
        if(m_DLink.Tail != null) {
            Remove(m_DLink.Tail.t);
        }
    }
    /// <summary>
    /// 删除某个结点
    /// </summary>
    /// <param name="t"></param>
    public void Remove(T t) {
        if(!m_FindMap.TryGetValue(t, out DoubleLinkListNode<T> node) || node == null) {
            return;
        }
        m_DLink.RemoveNode(node);
        m_FindMap.Remove(t);
    }

    /// <summary>
    /// 获取尾部结点
    /// </summary>
    /// <returns></returns>
    public T Back() {
        return m_DLink.Tail == null ? null : m_DLink.Tail.t;
    }

    /// <summary>
    /// 返回结点个数
    /// </summary>
    /// <returns></returns>
    public int Size() {
        return m_FindMap.Count;
    }

    /// <summary>
    /// 查找是否存在该结点
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Find(T t) {
        if(!this.m_FindMap.TryGetValue(t, out DoubleLinkListNode<T> node) || node == null) {
            return false;
        }
        return true;
    }
    /// <summary>
    /// 刷新某个结点，把结点移动到头部
    /// </summary>
    /// <param name="t"></param>
    /// <returns></returns>
    public bool Refresh(T t) {
        if(!this.m_FindMap.TryGetValue(t, out DoubleLinkListNode<T> node) || node == null) {
            return false;
        }
        m_DLink.MoveToHead(node);
        return true;
    }
}