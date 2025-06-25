using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class UnifiedSpeechUI : SerializedMonoBehaviour
{
    [Title("🎨 Unified Speech UI Generator")]
    [InfoBox("This script automatically creates a complete UI for the UnifiedSpeechManager with both STT and TTS controls.")]
    
    [SerializeField, Required]
    [Tooltip("The UnifiedSpeechManager to connect the UI to")]
    private UnifiedSpeechManager speechManager;
    
    [TitleGroup("UI Generation")]
    [Button(ButtonSizes.Gigantic, ButtonStyle.Box)]
    [GUIColor(0.2f, 0.8f, 0.2f)]
    public void CreateCompleteUI()
    {
        if (speechManager == null)
        {
            Debug.LogError("Please assign the UnifiedSpeechManager first!");
            return;
        }
        
        CreateUI();
        ConnectToSpeechManager();
        Debug.Log("✅ Complete Speech UI created and connected successfully!");
    }
    
    [TitleGroup("UI Generation")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.2f, 0.2f)]
    public void ClearUI()
    {
        GameObject existingUI = GameObject.Find("Speech_UI_Canvas");
        if (existingUI != null)
        {
            if (Application.isPlaying)
                Destroy(existingUI);
            else
                DestroyImmediate(existingUI);
            Debug.Log("UI Cleared");
        }
    }
    
    [TitleGroup("UI Preview")]
    [InfoBox("The generated UI will include:\n" +
             "• 🎤 Recording controls with volume indicator\n" +
             "• 🔊 Text-to-Speech input and synthesis\n" +
             "• 📝 Recognition results display\n" +
             "• 🎛️ Microphone and voice selection\n" +
             "• 💬 Conversation mode toggle\n" +
             "• 📊 Real-time status indicators")]
    [ShowInInspector, ReadOnly]
    public string uiPreview = "Click 'Create Complete UI' to generate the interface";
    
    private void CreateUI()
    {
        ClearUI();
        
        // Create main canvas
        GameObject canvas = CreateCanvas();
        
        // Create main panel
        GameObject mainPanel = CreateMainPanel(canvas);
        
        // Create header
        CreateHeader(mainPanel);
        
        // Create STT section
        CreateSTTSection(mainPanel);
        
        // Create TTS section
        CreateTTSSection(mainPanel);
        
        // Create conversation section
        CreateConversationSection(mainPanel);
        
        // Create status section
        CreateStatusSection(mainPanel);
    }
    
    private GameObject CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Speech_UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        return canvasObj;
    }
    
    private GameObject CreateMainPanel(GameObject canvas)
    {
        GameObject panelObj = new GameObject("MainPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 600);
        
        Image image = panelObj.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Add subtle border
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.6f, 1f, 0.8f);
        outline.effectDistance = new Vector2(2, 2);
        
        return panelObj;
    }
    
    private void CreateHeader(GameObject parent)
    {
        GameObject headerObj = CreateTextDisplay("HeaderText", "🎤🔊 Unified Speech Manager", parent, new Vector2(0, 250), new Vector2(600, 50));
        TextMeshProUGUI headerText = headerObj.GetComponent<TextMeshProUGUI>();
        headerText.fontSize = 24;
        headerText.fontStyle = FontStyles.Bold;
        headerText.color = new Color(0.3f, 0.8f, 1f);
    }
    
    private void CreateSTTSection(GameObject parent)
    {
        // STT Header
        CreateTextDisplay("STTHeader", "🎤 Speech-to-Text", parent, new Vector2(-200, 180), new Vector2(200, 30));
        
        // Microphone dropdown
        CreateDropdown("MicrophoneDropdown", "Select Microphone", parent, new Vector2(-200, 140), new Vector2(180, 30));
        
        // Record button
        CreateButton("RecordButton", "🎤 Start Recording", parent, new Vector2(-200, 100), new Vector2(180, 40), new Color(0.2f, 0.8f, 0.2f));
        
        // Play button
        CreateButton("PlayButton", "▶️ Play", parent, new Vector2(-200, 55), new Vector2(85, 35), new Color(0.2f, 0.6f, 0.8f));
        
        // Recognize button
        CreateButton("RecognizeButton", "🧠 Recognize", parent, new Vector2(-105, 55), new Vector2(85, 35), new Color(0.8f, 0.6f, 0.2f));
        
        // Volume indicator
        CreateSlider("VolumeIndicator", parent, new Vector2(-200, 15), new Vector2(180, 20));
        
        // Recognition result display
        CreateScrollableTextDisplay("RecognitionDisplay", "Recognition results will appear here...", parent, new Vector2(-200, -40), new Vector2(180, 80));
    }
    
    private void CreateTTSSection(GameObject parent)
    {
        // TTS Header
        CreateTextDisplay("TTSHeader", "🔊 Text-to-Speech", parent, new Vector2(200, 180), new Vector2(200, 30));
        
        // Voice dropdown
        CreateDropdown("VoiceDropdown", "Select Voice", parent, new Vector2(200, 140), new Vector2(180, 30));
        
        // Text input
        CreateInputField("TextInput", "Enter text to synthesize...", parent, new Vector2(200, 80), new Vector2(180, 80));
        
        // Synthesize button
        CreateButton("SynthesizeButton", "🔊 Speak Text", parent, new Vector2(200, 20), new Vector2(180, 40), new Color(0.6f, 0.2f, 0.8f));
    }
    
    private void CreateConversationSection(GameObject parent)
    {
        // Conversation button (spans both sections)
        CreateButton("ConversationButton", "💬 Start Conversation Mode", parent, new Vector2(0, -110), new Vector2(300, 50), new Color(0.8f, 0.2f, 0.6f));
    }
    
    private void CreateStatusSection(GameObject parent)
    {
        // Status display (spans full width)
        CreateScrollableTextDisplay("StatusDisplay", "Status information will appear here...", parent, new Vector2(0, -190), new Vector2(750, 120));
    }
    
    private GameObject CreateTextDisplay(string name, string text, GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;
        
        return textObj;
    }
    
    private GameObject CreateScrollableTextDisplay(string name, string text, GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject scrollObj = new GameObject(name + "_Scroll");
        scrollObj.transform.SetParent(parent.transform, false);
        
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = position;
        scrollRect.sizeDelta = size;
        
        // Background
        Image scrollBg = scrollObj.AddComponent<Image>();
        scrollBg.color = new Color(0.05f, 0.05f, 0.05f, 0.8f);
        
        // Scroll view
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        
        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollObj.transform, false);
        
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        
        viewport.AddComponent<Mask>();
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = Color.clear;
        
        // Content
        GameObject content = new GameObject(name);
        content.transform.SetParent(viewport.transform, false);
        
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(5, 0);
        contentRect.offsetMax = new Vector2(-5, 0);
        
        TextMeshProUGUI textComponent = content.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 12;
        textComponent.color = new Color(0.9f, 0.9f, 0.9f);
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.overflowMode = TextOverflowModes.Overflow;
        
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        
        return content;
    }
    
    private GameObject CreateButton(string name, string text, GameObject parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        
        // Button effects
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        colors.disabledColor = color * 0.5f;
        button.colors = colors;
        
        // Create button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.fontStyle = FontStyles.Bold;
        
        return buttonObj;
    }
    
    private GameObject CreateInputField(string name, string placeholder, GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = inputObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
        
        // Text area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(5, 5);
        textAreaRect.offsetMax = new Vector2(-5, -5);
        
        textArea.AddComponent<RectMask2D>();
        
        // Text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = "";
        textComponent.fontSize = 12;
        textComponent.color = Color.white;
        
        // Placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(textArea.transform, false);
        
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;
        
        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 12;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        placeholderText.fontStyle = FontStyles.Italic;
        
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        
        return inputObj;
    }
    
    private GameObject CreateDropdown(string name, string label, GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = dropdownObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Image image = dropdownObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        
        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(10, 6);
        labelRect.offsetMax = new Vector2(-25, -7);
        
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 12;
        labelText.color = Color.white;
        
        // Arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(20, 20);
        arrowRect.anchoredPosition = new Vector2(-15, 0);
        
        Image arrowImage = arrowObj.AddComponent<Image>();
        arrowImage.color = Color.white;
        // You might want to assign a dropdown arrow sprite here
        
        dropdown.captionText = labelText;
        dropdown.targetGraphic = image;
        
        return dropdownObj;
    }
    
    private GameObject CreateSlider(string name, GameObject parent, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent.transform, false);
        
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;
        
        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.25f);
        bgRect.anchorMax = new Vector2(1f, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        
        RectTransform fillRect = fillArea.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0.25f);
        fillRect.anchorMax = new Vector2(1f, 0.75f);
        fillRect.sizeDelta = Vector2.zero;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        
        RectTransform fillImageRect = fill.AddComponent<RectTransform>();
        fillImageRect.sizeDelta = Vector2.zero;
        fillImageRect.offsetMin = Vector2.zero;
        fillImageRect.offsetMax = Vector2.zero;
        
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
        
        slider.fillRect = fillImageRect;
        
        return sliderObj;
    }
    
    private void ConnectToSpeechManager()
    {
        GameObject canvas = GameObject.Find("Speech_UI_Canvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas not found!");
            return;
        }
        
        // Find and assign all UI components
        speechManager.statusDisplay = canvas.transform.Find("MainPanel/StatusDisplay")?.GetComponent<TextMeshProUGUI>();
        speechManager.recognitionDisplay = canvas.transform.Find("MainPanel/RecognitionDisplay")?.GetComponent<TextMeshProUGUI>();
        speechManager.textInput = canvas.transform.Find("MainPanel/TextInput")?.GetComponent<TMP_InputField>();
        speechManager.recordButton = canvas.transform.Find("MainPanel/RecordButton")?.GetComponent<Button>();
        speechManager.playButton = canvas.transform.Find("MainPanel/PlayButton")?.GetComponent<Button>();
        speechManager.recognizeButton = canvas.transform.Find("MainPanel/RecognizeButton")?.GetComponent<Button>();
        speechManager.synthesizeButton = canvas.transform.Find("MainPanel/SynthesizeButton")?.GetComponent<Button>();
        speechManager.conversationButton = canvas.transform.Find("MainPanel/ConversationButton")?.GetComponent<Button>();
        speechManager.microphoneDropdown = canvas.transform.Find("MainPanel/MicrophoneDropdown")?.GetComponent<TMP_Dropdown>();
        speechManager.voiceDropdown = canvas.transform.Find("MainPanel/VoiceDropdown")?.GetComponent<TMP_Dropdown>();
        speechManager.volumeIndicator = canvas.transform.Find("MainPanel/VolumeIndicator")?.GetComponent<Slider>();
        
        // Add AudioSource if needed
        if (speechManager.audioSource == null)
        {
            speechManager.audioSource = speechManager.gameObject.GetComponent<AudioSource>();
            if (speechManager.audioSource == null)
            {
                speechManager.audioSource = speechManager.gameObject.AddComponent<AudioSource>();
            }
        }
        
        Debug.Log("🎯 All UI components connected to UnifiedSpeechManager!");
        uiPreview = "✅ UI Generated and Connected Successfully!";
    }
} 