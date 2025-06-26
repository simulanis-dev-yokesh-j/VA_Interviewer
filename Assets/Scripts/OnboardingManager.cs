using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class OnboardingManager : SerializedMonoBehaviour
{
    [Title("Interview Onboarding System")]
    [InfoBox("Collects user information through a guided dialogue interface")]
    
    // ========================================
    // UI COMPONENTS
    // ======================================== 
    
    [TitleGroup("UI References")]
    [Required]
    [LabelText("Main Onboarding Panel")]
    public GameObject onboardingPanel;
    
    [TitleGroup("UI References")]
    [Required]
    [LabelText("Dialogue Text")]
    [InfoBox("Displays the current onboarding question/instruction")]
    public TMP_Text dialogueText;
    
    [TitleGroup("UI References")]
    [Required]
    [LabelText("User Input Field")]
    [InfoBox("Text input for name and other text responses")]
    public TMP_InputField userInputField;
    
    [TitleGroup("UI References")]
    [LabelText("Selection Panel")]
    [InfoBox("Panel containing selection options (dropdown/toggles)")]
    public GameObject selectionPanel;
    
    [TitleGroup("UI References")]
    [LabelText("Next Button")]
    public Button nextButton;
    
    [TitleGroup("UI References")]
    [LabelText("Previous Button")]
    public Button previousButton;
    
    [TitleGroup("UI References")]
    [LabelText("Start Interview Button")]
    public Button startInterviewButton;
    
    [TitleGroup("UI References")]
    [LabelText("Selection Summary Text")]
    [InfoBox("Shows current selections (name, role, type, difficulty)")]
    public TMP_Text selectionSummaryText;
    
    // ========================================
    // SELECTION OPTIONS UI
    // ========================================
    
    [TitleGroup("Selection Options")]
    [LabelText("Role Dropdown")]
    [InfoBox("Dropdown for role selection")]
    public TMP_Dropdown roleDropdown;
    
    [TitleGroup("Selection Options")]
    [LabelText("Interview Type Dropdown")]
    [InfoBox("Dropdown for interview type selection")]
    public TMP_Dropdown interviewTypeDropdown;
    
    [TitleGroup("Selection Options")]
    [LabelText("Difficulty Dropdown")]
    [InfoBox("Dropdown for difficulty selection")]
    public TMP_Dropdown difficultyDropdown;
    

    
    // ========================================
    // ONBOARDING SETTINGS
    // ========================================
    

    
    [TitleGroup("Onboarding Settings")]
    [LabelText("Auto-advance Timer")]
    [InfoBox("Seconds to wait before auto-advancing (0 = disabled)")]
    [Range(0f, 10f)]
    public float autoAdvanceTimer = 0f;
    
    [TitleGroup("Onboarding Settings")]
    [LabelText("Type Writer Effect")]
    [InfoBox("Animate text appearing character by character")]
    public bool useTypeWriterEffect = true;
    
    [TitleGroup("Onboarding Settings")]
    [LabelText("Type Writer Speed")]
    [ShowIf("useTypeWriterEffect")]
    [Range(0.01f, 0.2f)]
    public float typeWriterSpeed = 0.05f;
    
    [TitleGroup("TTS Settings")]
    [LabelText("Enable TTS")]
    [InfoBox("Speak dialogue text using Text-to-Speech")]
    public bool enableTTS = true;
    
    [TitleGroup("TTS Settings")]
    [LabelText("TTS with TypeWriter")]
    [InfoBox("Speak while typing (true) or after typing is complete (false)")]
    [ShowIf("enableTTS")]
    public bool ttsWithTypeWriter = false;
    
    [TitleGroup("TTS Settings")]
    [LabelText("TTS Delay")]
    [InfoBox("Delay before starting TTS (in seconds)")]
    [ShowIf("enableTTS")]
    [Range(0f, 2f)]
    public float ttsDelay = 0.2f;
    
    // ========================================
    // COLLECTED DATA
    // ========================================
    
    [TitleGroup("Collected Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("User Name")]
    public string collectedUserName = "";
    
    [TitleGroup("Collected Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Selected Role")]
    public string collectedRole = "";
    
    [TitleGroup("Collected Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Selected Interview Type")]
    public string collectedInterviewType = "";
    
    [TitleGroup("Collected Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Selected Difficulty")]
    public string collectedDifficulty = "";
    
    [TitleGroup("Collected Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Onboarding Complete")]
    public bool isOnboardingComplete = false;
    
    // ========================================
    // INTEGRATION
    // ========================================
    
    [TitleGroup("Integration")]
    [LabelText("Interview Conversation Manager")]
    [InfoBox("Will receive the collected information")]
    public InterviewConversationManager interviewManager;
    
    [TitleGroup("Integration")]
    [LabelText("TTS Manager")]
    [InfoBox("Text-to-Speech for speaking dialogue")]
    public TTSManager ttsManager;
    
    [TitleGroup("Integration")]
    [LabelText("On Onboarding Complete")]
    [InfoBox("Event triggered when onboarding is finished")]
    public UnityEvent<string, string, string, string> OnOnboardingComplete;
    
    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    
    private int currentStep = 0;
    private bool isWaitingForInput = false;
    private Coroutine typeWriterCoroutine;
    private Coroutine autoAdvanceCoroutine;
    
    // Onboarding dialogue steps
    private readonly OnboardingStep[] onboardingSteps = new OnboardingStep[]
    {
        new OnboardingStep(
            StepType.Welcome,
            "Welcome to the AI Interview System!",
            "Hello! I'm your AI interview assistant. I'll help you prepare for your interview session. Let's start by getting to know you a bit better.",
            false,
            false
        ),
        new OnboardingStep(
            StepType.NameInput,
            "What's your name?",
            "Please enter your full name. This will help me personalize our conversation and address you properly during the interview.",
            true,
            false
        ),
        new OnboardingStep(
            StepType.AllSelections,
            "Configure your interview settings",
            "Please select your interview preferences below. Default options are already selected, but feel free to customize them to match your needs.",
            false,
            true
        ),
        new OnboardingStep(
            StepType.Confirmation,
            "Let's review your information",
            "Perfect! I have all the information I need. Please review your selections and click 'Start Interview' when you're ready to begin.",
            false,
            false
        )
    };
    
    // ========================================
    // UNITY LIFECYCLE
    // ========================================
    
    private void Start()
    {
        InitializeOnboarding();
    }
    
    private void OnDestroy()
    {
        // Clean up coroutines
        if (typeWriterCoroutine != null)
            StopCoroutine(typeWriterCoroutine);
        if (autoAdvanceCoroutine != null)
            StopCoroutine(autoAdvanceCoroutine);
    }
    
    // ========================================
    // INITIALIZATION
    // ========================================
    
    private void InitializeOnboarding()
    {
        // Setup UI
        SetupButtons();
        SetupDropdowns();
        
        // Reset state
        currentStep = 0;
        isOnboardingComplete = false;
        
        // Clear collected data
        ClearCollectedData();
        
        // Show onboarding panel
        if (onboardingPanel != null)
            onboardingPanel.SetActive(true);
        
        // Start with first step
        ShowCurrentStep();
        
        Debug.Log("[ONBOARDING] Onboarding system initialized");
    }
    
    private void SetupButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(NextStep);
        }
        
        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(PreviousStep);
        }
        
        if (startInterviewButton != null)
        {
            startInterviewButton.onClick.RemoveAllListeners();
            startInterviewButton.onClick.AddListener(StartInterview);
            startInterviewButton.gameObject.SetActive(false);
        }
        
        // Add listener to input field to update button states when text changes
        if (userInputField != null)
        {
            userInputField.onValueChanged.RemoveAllListeners();
            userInputField.onValueChanged.AddListener(OnInputFieldChanged);
        }
    }
    
    private void SetupDropdowns()
    {
        // Setup role dropdown
        if (roleDropdown != null)
        {
            roleDropdown.options.Clear();
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Software Developer"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Data Scientist"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Product Manager"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("UX/UI Designer"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("DevOps Engineer"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Business Analyst"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Project Manager"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Marketing Manager"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Sales Representative"));
            roleDropdown.options.Add(new TMP_Dropdown.OptionData("Other"));
            roleDropdown.value = 0;
            roleDropdown.onValueChanged.AddListener(OnRoleSelected);
            
            // Automatically select first option and collect data
            OnRoleSelected(0);
        }
        
        // Setup interview type dropdown
        if (interviewTypeDropdown != null)
        {
            interviewTypeDropdown.options.Clear();
            interviewTypeDropdown.options.Add(new TMP_Dropdown.OptionData("General Interview"));
            interviewTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Technical Interview"));
            interviewTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Behavioral Interview"));
            interviewTypeDropdown.options.Add(new TMP_Dropdown.OptionData("Case Study Interview"));
            interviewTypeDropdown.value = 0;
            interviewTypeDropdown.onValueChanged.AddListener(OnInterviewTypeSelected);
            
            // Automatically select first option and collect data
            OnInterviewTypeSelected(0);
        }
        
        // Setup difficulty dropdown
        if (difficultyDropdown != null)
        {
            difficultyDropdown.options.Clear();
            difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Easy - Beginner Level"));
            difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Medium - Intermediate Level"));
            difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Hard - Advanced Level"));
            difficultyDropdown.options.Add(new TMP_Dropdown.OptionData("Expert - Senior Level"));
            difficultyDropdown.value = 1; // Default to Medium
            difficultyDropdown.onValueChanged.AddListener(OnDifficultySelected);
            
            // Automatically select default option and collect data
            OnDifficultySelected(1); // Medium difficulty
        }
    }
    
    // ========================================
    // STEP MANAGEMENT
    // ========================================
    
    private void ShowCurrentStep()
    {
        if (currentStep >= onboardingSteps.Length)
        {
            CompleteOnboarding();
            return;
        }
        
        OnboardingStep step = onboardingSteps[currentStep];
        
        // Update UI visibility
        UpdateUIVisibility(step);
        
        // Show dialogue with typewriter effect and TTS
        if (useTypeWriterEffect)
        {
            if (typeWriterCoroutine != null)
                StopCoroutine(typeWriterCoroutine);
            typeWriterCoroutine = StartCoroutine(TypeWriterEffect(step.dialogue));
        }
        else
        {
            if (dialogueText != null)
                dialogueText.text = step.dialogue;
            
            // Speak immediately if not using typewriter effect
            if (enableTTS && !ttsWithTypeWriter)
            {
                SpeakDialogue(step.dialogue);
            }
        }
        
        // Handle auto-advance
        if (autoAdvanceTimer > 0 && !step.requiresInput && !step.requiresSelection)
        {
            if (autoAdvanceCoroutine != null)
                StopCoroutine(autoAdvanceCoroutine);
            autoAdvanceCoroutine = StartCoroutine(AutoAdvance());
        }
        
        // Update button states
        UpdateButtonStates();
        
        Debug.Log($"[ONBOARDING] Showing step {currentStep + 1}/{onboardingSteps.Length}: {step.title}");
    }
    
    private void UpdateUIVisibility(OnboardingStep step)
    {
        // Show/hide input field
        if (userInputField != null)
        {
            userInputField.gameObject.SetActive(step.requiresInput);
            if (step.requiresInput)
            {
                userInputField.text = "";
                userInputField.placeholder.GetComponent<TMP_Text>().text = "Enter your name here...";
            }
        }
        
        // Show/hide selection panel
        if (selectionPanel != null)
        {
            selectionPanel.SetActive(step.requiresSelection);
        }
        
        // Show/hide selection summary (only during confirmation)
        if (selectionSummaryText != null)
        {
            selectionSummaryText.gameObject.SetActive(step.stepType == StepType.Confirmation);
        }
        
        // Show appropriate selection UI based on step type
        if (step.requiresSelection)
        {
            ShowAppropriateSelectionUI(step.stepType);
        }
        
        // Handle confirmation step
        if (step.stepType == StepType.Confirmation)
        {
            ShowConfirmationInfo();
        }
    }
    
    private void ShowAppropriateSelectionUI(StepType stepType)
    {
        // Hide all selection UIs first
        if (roleDropdown != null) roleDropdown.gameObject.SetActive(false);
        if (interviewTypeDropdown != null) interviewTypeDropdown.gameObject.SetActive(false);
        if (difficultyDropdown != null) difficultyDropdown.gameObject.SetActive(false);
        
        // Show appropriate UI based on step type
        switch (stepType)
        {
            case StepType.AllSelections:
                // Show all dropdowns at once
                if (roleDropdown != null)
                    roleDropdown.gameObject.SetActive(true);
                if (interviewTypeDropdown != null)
                    interviewTypeDropdown.gameObject.SetActive(true);
                if (difficultyDropdown != null)
                    difficultyDropdown.gameObject.SetActive(true);
                break;
        }
    }
    
    private void ShowConfirmationInfo()
    {
        string confirmationText = $"Perfect! Everything is set up for your interview. " +
                                 $"I can see your selections below. " +
                                 $"If you'd like to make changes, use the Previous button. " +
                                 $"Otherwise, click 'Start Interview' to begin!";
        
        if (dialogueText != null)
            dialogueText.text = confirmationText;
        
        // Show selection summary
        UpdateSelectionSummary();
        if (selectionSummaryText != null)
            selectionSummaryText.gameObject.SetActive(true);
        
        // Show start interview button
        if (startInterviewButton != null)
            startInterviewButton.gameObject.SetActive(true);
    }
    
    private void UpdateSelectionSummary()
    {
        if (selectionSummaryText == null)
            return;
        
        string summaryText = $"<b>Your Interview Setup:</b>\n\n" +
                            $"• <b>Name:</b> {collectedUserName}\n" +
                            $"• <b>Position:</b> {collectedRole}\n" +
                            $"• <b>Interview Type:</b> {GetDisplayNameForInterviewType(collectedInterviewType)}\n" +
                            $"• <b>Difficulty Level:</b> {GetDisplayNameForDifficulty(collectedDifficulty)}";
        
        selectionSummaryText.text = summaryText;
        
        Debug.Log($"[ONBOARDING] Updated selection summary");
    }
    
    private string GetDisplayNameForInterviewType(string apiFormat)
    {
        switch (apiFormat)
        {
            case "general": return "General Interview";
            case "technical": return "Technical Interview";
            case "behavioral": return "Behavioral Interview";
            case "case_study": return "Case Study Interview";
            default: return apiFormat;
        }
    }
    
    private string GetDisplayNameForDifficulty(string apiFormat)
    {
        switch (apiFormat)
        {
            case "easy": return "Easy - Beginner Level";
            case "medium": return "Medium - Intermediate Level";
            case "hard": return "Hard - Advanced Level";
            case "expert": return "Expert - Senior Level";
            default: return apiFormat;
        }
    }
    
    private void UpdateButtonStates()
    {
        // Previous button
        if (previousButton != null)
            previousButton.interactable = currentStep > 0;
        
        // Next button
        if (nextButton != null)
        {
            bool canAdvance = CanAdvanceToNextStep();
            nextButton.interactable = canAdvance;
            
            // Hide next button on confirmation step
            if (currentStep == onboardingSteps.Length - 1)
                nextButton.gameObject.SetActive(false);
            else
                nextButton.gameObject.SetActive(true);
        }
    }
    
    private bool CanAdvanceToNextStep()
    {
        if (currentStep >= onboardingSteps.Length)
            return false;
        
        OnboardingStep step = onboardingSteps[currentStep];
        
        if (step.requiresInput)
        {
            if (userInputField == null)
                return false;
            
            string inputText = userInputField.text.Trim();
            bool hasValidInput = !string.IsNullOrEmpty(inputText) && inputText.Length >= 2; // Require at least 2 characters for name
            return hasValidInput;
        }
        
        if (step.requiresSelection)
        {
            switch (step.stepType)
            {
                case StepType.AllSelections:
                    // All selections must be valid
                    return !string.IsNullOrEmpty(collectedRole) && 
                           !string.IsNullOrEmpty(collectedInterviewType) && 
                           !string.IsNullOrEmpty(collectedDifficulty);
            }
        }
        
        return true;
    }
    
    // ========================================
    // NAVIGATION
    // ========================================
    
    public void NextStep()
    {
        if (!CanAdvanceToNextStep())
            return;
        
        // Collect data from current step
        CollectCurrentStepData();
        
        // Advance to next step
        currentStep++;
        ShowCurrentStep();
    }
    
    public void PreviousStep()
    {
        if (currentStep <= 0)
            return;
        
        currentStep--;
        ShowCurrentStep();
    }
    
    private void CollectCurrentStepData()
    {
        OnboardingStep step = onboardingSteps[currentStep];
        
        if (step.requiresInput && step.stepType == StepType.NameInput)
        {
            collectedUserName = userInputField.text.Trim();
            Debug.Log($"[ONBOARDING] Collected name: {collectedUserName}");
        }
    }
    
    // ========================================
    // INPUT HANDLERS
    // ========================================
    
    public void OnInputFieldChanged(string text)
    {
        // Update button states when input field text changes
        UpdateButtonStates();
        Debug.Log($"[ONBOARDING] Input field changed: '{text}' - Can advance: {CanAdvanceToNextStep()}");
    }
    
    // ========================================
    // SELECTION HANDLERS
    // ========================================
    
    public void OnRoleSelected(int index)
    {
        if (roleDropdown != null && index >= 0 && index < roleDropdown.options.Count)
        {
            collectedRole = roleDropdown.options[index].text;
            UpdateButtonStates();
            UpdateSelectionSummary();
            Debug.Log($"[ONBOARDING] Role selected: {collectedRole}");
        }
    }
    
    public void OnInterviewTypeSelected(int index)
    {
        if (interviewTypeDropdown != null && index >= 0 && index < interviewTypeDropdown.options.Count)
        {
            string selectedText = interviewTypeDropdown.options[index].text;
            // Convert display text to API format
            collectedInterviewType = ConvertToAPIFormat(selectedText);
            UpdateButtonStates();
            UpdateSelectionSummary();
            Debug.Log($"[ONBOARDING] Interview type selected: {collectedInterviewType}");
        }
    }
    
    public void OnDifficultySelected(int index)
    {
        if (difficultyDropdown != null && index >= 0 && index < difficultyDropdown.options.Count)
        {
            string selectedText = difficultyDropdown.options[index].text;
            // Convert display text to API format
            collectedDifficulty = ConvertDifficultyToAPIFormat(selectedText);
            UpdateButtonStates();
            UpdateSelectionSummary();
            Debug.Log($"[ONBOARDING] Difficulty selected: {collectedDifficulty}");
        }
    }
    
    private string ConvertToAPIFormat(string displayText)
    {
        switch (displayText)
        {
            case "General Interview": return "general";
            case "Technical Interview": return "technical";
            case "Behavioral Interview": return "behavioral";
            case "Case Study Interview": return "case_study";
            default: return "general";
        }
    }
    
    private string ConvertDifficultyToAPIFormat(string displayText)
    {
        if (displayText.StartsWith("Easy")) return "easy";
        if (displayText.StartsWith("Medium")) return "medium";
        if (displayText.StartsWith("Hard")) return "hard";
        if (displayText.StartsWith("Expert")) return "expert";
        return "medium";
    }
    
    // ========================================
    // COMPLETION
    // ========================================
    
    public void StartInterview()
    {
        CompleteOnboarding();
    }
    
    private void CompleteOnboarding()
    {
        isOnboardingComplete = true;
        
        // Trigger completion event
        OnOnboardingComplete?.Invoke(collectedUserName, collectedRole, collectedInterviewType, collectedDifficulty);
        
        // Pass data to interview manager
        if (interviewManager != null)
        {
            interviewManager.candidateName = collectedUserName;
            interviewManager.position = collectedRole;
            interviewManager.interviewType = collectedInterviewType;
            interviewManager.difficultyLevel = collectedDifficulty;
            interviewManager.StartInterview();
            Debug.Log($"[ONBOARDING] Data passed to interview manager");
        }
        
        // Hide onboarding panel
        if (onboardingPanel != null)
            onboardingPanel.SetActive(false);
        
        Debug.Log($"[ONBOARDING] Onboarding completed: Name={collectedUserName}, Role={collectedRole}, Type={collectedInterviewType}, Difficulty={collectedDifficulty}");
    }
    
    // ========================================
    // TTS METHODS
    // ========================================
    
    private void SpeakDialogue(string text)
    {
        if (ttsManager == null)
        {
            Debug.LogWarning("[ONBOARDING] TTS Manager not assigned - cannot speak dialogue");
            return;
        }
        
        if (string.IsNullOrEmpty(text))
            return;
        
        // Clean the text for TTS (remove special formatting if needed)
        string cleanText = CleanTextForTTS(text);
        
        // Speak the dialogue
        ttsManager.SpeakTextImmediate(cleanText);
        
        Debug.Log($"[ONBOARDING] Speaking: {cleanText}");
    }
    
    private string CleanTextForTTS(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        // Remove markdown-like formatting, extra spaces, etc.
        string cleanText = text
            .Replace("**", "") // Remove bold markers
            .Replace("*", "")  // Remove italic markers
            .Replace("•", "")  // Remove bullet points
            .Replace("\n\n", ". ") // Replace double newlines with period
            .Replace("\n", " ")    // Replace single newlines with space
            .Trim();
        
        return cleanText;
    }
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    private IEnumerator TypeWriterEffect(string text)
    {
        if (dialogueText != null)
        {
            dialogueText.text = "";
            
            // Start TTS in parallel if speaking while typing
            if (enableTTS && ttsWithTypeWriter)
            {
                StartCoroutine(DelayedTTS(text, ttsDelay));
            }
            
            // Type the text character by character
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typeWriterSpeed);
            }
            
            // Speak after typing is complete if not speaking while typing
            if (enableTTS && !ttsWithTypeWriter)
            {
                yield return new WaitForSeconds(ttsDelay);
                SpeakDialogue(text);
            }
        }
    }
    
    private IEnumerator DelayedTTS(string text, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpeakDialogue(text);
    }
    
    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoAdvanceTimer);
        if (CanAdvanceToNextStep())
        {
            NextStep();
        }
    }
    
    public void ClearCollectedData()
    {
        collectedUserName = "";
        collectedRole = "";
        collectedInterviewType = "";
        collectedDifficulty = "";
        isOnboardingComplete = false;
    }
    
    // ========================================
    // PUBLIC CONTROL METHODS
    // ========================================
    
    [Button("Restart Onboarding")]
    [GUIColor(0.8f, 0.8f, 0.4f)]
    public void RestartOnboarding()
    {
        ClearCollectedData();
        currentStep = 0;
        InitializeOnboarding();
    }
    
    [Button("Skip to Confirmation")]
    [GUIColor(0.6f, 0.8f, 0.6f)]
    [ShowIf("@!isOnboardingComplete")]
    public void SkipToConfirmation()
    {
        // Set dummy data for testing
        collectedUserName = "Test User";
        collectedRole = "Software Developer";
        collectedInterviewType = "general";
        collectedDifficulty = "medium";
        
        currentStep = onboardingSteps.Length - 1;
        ShowCurrentStep();
    }
    
    [Button("Debug Current Step")]
    [GUIColor(0.8f, 0.6f, 0.8f)]
    [ShowIf("@!isOnboardingComplete")]
    public void DebugCurrentStep()
    {
        if (currentStep < onboardingSteps.Length)
        {
            OnboardingStep step = onboardingSteps[currentStep];
            string inputText = userInputField != null ? userInputField.text : "NULL";
            
            Debug.Log($"[ONBOARDING DEBUG] Current Step: {currentStep + 1}/{onboardingSteps.Length}");
            Debug.Log($"[ONBOARDING DEBUG] Step Type: {step.stepType}");
            Debug.Log($"[ONBOARDING DEBUG] Requires Input: {step.requiresInput}");
            Debug.Log($"[ONBOARDING DEBUG] Requires Selection: {step.requiresSelection}");
            Debug.Log($"[ONBOARDING DEBUG] Input Field Text: '{inputText}'");
            Debug.Log($"[ONBOARDING DEBUG] Can Advance: {CanAdvanceToNextStep()}");
            Debug.Log($"[ONBOARDING DEBUG] Next Button Active: {nextButton != null && nextButton.gameObject.activeSelf}");
            Debug.Log($"[ONBOARDING DEBUG] Next Button Interactable: {nextButton != null && nextButton.interactable}");
        }
    }
    
    [Button("Test Speak Current Dialogue")]
    [GUIColor(0.4f, 0.8f, 0.8f)]
    [ShowIf("@enableTTS && !isOnboardingComplete")]
    public void TestSpeakCurrentDialogue()
    {
        if (currentStep < onboardingSteps.Length)
        {
            OnboardingStep step = onboardingSteps[currentStep];
            SpeakDialogue(step.dialogue);
        }
    }
    
    [Button("Stop TTS")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [ShowIf("@enableTTS")]
    public void StopTTS()
    {
        if (ttsManager != null && ttsManager.audioSource != null)
        {
            ttsManager.audioSource.Stop();
            Debug.Log("[ONBOARDING] TTS stopped");
        }
    }
}

// ========================================
// SUPPORTING CLASSES
// ========================================

[System.Serializable]
public class OnboardingStep
{
    public StepType stepType;
    public string title;
    public string dialogue;
    public bool requiresInput;
    public bool requiresSelection;
    
    public OnboardingStep(StepType type, string stepTitle, string stepDialogue, bool needsInput, bool needsSelection)
    {
        stepType = type;
        title = stepTitle;
        dialogue = stepDialogue;
        requiresInput = needsInput;
        requiresSelection = needsSelection;
    }
}

public enum StepType
{
    Welcome,
    NameInput,
    AllSelections,
    Confirmation
} 