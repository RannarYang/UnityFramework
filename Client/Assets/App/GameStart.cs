/*
 * @Author       : RannarYang
 * @Date         : 2021-04-26 14:40:59
 * @LastEditors  : RannarYang
 * @LastEditTime : 2021-04-27 11:30:35
 * @FilePath     : \Client\Assets\App\GameStart.cs
 */
using UnityEngine;
public class GameStart : Base
{
    //加载配置表
    protected override void LoadConfiger() {
        
    }

    protected override void OnAwake() {
        Sprite sp = ResourceManager.Instance.LoadResource<Sprite>("Assets/App/Res/Sprites/Test.png");
        ResourceManager.Instance.ReleaseResouce(sp);

        ObjectManager.Instance.PreloadGameObject("Assets/App/Res/Prefabs/Sphere.prefab", 5);

        ObjectManager.Instance.InstantiateObject("Assets/App/Res/Prefabs/Sphere.prefab");
        CheckAndAddMemoryDisplay();
    } 

    private void CheckAndAddMemoryDisplay() {
#if UNITY_EDITOR
        this.gameObject.AddComponent<MemoryDisplay>();
#endif
    }

    protected override void OnStart() {
        
    }
}