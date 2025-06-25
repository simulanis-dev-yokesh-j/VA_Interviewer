using UnityEngine;
using TMPro;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

public class InterviewIntegrationManager : SerializedMonoBehaviour
{
    [Title("Interview Integration Manager")]
    [InfoBox("This script integrates Speech-to-Text with the Interview API for a complete voice-based interview experience.")]
    
    // ========================================
    // COMPONENT REFERENCES
    // ========================================
    
    [TitleGroup("Component References")]
    [Required]
    [LabelText("SST Manager")]
    public SSTManager sstManager;
    
    [TitleGroup("Component References")]
    [Required]
    [LabelText("API Manager")]
    public InterviewAPIManager apiManager;
    
    // ========================================
    // INTERVIEW SETTINGS
    // ========================================
    
    [TitleGroup("Interview Settings")]
    [LabelText("Candidate Name")]
    public string candidateName = "John Doe";
    
    [TitleGroup("Interview Settings")]
    [LabelText("Position")]
    public string position = "Software Developer";
    
    [TitleGroup("Interview Settings")]
    [ValueDropdown("GetInterviewTypes")]
    [LabelText("Interview Type")]
    public string interviewType = "general";
    
    [TitleGroup("Interview Settings")]
    [ValueDropdown("GetDifficultyLevels")]
    [LabelText("Difficulty Level")]
    public string difficultyLevel = "medium";
    
    // ========================================
    // CURRENT INTERVIEW STATE
    // ========================================
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Interview Status")]
    public string interviewStatus = "Ready to start";
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Current Session ID")]
    public string currentSessionId;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Message Count")]
    public int messageCount = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Last AI Response")]
    [TextArea(3, 8)]
    public string lastAIResponse;
    
    // ========================================
    // CONVERSATION HISTORY
    // ========================================
    
    [TitleGroup("Conversation")]
    [ShowInInspector, ReadOnly]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [LabelText("Conversation History")]
    public List<ConversationEntry> conversationHistory = new List<ConversationEntry>();
    
    [System.Serializable]
    public class ConversationEntry
    {
        public string speaker;
        public string message;
        public string timestamp;
        
        public ConversationEntry(string speaker, string message)
        {
            this.speaker = speaker;
            this.message = message;
            this.timestamp = DateTime.Now.ToString("HH:mm:ss");
        }
    }
    
    // ========================================
    // INTERVIEW CONTROLS
    // ========================================
    
    [TitleGroup("Interview Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [EnableIf("@interviewStatus == \"Ready to start\"")]
    public async void StartInterview()
    {
        await StartInterviewSession();
    }
    
