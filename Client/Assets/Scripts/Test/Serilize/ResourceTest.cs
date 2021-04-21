using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine;
public class ResourceTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        TestLoadAB();
    }

    void TestLoadAB() {
        // ReadTestAssets(); 
        // TextAsset textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(Application.streamingAssetsPath + "/AssetBundleConfig.bytes");
        AssetBundle configAB = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/assetbundleconfig");
        TextAsset textAsset = configAB.LoadAsset<TextAsset>("AssetBundleConfig");
        MemoryStream stream = new MemoryStream(textAsset.bytes);
        BinaryFormatter bf = new BinaryFormatter();
        AssetBundleConfig config = (AssetBundleConfig) bf.Deserialize(stream);
        stream.Close();

        string path = "Assets/Res/Prefabs/Sohpie.prefab";

        uint crc = Crc32.GetCrc32(path);
        ABBase abBase = null;
        for(int i = 0; i < config.ABList.Count; i++) {
            if(config.ABList[i].Crc == crc) {
                abBase = config.ABList[i];
            }
        }

        for(int i = 0; i < abBase.ABDependency.Count; i++) {
            AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABDependency[i]);
        }
        AssetBundle assetBundle = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + abBase.ABName);
        GameObject prefab = assetBundle.LoadAsset<GameObject>(abBase.AssetName);

        GameObject obj = GameObject.Instantiate(prefab); 

    }

    void ReadTestAssets() {
        // AssetSerilize assets = AssetDatabase.LoadAssetAtPath<AssetSerilize>("Assets/Scripts/Test/TestAssets.asset");
        // Debug.Log(assets.Id);
        // Debug.Log(assets.Name);
        // foreach(string str in assets.TestList) {
        //     Debug.Log(str);
        // }
    }
}
