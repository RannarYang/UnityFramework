/*
 * @Author       : RannarYang
 * @Date         : 2021-04-21 14:55:05
 * @LastEditors  : RannarYang
 * @LastEditTime : 2021-04-21 15:08:26
 * @FilePath     : \Client\Assets\Editor\ABConfig.cs
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ABConfig", menuName = "CreateABConfig", order = 0)]
public class ABConfig : ScriptableObject {
    //单个文件所在文件夹路径，会遍历这个文件夹下面所有prefab，所有的prefab的名字不能重复，必须保证名字的唯一性
    public List<string> m_AllPrefabPath = new List<string>();
    public List<FileDirABName> m_AllFileDirAB = new List<FileDirABName>();

    [System.Serializable]
    public struct FileDirABName {
        public string ABName;
        public string Path;
    }
}
