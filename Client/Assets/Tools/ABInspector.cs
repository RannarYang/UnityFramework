using UnityEngine;
class ABInspector: MonoBehaviour {
    public string targetABName = null;
    public AssetBundle target = null;

    void OnGUI() {
        if(GUI.Button(new Rect(0,0,100,100), "Load Target AB")) {
            if(target != null) {
                target.Unload(true);
            }

            if(!string.IsNullOrEmpty(targetABName)) {
                target = AssetBundle.LoadFromFile(Application.streamingAssetsPath + "/" + targetABName);
            }
        }
    }
}