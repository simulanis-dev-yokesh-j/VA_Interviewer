using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;

public class OnboardingUIGenerator : MonoBehaviour
{
    [Title("Onboarding UI Generator")]
    [InfoBox("Automatically generates the complete onboarding UI with proper panels and components")]
    
    // ========================================
    // GENERATION SETTINGS
    // ========================================
    
    [TitleGroup("UI Generation")]
    [LabelText("Canvas")]
    [InfoBox("The main canvas to create UI under")]
    public Canvas targetCanvas;
    
    [TitleGroup("UI Generation")]
    [LabelText("UI Scale Factor")]
    [Range(0.5f, 2f)]
    public float uiScale = 1f;
    
    [TitleGroup("UI Generation")]
    [LabelText("Panel Width")]
    [Range(400f, 1200f)]
    public float panelWidth = 800f;
    
    [TitleGroup("UI Generation")]
    [LabelText("Panel Height")]
    [Range(300f, 800f)]
    public float panelHeight = 600f;
    
    // ========================================
    // STYLE SETTINGS
    // ========================================
    
    [TitleGroup("Style Settings")]
    [LabelText("Background Color")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    
    [TitleGroup("Style Settings")]
    [LabelText("Primary Button Color")]
    public Color primaryButtonColor = new Color(0.2f, 0.6f, 0.8f, 1f);
    
    [TitleGroup("Style Settings")]
    [LabelText("Secondary Button Color")]
    public Color secondaryButtonColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    
    [TitleGroup("Style Settings")]
    [LabelText("Text Color")]
    public Color textColor = Color.white;
    
    [TitleGroup("Style Settings")]
    [LabelText("Input Field Color")]
    public Color inputFieldColor = new Color(0.2f, 0.2f, 0.25f, 1f);
    
    // ========================================
    // FONT SETTINGS
    // ========================================
    
    [TitleGroup("Font Settings")]
    [LabelText("Title Font Size")]
    [Range(18, 36)]
    public int titleFontSize = 28;
    
    [TitleGroup("Font Settings")]
    [LabelText("Dialogue Font Size")]
    [Range(14, 24)]
    public int dialogueFontSize = 18;
    
    [TitleGroup("Font Settings")]
    [LabelText("Button Font Size")]
    [Range(12, 20)]
    public int buttonFontSize = 16;
    
    // ========================================
    // GENERATED COMPONENTS
    // ========================================
    
    [TitleGroup("Generated Components")]
    [ShowInInspector, ReadOnly]
    [LabelText("Generated Onboarding Manager")]
    public OnboardingManager generatedOnboardingManager;
    
    [TitleGroup("Generated Components")]
    [ShowInInspector, ReadOnly]
    [LabelText("Main Panel")]
    public GameObject mainPanel;
    
    // ========================================
    // GENERATION METHODS
    // ========================================
    
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public void GenerateOnboardingUI()
    {
        if (targetCanvas == null)
        {
            Debug.LogError("[UI GENERATOR] Target Canvas is required for UI generation!");
            return;
        }
        
        try
        {
            Debug.Log("[UI GENERATOR] Starting UI generation...");
            
            // Clear existing UI if any
            ClearExistingUI();
            
            // Create main structure
            Debug.Log("[UI GENERATOR] Creating main panel...");
            CreateMainPanel();
            
            Debug.Log("[UI GENERATOR] Creating header section...");
            CreateHeaderSection();
            
            Debug.Log("[UI GENERATOR] Creating content section...");
            CreateContentSection();
            
            Debug.Log("[UI GENERATOR] Creating input section...");
            CreateInputSection();
            
            Debug.Log("[UI GENERATOR] Creating selection section...");
            CreateSelectionSection();
            
            Debug.Log("[UI GENERATOR] Creating button section...");
            CreateButtonSection();
            
            // Setup OnboardingManager component
            Debug.Log("[UI GENERATOR] Setting up OnboardingManager...");
            SetupOnboardingManager();
            
            Debug.Log("[UI GENERATOR] Onboarding UI generated successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[UI GENERATOR] Failed to generate UI: {e.Message}\n{e.StackTrace}");
        }
    }
    
    [Button("Clear Generated UI")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void ClearExistingUI()
    {
        if (mainPanel != null)
        {
            DestroyImmediate(mainPanel);
            mainPanel = null;
        }
        
        if (generatedOnboardingManager != null)
        {
            DestroyImmediate(generatedOnboardingManager);
            generatedOnboardingManager = null;
        }
        
        Debug.Log("[UI GENERATOR] Cleared existing onboarding UI");
    }
    
    private void CreateMainPanel()
    {
        // Create main panel GameObject
        mainPanel = new GameObject("OnboardingPanel");
        mainPanel.transform.SetParent(targetCanvas.transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = mainPanel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Add background image
        Image backgroundImage = mainPanel.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        
        // Create inner content panel
        GameObject contentPanel = new GameObject("ContentPanel");
        contentPanel.transform.SetParent(mainPanel.transform, false);
        
        RectTransform contentRect = contentPanel.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = new Vector2(panelWidth * uiScale, panelHeight * uiScale);
        contentRect.anchoredPosition = Vector2.zero;
        
        // Add content background
        Image contentBg = contentPanel.AddComponent<Image>();
        contentBg.color = new Color(0.05f, 0.05f, 0.1f, 0.8f);
        
        // Add vertical layout
        VerticalLayoutGroup layout = contentPanel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 20f;
        layout.padding = new RectOffset(40, 40, 40, 40);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        // Add content size fitter
        ContentSizeFitter fitter = contentPanel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    
        private void CreateHeaderSection()
    {
        GameObject headerSection = new GameObject("HeaderSection");
        headerSection.transform.SetParent(GetContentPanel().transform, false);
        
        // Add RectTransform to header section
        RectTransform headerRect = headerSection.AddComponent<RectTransform>();
        headerRect.anchorMin = Vector2.zero;
        headerRect.anchorMax = Vector2.one;
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;
        
        // Title text
        GameObject titleObj = new GameObject("TitleText");
        titleObj.transform.SetParent(headerSection.transform, false);
        
        // Add RectTransform first
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Then add TextMeshProUGUI component
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        if (titleText != null)
        {
            titleText.text = "AI Interview System";
            titleText.fontSize = titleFontSize;
            titleText.color = textColor;
            titleText.alignment = TextAlignmentOptions.Center;
            
            try
            {
                titleText.fontStyle = FontStyles.Bold;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[UI GENERATOR] Could not set font style for title: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[UI GENERATOR] Failed to add TMP_Text component to title object!");
        }
        
        // Set header height
        LayoutElement headerLayout = headerSection.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 60f;
    }
    
    private void CreateContentSection()
    {
        GameObject contentSection = new GameObject("ContentSection");
        contentSection.transform.SetParent(GetContentPanel().transform, false);
        
        // Dialogue text
        GameObject dialogueObj = new GameObject("DialogueText");
        dialogueObj.transform.SetParent(contentSection.transform, false);
        
        TextMeshProUGUI dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "Welcome! Let's get started with your interview preparation.";
        dialogueText.fontSize = dialogueFontSize;
        dialogueText.color = textColor;
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        dialogueText.enableWordWrapping = true;
        
        RectTransform dialogueRect = dialogueText.GetComponent<RectTransform>();
        dialogueRect.anchorMin = Vector2.zero;
        dialogueRect.anchorMax = Vector2.one;
        dialogueRect.offsetMin = Vector2.zero;
        dialogueRect.offsetMax = Vector2.zero;
        
        // Set content height
        LayoutElement contentLayout = contentSection.AddComponent<LayoutElement>();
        contentLayout.preferredHeight = 120f;
        contentLayout.flexibleHeight = 1f;
    }
    
    private void CreateInputSection()
    {
        GameObject inputSection = new GameObject("InputSection");
        inputSection.transform.SetParent(GetContentPanel().transform, false);
        
        // Input field
        GameObject inputFieldObj = new GameObject("UserInputField");
        inputFieldObj.transform.SetParent(inputSection.transform, false);
        
        // Input field background
        Image inputBg = inputFieldObj.AddComponent<Image>();
        inputBg.color = inputFieldColor;
        
        // TMP Input Field
        TMP_InputField inputField = inputFieldObj.AddComponent<TMP_InputField>();
        
        // Create placeholder
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputFieldObj.transform, false);
        
        TextMeshProUGUI placeholder = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter your name here...";
                placeholder.fontSize = dialogueFontSize - 2;
        placeholder.color = new Color(textColor.r, textColor.g, textColor.b, 0.5f);
        placeholder.fontStyle = FontStyles.Italic;
        
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 5);
        placeholderRect.offsetMax = new Vector2(-10, -5);
        
        // Create text component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputFieldObj.transform, false);
        
        TextMeshProUGUI inputText = textObj.AddComponent<TextMeshProUGUI>();
        inputText.fontSize = dialogueFontSize;
        inputText.color = textColor;
        
        RectTransform textRect = inputText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);
        
        // Setup input field
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        
        RectTransform inputRect = inputField.GetComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        
        // Set input section height
        LayoutElement inputLayout = inputSection.AddComponent<LayoutElement>();
        inputLayout.preferredHeight = 50f;
    }
    
    private void CreateSelectionSection()
    {
        GameObject selectionSection = new GameObject("SelectionPanel");
        selectionSection.transform.SetParent(GetContentPanel().transform, false);
        
        // Vertical layout for dropdowns
        VerticalLayoutGroup dropdownLayout = selectionSection.AddComponent<VerticalLayoutGroup>();
        dropdownLayout.spacing = 15f;
        dropdownLayout.childControlWidth = true;
        dropdownLayout.childControlHeight = false;
        dropdownLayout.childForceExpandWidth = true;
        
        // Create dropdowns
        CreateDropdown(selectionSection, "RoleDropdown", "Select Position");
        CreateDropdown(selectionSection, "InterviewTypeDropdown", "Select Interview Type");
        CreateDropdown(selectionSection, "DifficultyDropdown", "Select Difficulty Level");
        
        // Set selection section height
        LayoutElement selectionLayout = selectionSection.AddComponent<LayoutElement>();
        selectionLayout.preferredHeight = 180f;
        
        // Initially hide selection section
        selectionSection.SetActive(false);
    }
    
    private void CreateDropdown(GameObject parent, string name, string labelText)
    {
        GameObject dropdownObj = new GameObject(name);
        dropdownObj.transform.SetParent(parent.transform, false);
        
        // Add RectTransform to dropdown
        RectTransform dropdownRect = dropdownObj.AddComponent<RectTransform>();
        
        // Dropdown background
        Image dropdownBg = dropdownObj.AddComponent<Image>();
        dropdownBg.color = inputFieldColor;
        
        // TMP Dropdown
        TMP_Dropdown dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
        
        // Create label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(dropdownObj.transform, false);
        
        // Add RectTransform to label
        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        
        TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
        label.text = labelText;
        label.fontSize = dialogueFontSize - 2;
        label.color = textColor;
        
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(10, 5);
        labelRect.offsetMax = new Vector2(-25, -5);
        
        // Create arrow
        GameObject arrowObj = new GameObject("Arrow");
        arrowObj.transform.SetParent(dropdownObj.transform, false);
        
        // Add RectTransform to arrow
        RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
        
        TextMeshProUGUI arrow = arrowObj.AddComponent<TextMeshProUGUI>();
        arrow.text = "▼";
        arrow.fontSize = 14;
        arrow.color = textColor;
        arrow.alignment = TextAlignmentOptions.Center;
        
        arrowRect.anchorMin = new Vector2(1, 0);
        arrowRect.anchorMax = new Vector2(1, 1);
        arrowRect.offsetMin = new Vector2(-25, 0);
        arrowRect.offsetMax = new Vector2(0, 0);
        
        // Create template (for dropdown list)
        GameObject templateObj = new GameObject("Template");
        templateObj.transform.SetParent(dropdownObj.transform, false);
        
        // Add RectTransform to template
        RectTransform templateRect = templateObj.AddComponent<RectTransform>();
        
        Image templateBg = templateObj.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(1, 0);
        templateRect.offsetMin = new Vector2(0, -150);
        templateRect.offsetMax = new Vector2(0, 0);
        
        // Create viewport for template
        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(templateObj.transform, false);
        
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(5, 5);
        viewportRect.offsetMax = new Vector2(-5, -5);
        
        // Create content for viewport
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;
        contentRect.pivot = new Vector2(0.5f, 1f);
        
        // Create item template
        GameObject itemObj = new GameObject("Item");
        itemObj.transform.SetParent(contentObj.transform, false);
        
        Toggle itemToggle = itemObj.AddComponent<Toggle>();
        itemToggle.isOn = false;
        
        // Item background
        Image itemBg = itemObj.AddComponent<Image>();
        itemBg.color = new Color(0.2f, 0.2f, 0.25f, 1f);
        
        // Item text
        GameObject itemTextObj = new GameObject("Item Label");
        itemTextObj.transform.SetParent(itemObj.transform, false);
        
        TextMeshProUGUI itemText = itemTextObj.AddComponent<TextMeshProUGUI>();
        itemText.text = "Option";
        itemText.fontSize = dialogueFontSize - 4;
        itemText.color = textColor;
        
        RectTransform itemTextRect = itemTextObj.AddComponent<RectTransform>();
        itemTextRect.anchorMin = Vector2.zero;
        itemTextRect.anchorMax = Vector2.one;
        itemTextRect.offsetMin = new Vector2(10, 2);
        itemTextRect.offsetMax = new Vector2(-10, -2);
        
        RectTransform itemRect = itemObj.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 0.5f);
        itemRect.anchorMax = new Vector2(1, 0.5f);
        itemRect.offsetMin = Vector2.zero;
        itemRect.offsetMax = Vector2.zero;
        itemRect.sizeDelta = new Vector2(0, 30);
        
        // Setup dropdown
        dropdown.captionText = label;
        dropdown.itemText = itemText;
        dropdown.template = templateRect;
        
        // Set dropdown height
        LayoutElement dropdownLayout = dropdownObj.AddComponent<LayoutElement>();
        dropdownLayout.preferredHeight = 40f;
        
        // Initially hide template
        templateObj.SetActive(false);
    }
    
    private void CreateButtonSection()
    {
        GameObject buttonSection = new GameObject("ButtonSection");
        buttonSection.transform.SetParent(GetContentPanel().transform, false);
        
        // Horizontal layout for buttons
        HorizontalLayoutGroup buttonLayout = buttonSection.AddComponent<HorizontalLayoutGroup>();
        buttonLayout.spacing = 20f;
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.childControlWidth = true;
        buttonLayout.childControlHeight = true;
        buttonLayout.childForceExpandWidth = false;
        buttonLayout.childForceExpandHeight = false;
        
        // Previous button
        CreateButton(buttonSection, "PreviousButton", "Previous", secondaryButtonColor);
        
        // Next button
        CreateButton(buttonSection, "NextButton", "Next", primaryButtonColor);
        
        // Start interview button
        CreateButton(buttonSection, "StartInterviewButton", "Start Interview", new Color(0.2f, 0.8f, 0.2f, 1f));
        
        // Set button section height
        LayoutElement buttonSectionLayout = buttonSection.AddComponent<LayoutElement>();
        buttonSectionLayout.preferredHeight = 60f;
    }
    
    private void CreateButton(GameObject parent, string name, string text, Color color)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent.transform, false);
        
