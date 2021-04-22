using System.Collections.Generic;
using UnityEngine;
class ResourceManager: Singleton<ResourceManager> {
    public bool m_LoadFromAssetBundle = true;
    // 缓存使用的资源列表
    protected Dictionary<uint, ResourceItem> AssetDic {get; set;} = new Dictionary<uint, ResourceItem>();
    // 缓存引用计数为0的列表(不是实例化的资源，而是在内存中的，eq:图片，材质，未实例化的prefab)，达到缓存最大的时候，释放这个列表里最早没用的资源
    protected CMapList<ResourceItem> m_NoReferenceAssetMapList = new CMapList<ResourceItem>();

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
    /// 不需要实例化的资源的卸载
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
            m_NoReferenceAssetMapList.InsertToHead(item);
            return;
        }

        if(!AssetDic.Remove(item.m_Crc)) {
            return;
        }

        // 释放AssetBundle引用
        AssetBundleManager.Instance.ReleaseAsset(item);

        if(item.m_Obj != null) {
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