    [TitleGroup("Interview Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.4f, 0.8f)]
    [EnableIf("@interviewStatus == \"Interview in progress\" && !sstManager.isRecording")]
    public void StartVoiceInput()
    {
        sstManager.StartRecording();
        interviewStatus = "Recording voice input...";
    }
    
    [TitleGroup("Interview Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.6f, 0.2f)]
    [EnableIf("@sstManager != null && sstManager.isRecording")]
    public async void StopVoiceInputAndSend()
    {
        sstManager.StopRecording();
        interviewStatus = "Processing voice...";
        
        // Wait a moment for recording to process
        await Task.Delay(1000);
        
        // Send to Azure Speech Service
        sstManager.SendToAzureSpeechService();
        
        // Wait for speech recognition to complete
        await WaitForSpeechRecognition();
        
        // Send recognized text to API
        await SendRecognizedTextToAPI();
    }
    
    [TitleGroup("Interview Controls")]
    [Button("End Interview")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [EnableIf("@!string.IsNullOrEmpty(currentSessionId)")]
    public async void EndInterview()
    {
        await EndInterviewSession();
    }
    
    // ========================================
    // MANUAL INPUT (FOR TESTING)
    // ========================================
    
    [TitleGroup("Manual Input")]
    [LabelText("Manual Message")]
    [TextArea(2, 5)]
    public string manualMessage = "I have 3 years of experience in C# development.";
    
    [TitleGroup("Manual Input")]
    [Button("Send Manual Message")]
    [EnableIf("@!string.IsNullOrEmpty(currentSessionId)")]
    public async void SendManualMessage()
    {
        if (!string.IsNullOrEmpty(manualMessage) && !string.IsNullOrEmpty(currentSessionId))
        {
            await SendMessageToAPI(manualMessage);
            manualMessage = ""; // Clear after sending
        }
    }
    
    // ========================================
    // DROPDOWN VALUE PROVIDERS
    // ========================================
    
    private List<string> GetInterviewTypes()
    {
        return new List<string> { "general", "technical", "behavioral", "coding" };
    }
    
    private List<string> GetDifficultyLevels()
    {
        return new List<string> { "easy", "medium", "hard" };
    }
    
    // ========================================
    // CORE INTERVIEW LOGIC
    // ========================================
    
    private async Task StartInterviewSession()
    {
        try
        {
            interviewStatus = "Starting interview session...";
            
            // Start session with API
            var response = await apiManager.StartInterviewSession(candidateName, position, interviewType, difficultyLevel);
            
            // Update state
            currentSessionId = response.session_id;
            interviewStatus = "Interview in progress";
            lastAIResponse = response.welcome_message;
            messageCount = 0;
            
            // Add welcome message to conversation
            conversationHistory.Clear();
            conversationHistory.Add(new ConversationEntry("AI Interviewer", response.welcome_message));
            
            Debug.Log($"Interview started successfully! Session ID: {currentSessionId}");
        }
        catch (Exception e)
        {
            interviewStatus = $"Failed to start interview: {e.Message}";
            Debug.LogError($"Failed to start interview: {e}");
        }
    }
    
    private async Task WaitForSpeechRecognition()
    {
        // Wait for Azure Speech Service to complete processing
        int maxWaitTime = 30; // seconds
        int waitTime = 0;
        
        while (sstManager.waitingForReco && waitTime < maxWaitTime)
        {
            await Task.Delay(500);
            waitTime++;
        }
        
        if (waitTime >= maxWaitTime)
        {
            interviewStatus = "Speech recognition timed out";
            Debug.LogWarning("Speech recognition timed out");
        }
    }
    
    private async Task SendRecognizedTextToAPI()
    {
        // Check if we have recognized text
        string recognizedText = sstManager.recognitionResult;
        
        if (string.IsNullOrEmpty(recognizedText) || recognizedText.StartsWith("❌") || recognizedText.StartsWith("⚠️"))
        {
            interviewStatus = "Speech recognition failed - please try again";
            return;
        }
        
        // Extract the actual text (remove the ✅ prefix if present)
        if (recognizedText.StartsWith("✅ Recognized: "))
        {
            recognizedText = recognizedText.Substring("✅ Recognized: ".Length);
        }
        
        await SendMessageToAPI(recognizedText);
    }
    
    private async Task SendMessageToAPI(string message)
    {
        try
        {
            interviewStatus = "Sending message to AI...";
            
            // Add user message to conversation history
            conversationHistory.Add(new ConversationEntry("Candidate", message));
            
            // Send to API
            var response = await apiManager.SendChatMessage(message, currentSessionId);
            
            // Update state
            lastAIResponse = response.response;
            messageCount++;
            
            // Add AI response to conversation history
            conversationHistory.Add(new ConversationEntry("AI Interviewer", response.response));
            
            // Check if interview ended
            if (response.session_ended)
            {
                interviewStatus = "Interview completed";
                
                if (!string.IsNullOrEmpty(response.feedback))
                {
                    conversationHistory.Add(new ConversationEntry("System", $"Interview Feedback: {response.feedback}"));
                }
                
                Debug.Log("Interview session ended by AI");
            }
            else
            {
                interviewStatus = "Interview in progress - Ready for next input";
            }
            
            Debug.Log($"Message sent successfully. AI Response: {response.response}");
        }
        catch (Exception e)
        {
            interviewStatus = $"Failed to send message: {e.Message}";
            Debug.LogError($"Failed to send message: {e}");
        }
    }
    
    private async Task EndInterviewSession()
    {
        try
        {
            interviewStatus = "Ending interview...";
            
            // Get final feedback if available
            try
            {
                var feedback = await apiManager.GetSessionFeedback(currentSessionId);
                conversationHistory.Add(new ConversationEntry("System", $"Final Feedback - Score: {feedback.overall_score}"));
                conversationHistory.Add(new ConversationEntry("System", $"Strengths: {feedback.strengths}"));
                conversationHistory.Add(new ConversationEntry("System", $"Areas for Improvement: {feedback.improvements}"));
            }
            catch (Exception feedbackError)
            {
                Debug.LogWarning($"Could not retrieve feedback: {feedbackError.Message}");
            }
            
            // Clear session data
            apiManager.ClearSessionData();
            currentSessionId = "";
            interviewStatus = "Interview ended - Ready to start new session";
            
            Debug.Log("Interview session ended successfully");
        }
        catch (Exception e)
        {
            interviewStatus = $"Error ending interview: {e.Message}";
            Debug.LogError($"Error ending interview: {e}");
        }
    }
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    [Button("Clear Conversation History")]
    [GUIColor(0.8f, 0.6f, 0.4f)]
    public void ClearConversationHistory()
    {
        conversationHistory.Clear();
        Debug.Log("Conversation history cleared");
    }
    
    [Button("Export Conversation")]
    [EnableIf("@conversationHistory.Count > 0")]
    public void ExportConversation()
    {
        string export = $"Interview Session Export\n";
        export += $"Candidate: {candidateName}\n";
        export += $"Position: {position}\n";
        export += $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        export += $"Messages: {conversationHistory.Count}\n\n";
        
        foreach (var entry in conversationHistory)
        {
            export += $"[{entry.timestamp}] {entry.speaker}: {entry.message}\n\n";
        }
        
        Debug.Log("Conversation Export:\n" + export);
        
        // You could also save this to a file here
        string fileName = $"Interview_{candidateName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        Debug.Log($"Export ready - suggested filename: {fileName}");
    }
    
    private void Start()
    {
        // Initialize
        if (sstManager == null)
            sstManager = FindObjectOfType<SSTManager>();
        
        if (apiManager == null)
            apiManager = FindObjectOfType<InterviewAPIManager>();
        
        if (sstManager == null || apiManager == null)
        {
            Debug.LogError("SST Manager or API Manager not found! Please assign them in the inspector.");
            interviewStatus = "Component references missing!";
        }
        else
        {
            interviewStatus = "Ready to start";
            Debug.Log("Interview Integration Manager initialized successfully");
        }
    }
    
    private void Update()
    {
        // Auto-update current session ID from API manager
        if (apiManager != null && !string.IsNullOrEmpty(apiManager.currentSessionId))
        {
            currentSessionId = apiManager.currentSessionId;
        }
    }
} 