/*
 * @Author       : RannarYang
 * @Date         : 2021-04-26 07:31:33
 * @LastEditors  : RannarYang
 * @LastEditTime : 2021-04-26 14:55:20
 * @FilePath     : \Client\Assets\Base\Pool\PoolManager.cs
 */
using System.Collections.Generic;
using System;
public class PoolManager: Singleton<PoolManager> {
    #region 类对象池的使用
    protected Dictionary<Type, object> m_ClassPoolDic = new Dictionary<Type, object>();

    /// <summary>
    /// 创建类对象池，创建完成以后外面可以保存ClassObjectPool<T>,然后调用Spawn和Recycle来创建和回收类对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="maxcount"></param>
    /// <returns></returns>
    public Pool<T> GetOrCreatClassPool<T>(int maxcount) where T : class, new()
    {
        Type type = typeof(T);
        object outObj = null;
        if (!m_ClassPoolDic.TryGetValue(type, out outObj) || outObj == null)
        {
            Pool<T> newPool = new Pool<T>(maxcount);
            m_ClassPoolDic.Add(type, newPool);
            return newPool;
        }

        return outObj as Pool<T>;
    }
    #endregion
}