/*
 * @Author       : RannarYang
 * @Date         : 2021-04-22 16:49:53
 * @LastEditors  : RannarYang
 * @LastEditTime : 2021-04-24 11:17:41
 * @FilePath     : \Client\Assets\Scripts\GameStart.cs
 */
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private GameObject obj;
    private void Awake()
    {
        GameObject.DontDestroyOnLoad(gameObject);
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
        ObjectManager.Instance.Init(transform.Find("RecyclePoolTrs"), transform.Find("SceneTrs"));
    }

    private void Start()
    {
        ObjectManager.Instance.PreloadGameObject("Assets/Res/Prefabs/Sohpie.prefab", 20);
    }

    void OnLoadFinish(string path, Object obj, object param1 = null, object param2 = null, object param3 = null) {
        this.obj = obj as GameObject;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A)) {
            ObjectManager.Instance.ReleaseObject(obj);
            obj = null;
        } else if(Input.GetKeyDown(KeyCode.D)) {
            ObjectManager.Instance.InstantiateObjectAsync("Assets/Res/Prefabs/Sohpie.prefab", OnLoadFinish,LoadResPriority.RES_HIGHT, true);
        } else if(Input.GetKeyDown(KeyCode.S)) {
            ObjectManager.Instance.ReleaseObject(obj, 0, true);
            obj = null;
        }
    }
}
