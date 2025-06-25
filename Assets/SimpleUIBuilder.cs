using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;

public class SimpleUIBuilder : SerializedMonoBehaviour
{
    [Title("Simple UI Builder for Speech Testing")]
    [InfoBox("This script creates UI elements for testing the HelloWorld speech script.")]
    
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void BuildTestUI()
    {
        CreateUI();
    }
    
    [Button(ButtonSizes.Medium, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void ClearUI()
    {
        GameObject existingUI = GameObject.Find("TestUI_Canvas");
        if (existingUI != null)
        {
            if (Application.isPlaying)
                Destroy(existingUI);
            else
                DestroyImmediate(existingUI);
        }
    }
    
    private void CreateUI()
    {
        // Clear existing UI
        ClearUI();
        
        // Create Canvas
        GameObject canvasObj = new GameObject("TestUI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Create UI Panel
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        // Create Status Text
        GameObject statusTextObj = CreateText("StatusText", "Status will appear here...", panelObj.transform);
        PositionElement(statusTextObj, new Vector2(0, 200), new Vector2(700, 100));
        
        // Create Copy Files Button
        GameObject copyBtn = CreateButton("CopyFilesButton", "Copy Files", panelObj.transform);
        PositionElement(copyBtn, new Vector2(-200, 100), new Vector2(150, 40));
        
        // Create Init Objects Button
        GameObject initBtn = CreateButton("InitObjectsButton", "Init Objects", panelObj.transform);
        PositionElement(initBtn, new Vector2(0, 100), new Vector2(150, 40));
        SetButtonColor(initBtn, new Color(0.4f, 0.8f, 0.4f, 1f));
        
        // Create Input Field for synthesis
        GameObject inputObj = CreateInputField("SynthesisInput", "Enter text to synthesize...", panelObj.transform);
        PositionElement(inputObj, new Vector2(0, 0), new Vector2(400, 30));
        
        // Create Recognize Button  
        GameObject recognizeBtn = CreateButton("RecognizeButton", "🎤 Recognize Speech", panelObj.transform);
        PositionElement(recognizeBtn, new Vector2(-100, -50), new Vector2(180, 40));
        SetButtonColor(recognizeBtn, new Color(0.4f, 0.4f, 0.8f, 1f));
        
        // Create Synthesize Button
        GameObject synthesizeBtn = CreateButton("SynthesizeButton", "🔊 Synthesize Speech", panelObj.transform);
        PositionElement(synthesizeBtn, new Vector2(100, -50), new Vector2(180, 40));
        SetButtonColor(synthesizeBtn, new Color(0.8f, 0.6f, 0.2f, 1f));
        
        // Auto-assign to HelloWorld script if it exists
        HelloWorld helloWorld = FindObjectOfType<HelloWorld>();
        if (helloWorld != null)
        {
            helloWorld.statusOutput = statusTextObj.GetComponent<Text>();
            helloWorld.copyFilesButton = copyBtn.GetComponent<Button>();
            helloWorld.initObjectsButton = initBtn.GetComponent<Button>();
            helloWorld.recognizeButton = recognizeBtn.GetComponent<Button>();
            helloWorld.synthesisInput = inputObj.GetComponent<InputField>();
            helloWorld.synthesizeButton = synthesizeBtn.GetComponent<Button>();
            
            Debug.Log("✅ UI elements created and assigned to HelloWorld script!");
        }
        else
        {
            Debug.LogWarning("⚠️ HelloWorld script not found. UI created but not assigned.");
        }
    }
    
    private GameObject CreateText(string name, string text, Transform parent)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;
        
        return textObj;
    }
    
    private GameObject CreateButton(string name, string text, Transform parent)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        
        // Create text child
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return buttonObj;
    }
    
    private GameObject CreateInputField(string name, string placeholder, Transform parent)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        
        Image inputImage = inputObj.AddComponent<Image>();
        inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        
        InputField inputField = inputObj.AddComponent<InputField>();
        
        // Create text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputObj.transform, false);
        
        Text inputText = textObj.AddComponent<Text>();
        inputText.text = "";
        inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inputText.fontSize = 14;
        inputText.color = Color.white;
        inputText.supportRichText = false;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        
        // Create placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputObj.transform, false);
        
        Text placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.fontStyle = FontStyle.Italic;
        
        RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);
        
        inputField.textComponent = inputText;
        inputField.placeholder = placeholderText;
        
        return inputObj;
    }
    
    private void PositionElement(GameObject element, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rectTransform = element.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = sizeDelta;
    }
    
    private void SetButtonColor(GameObject button, Color color)
    {
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = color;
        }
    }
} 