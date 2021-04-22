using System.Collections.Generic;
using System;
class ObjectManager: Singleton<ObjectManager> {
    #region 类对象池的使用
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();
    /// <summary>
    /// 创建类对象池，外面可以保存ClassObjectPool,然后可以调用Spawn创建和回收对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public ClassObjectPool<T> GetOrCreateClassPool<T>(int maxCount) where T: class, new() {
        Type type = typeof(T);
        if(!m_ClassPoolDic.TryGetValue(type, out object outObj) || null == outObj) {
            ClassObjectPool<T> newPool = new ClassObjectPool<T>(maxCount);
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }
        return outObj as ClassObjectPool<T>;
    }
    #endregion
}