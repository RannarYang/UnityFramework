using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource m_Audio;
    private AudioClip clip;
    private void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
        ResourceManager.Instance.Init(this);
    }

    private void Start()
    {
        // ResourceManager.Instance.AsyncLoadResource("Assets/Res/Sounds/ding.mp3", OnLoadFinish, LoadResPriority.RES_MIDDLE);
        ResourceManager.Instance.PreloadRes("Assets/Res/Sounds/ding.mp3");
    }

    void OnLoadFinish(string path, Object obj, object param1, object param2, object param3) {
        clip = obj as AudioClip;
        m_Audio.clip = clip ;
        m_Audio.Play();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A)) {
            long time = System.DateTime.Now.Ticks;
            clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Res/Sounds/ding.mp3");
            Debug.Log("预加载的时间：" + (System.DateTime.Now.Ticks - time));
            m_Audio.clip = clip;
            m_Audio.Play();
        } else if(Input.GetKeyDown(KeyCode.B)) {
            ResourceManager.Instance.ReleaseResource(this.clip, true);
            clip = null;
            m_Audio.clip = null;
        }
    }

    // 同步加载
    private void TestLoadResourceStart() {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Res/Sounds/ding.mp3");
        m_Audio.clip = clip;
        m_Audio.Play();
    }

    private void TestLoadResourceUpdate() {
        if(Input.GetKeyDown(KeyCode.A)) {
            m_Audio.Stop();
            ResourceManager.Instance.ReleaseResource(this.clip, true);
            clip = null;
        }
    }
    
    private void OnApplicationQuit()
    {
        ResourceManager.Instance.ClearCache();
        Resources.UnloadUnusedAssets();
        Debug.Log("清空编辑器缓存");    
    }
}
