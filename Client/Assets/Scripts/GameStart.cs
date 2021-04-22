using UnityEngine;

public class GameStart : MonoBehaviour
{
    public AudioSource m_Audio;
    private AudioClip clip;
    private void Awake()
    {
        AssetBundleManager.Instance.LoadAssetBundleConfig();
    }

    private void Start()
    {
        clip = ResourceManager.Instance.LoadResource<AudioClip>("Assets/Res/Sounds/ding.mp3");
        m_Audio.clip = clip;
        m_Audio.Play();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A)) {
            m_Audio.Stop();
            ResourceManager.Instance.ReleaseResource(this.clip, true);
            clip = null;
        }
    }
    
}
