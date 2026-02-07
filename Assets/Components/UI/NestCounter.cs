using UnityEngine;
using UnityEngine.UI;
using Antymology.Terrain;

public class NestCounter : MonoBehaviour
{
    private Text counterText;
    private WorldManager worldManager;
    
    void Start()
    {
        worldManager = WorldManager.Instance;
        
        // Create UI
        GameObject canvas = new GameObject("Canvas");
        Canvas c = canvas.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<CanvasScaler>();
        canvas.AddComponent<GraphicRaycaster>();
        
        GameObject textObj = new GameObject("NestCounterText");
        textObj.transform.SetParent(canvas.transform);
        counterText = textObj.AddComponent<Text>();
        counterText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        counterText.fontSize = 24;
        counterText.color = Color.white;
        counterText.alignment = TextAnchor.UpperLeft;
        
        RectTransform rt = counterText.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(10, -10);
        rt.sizeDelta = new Vector2(300, 50);
    }
    
    void Update()
    {
        counterText.text = "Nest Blocks: " + worldManager.nestCount;
    }

}