        // Add RectTransform to button
        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        
        // Button component
        Button button = buttonObj.AddComponent<Button>();
        
        // Button background
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = color;
        
        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = buttonFontSize;
                buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Setup button
        button.targetGraphic = buttonBg;
        
        // Button size
        LayoutElement buttonLayoutElement = buttonObj.AddComponent<LayoutElement>();
        buttonLayoutElement.preferredWidth = 120f;
        buttonLayoutElement.preferredHeight = 40f;
        
        // Special handling for start interview button
        if (name == "StartInterviewButton")
        {
            buttonObj.SetActive(false);
            buttonLayoutElement.preferredWidth = 150f;
        }
    }
    
    private void SetupOnboardingManager()
    {
        // Add OnboardingManager component to main panel
        generatedOnboardingManager = mainPanel.AddComponent<OnboardingManager>();
        
        // Auto-assign UI references
        GameObject contentPanel = GetContentPanel();
        
        generatedOnboardingManager.onboardingPanel = mainPanel;
        generatedOnboardingManager.dialogueText = contentPanel.transform.Find("ContentSection/DialogueText").GetComponent<TextMeshProUGUI>();
        generatedOnboardingManager.userInputField = contentPanel.transform.Find("InputSection/UserInputField").GetComponent<TMP_InputField>();
        generatedOnboardingManager.selectionPanel = contentPanel.transform.Find("SelectionPanel").gameObject;
        generatedOnboardingManager.nextButton = contentPanel.transform.Find("ButtonSection/NextButton").GetComponent<Button>();
        generatedOnboardingManager.previousButton = contentPanel.transform.Find("ButtonSection/PreviousButton").GetComponent<Button>();
        generatedOnboardingManager.startInterviewButton = contentPanel.transform.Find("ButtonSection/StartInterviewButton").GetComponent<Button>();
        generatedOnboardingManager.roleDropdown = contentPanel.transform.Find("SelectionPanel/RoleDropdown").GetComponent<TMP_Dropdown>();
        generatedOnboardingManager.interviewTypeDropdown = contentPanel.transform.Find("SelectionPanel/InterviewTypeDropdown").GetComponent<TMP_Dropdown>();
        generatedOnboardingManager.difficultyDropdown = contentPanel.transform.Find("SelectionPanel/DifficultyDropdown").GetComponent<TMP_Dropdown>();
        
        // Create and assign selection summary text
        CreateSelectionSummaryText(contentPanel);
        
        Debug.Log("[UI GENERATOR] OnboardingManager component configured with all UI references");
    }
    
    private void CreateSelectionSummaryText(GameObject contentPanel)
    {
        GameObject summarySection = new GameObject("SelectionSummarySection");
        summarySection.transform.SetParent(contentPanel.transform, false);
        
        // Add RectTransform to summary section
        RectTransform summaryRect = summarySection.AddComponent<RectTransform>();
        summaryRect.anchorMin = Vector2.zero;
        summaryRect.anchorMax = Vector2.one;
        summaryRect.offsetMin = Vector2.zero;
        summaryRect.offsetMax = Vector2.zero;
        
        // Selection summary text
        GameObject summaryObj = new GameObject("SelectionSummaryText");
        summaryObj.transform.SetParent(summarySection.transform, false);
        
        // Add RectTransform first
        RectTransform summaryTextRect = summaryObj.AddComponent<RectTransform>();
        summaryTextRect.anchorMin = Vector2.zero;
        summaryTextRect.anchorMax = Vector2.one;
        summaryTextRect.offsetMin = Vector2.zero;
        summaryTextRect.offsetMax = Vector2.zero;
        
        // Then add TextMeshProUGUI component
        TextMeshProUGUI summaryText = summaryObj.AddComponent<TextMeshProUGUI>();
        if (summaryText != null)
        {
            summaryText.text = "Selection summary will appear here...";
            summaryText.fontSize = dialogueFontSize - 2;
            summaryText.color = new Color(textColor.r, textColor.g, textColor.b, 0.9f);
            summaryText.alignment = TextAlignmentOptions.TopLeft;
            summaryText.enableWordWrapping = true;
        }
        
        // Set summary section height
        LayoutElement summaryLayout = summarySection.AddComponent<LayoutElement>();
        summaryLayout.preferredHeight = 140f;
        
        // Initially hide summary section
        summarySection.SetActive(false);
        
        // Assign to onboarding manager
        generatedOnboardingManager.selectionSummaryText = summaryText;
    }
    
    private GameObject GetContentPanel()
    {
        if (mainPanel == null)
        {
            Debug.LogError("[UI GENERATOR] Main panel is null!");
            return null;
        }
        
        Transform contentTransform = mainPanel.transform.Find("ContentPanel");
        if (contentTransform == null)
        {
            Debug.LogError("[UI GENERATOR] ContentPanel not found in main panel!");
            return null;
        }
        
        return contentTransform.gameObject;
    }
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    [Button("Test UI Visibility")]
    [GUIColor(0.6f, 0.8f, 0.6f)]
    public void TestUIVisibility()
    {
        if (generatedOnboardingManager == null)
        {
            Debug.LogWarning("Generate UI first!");
            return;
        }
        
        // Show/hide different sections for testing
        bool inputVisible = generatedOnboardingManager.userInputField.gameObject.activeSelf;
        bool selectionVisible = generatedOnboardingManager.selectionPanel.activeSelf;
        
        generatedOnboardingManager.userInputField.gameObject.SetActive(!inputVisible);
        generatedOnboardingManager.selectionPanel.SetActive(!selectionVisible);
        
        Debug.Log($"[UI TEST] Input visible: {!inputVisible}, Selection visible: {!selectionVisible}");
    }
} 