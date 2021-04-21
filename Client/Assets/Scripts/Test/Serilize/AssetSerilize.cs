using UnityEngine;
using System.Collections.Generic;
 
// [CreateAssetMenu(fileName = "TestAssets", menuName = "CreateAssets", order = 0)]
class AssetSerilize : ScriptableObject{
    public int Id;
    public string Name;
    public List<string> TestList;  
}