using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Threading.Tasks;

public class InterviewConversationManager : SerializedMonoBehaviour
{
    [Title("Interview Conversation Manager")]
    [InfoBox("Orchestrates the complete interview conversation flow: Speech Recognition → API → Text-to-Speech")]
    
    // ========================================
    // COMPONENT REFERENCES
    // ========================================
    
    [TitleGroup("Component References")]
    [Required]
    [LabelText("SST Manager")]
    public SSTManager sstManager;
    
    [TitleGroup("Component References")]
    [Required]
    [LabelText("Interview API Manager")]
    public InterviewAPIManager apiManager;
    
    [TitleGroup("Component References")]
    [Required]
    [LabelText("TTS Manager")]
    [InfoBox("Dedicated Text-to-Speech manager for voice responses")]
    public TTSManager ttsManager;
    
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
    [LabelText("Interview Type")]
    [ValueDropdown("GetInterviewTypes")]
    public string interviewType = "general";
    
    [TitleGroup("Interview Settings")]
    [LabelText("Difficulty Level")]
    [ValueDropdown("GetDifficultyLevels")]
    public string difficultyLevel = "medium";
    
    [TitleGroup("Interview Settings")]
    [LabelText("Auto-start Conversation Mode")]
    [InfoBox("Automatically enable conversation mode in SSTManager when interview starts")]
    public bool autoStartConversationMode = true;
    
    [TitleGroup("Interview Settings")]
    [LabelText("Auto-speak AI Responses")]
    [InfoBox("Automatically convert AI responses to speech")]
    public bool autoSpeakResponses = true;
    
    [TitleGroup("Interview Settings")]
    [LabelText("Pause Listening During TTS")]
    [InfoBox("Prevents the system from listening to its own voice during AI responses")]
    public bool pauseListeningDuringTTS = true;
    
    [TitleGroup("Interview Settings")]
    [LabelText("TTS Pause Buffer (seconds)")]
    [InfoBox("Extra time to wait after TTS finishes before resuming listening")]
    [Range(0f, 5f)]
    public float ttsPauseBuffer = 1.0f;
    
    // ========================================
    // CONVERSATION DISPLAY
    // ========================================
    
    [TitleGroup("Conversation Display")]
    [LabelText("Conversation Text Display")]
    [InfoBox("Optional: TMP field to display the full conversation")]
    public TMP_Text conversationDisplay;
     
    [TitleGroup("Conversation Display")]
    [LabelText("Show Timestamps")]
    public bool showTimestamps = true;
    
    [TitleGroup("Conversation Display")]
    [LabelText("Max Conversation Lines")]
    [Range(10, 1000)]
    public int maxConversationLines = 100;
    
    // ========================================
    // INTERVIEW STATUS
    // ========================================
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Interview Active")]
    public bool isInterviewActive = false;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Current Session ID")]
    public string currentSessionId;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Status")]
    public string currentStatus = "Ready to start interview";
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Messages Exchanged")]
    public int messageCount = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("API Requests Sent")]
    public int apiRequestsSent = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("API Responses Received")]
    public int apiResponsesReceived = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Speech Events Processed")]
    public int speechEventsProcessed = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Duplicates Detected")]
    public int duplicatesDetected = 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("TTS Speaking Status")]
    public bool isTTSSpeaking = false;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Listening Paused for TTS")]
    public bool isListeningPausedForTTS = false;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Interview Ending")]
    [InfoBox("Interview is ending - waiting for final TTS to complete")]
    public bool isInterviewEndingStatus => isInterviewEnding;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("SST Listening Status")]
    public string sstListeningStatus => GetSSTListeningStatus();
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Recognition Text Length")]
    public int recognitionTextLength => sstManager != null && sstManager.recognitionAppendText != null ? sstManager.recognitionAppendText.text.Length : 0;
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Event Connection Status")]
    public string eventConnectionStatus => GetEventConnectionStatus();
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Request/Response Balance")]
    [InfoBox("Requests and responses should match (may lag slightly during processing)")]
    public string requestResponseBalance => $"Requests: {apiRequestsSent} | Responses: {apiResponsesReceived} | Difference: {apiRequestsSent - apiResponsesReceived}";
    
    [TitleGroup("Interview Status")]
    [ShowInInspector, ReadOnly]
    [LabelText("Interview Feedback/Summary")]
    [InfoBox("Stores the final feedback separately from conversation")]
    [TextArea(3, 10)]
    public string interviewSummary = "";
    
    // ========================================
    // INTERVIEW CONTROLS
    // ========================================
    
    [TitleGroup("Interview Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [EnableIf("@!isInterviewActive")]
    public async void StartInterview()
    {
        await StartInterviewSession();
    }
    
    [TitleGroup("Interview Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [EnableIf("@isInterviewActive")]
    public void EndInterview()
    {
        EndInterviewSession();
    }
    
    [TitleGroup("Interview Controls")]
    [Button("Clear Conversation")]
    [GUIColor(0.8f, 0.8f, 0.4f)]
    public void ClearConversation()
    {
        if (conversationDisplay != null)
        {
            conversationDisplay.text = "";
        }
        messageCount = 0;
    }
    
    [TitleGroup("Interview Controls")]
    [Button("Reset Counters")]
    [GUIColor(0.6f, 0.6f, 0.8f)]
    public void ResetCounters()
    {
        messageCount = 0;
        apiRequestsSent = 0;
        apiResponsesReceived = 0;
        speechEventsProcessed = 0;
        duplicatesDetected = 0;
        Debug.Log("[COUNTERS] All counters reset to zero");
    }
    
    [TitleGroup("Interview Controls")]
    [Button("Force Resume Listening")]
    [GUIColor(0.4f, 0.8f, 0.8f)]
    [EnableIf("@isListeningPausedForTTS")]
    public void ForceResumeListening()
    {
        StopTTSListeningControl();
        Debug.Log("[MANUAL OVERRIDE] Forcing resume of listening");
    }
    
    [TitleGroup("Interview Controls")]
    [Button("Force End Interview")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [EnableIf("@isInterviewEndingStatus")]
    public void ForceEndInterview()
    {
        Debug.Log("[MANUAL OVERRIDE] Force ending interview - skipping TTS wait");
        if (interviewEndingCoroutine != null)
        {
            StopCoroutine(interviewEndingCoroutine);
            interviewEndingCoroutine = null;
        }
        EndInterviewSession();
    }
    
    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    
    private List<string> conversationHistory = new List<string>();
    private bool waitingForAPIResponse = false;
    
    // Thread-safe speech recognition queue
    private Queue<string> pendingSpeechRecognition = new Queue<string>();
    private readonly object speechQueueLock = new object();
    
    // Duplicate detection
    private string lastProcessedMessage = "";
    private float lastProcessedTime = 0f;
    
    // TTS listening control
    private Coroutine ttsListeningControlCoroutine;
    
    // Interview ending control
    private bool isInterviewEnding = false;
    private Coroutine interviewEndingCoroutine;
    
    // ========================================
    // UNITY LIFECYCLE
    // ========================================
    
    private void Start()
    {
        InitializeManager();
    }
    
    private void Update()
    {
        // Process pending speech recognition on main thread
        ProcessPendingSpeechRecognition();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    // ========================================
    // INITIALIZATION
    // ========================================
    
    private void InitializeManager()
    {
        // Validate component references
        if (sstManager == null)
        {
            Debug.LogError("SSTManager reference is missing!");
            return;
        }
        
        if (apiManager == null)
        {
            Debug.LogError("InterviewAPIManager reference is missing!");
            return;
        }
        
        if (ttsManager == null)
        {
            Debug.LogError("TTSManager reference is missing!");
            return;
        }
        
        // Subscribe to events
        SubscribeToEvents();
        
        currentStatus = "Ready to start interview";
        Debug.Log("Interview Conversation Manager initialized successfully");
    }
    
    private void SubscribeToEvents()
    {
        // Subscribe to SST Manager events
        if (sstManager != null)
        {
            sstManager.OnSpeechRecognized.AddListener(OnSpeechRecognized);
        }
        
        // Subscribe to API Manager events
        if (apiManager != null)
        {
            apiManager.OnSessionStarted.AddListener(OnSessionStarted);
            apiManager.OnMessageReceived.AddListener(OnMessageReceived);
            apiManager.OnAPIError.AddListener(OnAPIError);
        }
    }
    
    private void UnsubscribeFromEvents()
    {
        // Unsubscribe from SST Manager events
        if (sstManager != null)
        {
            sstManager.OnSpeechRecognized.RemoveListener(OnSpeechRecognized);
        }
        
        // Unsubscribe from API Manager events
        if (apiManager != null)
        {
            apiManager.OnSessionStarted.RemoveListener(OnSessionStarted);
            apiManager.OnMessageReceived.RemoveListener(OnMessageReceived);
            apiManager.OnAPIError.RemoveListener(OnAPIError);
        }
    }
    
    // ========================================
    // INTERVIEW SESSION MANAGEMENT
    // ========================================
    
    public async Task StartInterviewSession()
    {
        try
        {
            currentStatus = "Starting interview session...";
            
            // Reset all counters and state for new interview
            messageCount = 0;
            apiRequestsSent = 0;
            apiResponsesReceived = 0;
            speechEventsProcessed = 0;
            duplicatesDetected = 0;
            isInterviewEnding = false;
            interviewSummary = "";
            Debug.Log("[INTERVIEW START] All counters and state reset for new interview session");
            
            // Start the API session
            var sessionResponse = await apiManager.StartInterviewSession(
                candidateName, 
                position, 
                interviewType, 
                difficultyLevel
            );
            
            currentSessionId = sessionResponse.session_id;
            isInterviewActive = true;
            
            // Add welcome message to conversation
            AddToConversation("AI Interviewer", sessionResponse.welcome_message);
            
            // Optionally speak the welcome message
            if (autoSpeakResponses && ttsManager != null)
            {
                SpeakText(sessionResponse.welcome_message);
            }
            
            // Start conversation mode in SST Manager
            if (autoStartConversationMode && sstManager != null)
            {
                sstManager.conversationModeEnabled = true;
                // Actually start the conversation mode (this triggers the listening)
                sstManager.StartConversationMode();
                currentStatus = "Interview active - Listening for your response...";
                Debug.Log("Conversation mode started - SSTManager should now be listening");
            }
            
            // Speech recognition will now be handled via events
            Debug.Log("Interview session started - waiting for speech recognition events");
            
            Debug.Log($"Interview session started successfully: {currentSessionId}");
        }
        catch (Exception e)
        {
            currentStatus = $"Failed to start interview: {e.Message}";
            Debug.LogError($"Failed to start interview session: {e.Message}");
        }
    }
    
    public void EndInterviewSession()
    {
        isInterviewActive = false;
        isInterviewEnding = false;
        currentSessionId = "";
        messageCount = 0;
        waitingForAPIResponse = false;
        
        // Stop interview ending coroutine if running
        if (interviewEndingCoroutine != null)
        {
            StopCoroutine(interviewEndingCoroutine);
            interviewEndingCoroutine = null;
        }
        
        // Stop TTS listening control
        StopTTSListeningControl();
        
        // Stop conversation mode
        if (sstManager != null)
        {
            sstManager.StopConversationMode();
            sstManager.conversationModeEnabled = false;
        }
        
        // Clear API session data
        if (apiManager != null)
        {
            apiManager.ClearSessionData();
        }
        
        currentStatus = "Interview ended";
        AddToConversation("System", "Interview session ended");
        
        // Show summary if available
        if (!string.IsNullOrEmpty(interviewSummary))
        {
            Debug.Log($"[INTERVIEW END] Interview completed with summary: {interviewSummary}");
            summarText.text = interviewSummary;
            summarText.transform.parent.GetComponent<CanvasGroup>().alpha = 1f; // <--- This is the magic line that makes the text visible>()
        }
        
        Debug.Log("Interview session ended");
    }

    public TMP_Text summarText;
    // ========================================
    // SPEECH RECOGNITION EVENT HANDLING
    // ========================================
    
    private void OnSpeechRecognized(string recognizedText)
    {
        // Queue speech recognition for main thread processing (thread-safe)
        if (!string.IsNullOrEmpty(recognizedText))
        {
            speechEventsProcessed++;
            lock (speechQueueLock)
            {
                pendingSpeechRecognition.Enqueue(recognizedText);
            }
            Debug.Log($"[SPEECH EVENT #{speechEventsProcessed}] Speech Recognition Event Queued: '{recognizedText}'");
        }
    }
    
    private void ProcessPendingSpeechRecognition()
    {
        // Process all queued speech recognition on the main thread
        lock (speechQueueLock)
        {
            while (pendingSpeechRecognition.Count > 0)
            {
                string recognizedText = pendingSpeechRecognition.Dequeue();
                
                // Only process if interview is active and not waiting for API response
                if (!isInterviewActive || waitingForAPIResponse || isInterviewEnding)
                {
                    Debug.Log($"Speech ignored - Interview Active: {isInterviewActive}, Waiting: {waitingForAPIResponse}, Ending: {isInterviewEnding}, Text: '{recognizedText}'");
                    continue;
                }
                
                // Check if listening is paused for TTS
                if (isListeningPausedForTTS)
                {
                    Debug.Log($"[TTS PAUSE] Speech ignored - Listening paused for TTS: '{recognizedText}'");
                    continue;
                }
                
                // Check for duplicate messages (within 2 seconds)
                float currentTime = Time.time;
                if (recognizedText == lastProcessedMessage && (currentTime - lastProcessedTime) < 2f)
                {
                    duplicatesDetected++;
                    Debug.Log($"[DUPLICATE #{duplicatesDetected}] Ignoring duplicate message within 2 seconds: '{recognizedText}' (Time diff: {currentTime - lastProcessedTime:F2}s)");
                    continue;
                }
                
                // Update duplicate detection tracking
                lastProcessedMessage = recognizedText;
                lastProcessedTime = currentTime;
                
                Debug.Log($"[PROCESSING] Speech Recognition: '{recognizedText}' as user message");
                
                // Process the recognized speech as a user message (safe on main thread)
                StartCoroutine(ProcessUserMessageCoroutine(recognizedText));
            }
        }
    }
    
    // ========================================
    // MESSAGE PROCESSING
    // ========================================
    
    public async Task ProcessUserMessage(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage) || !isInterviewActive || waitingForAPIResponse)
            return;
        
        try
        {
            waitingForAPIResponse = true;
            currentStatus = "Processing your response...";
            
            // Add user message to conversation
            AddToConversation("You", userMessage);
            
            // Send message to API
            await apiManager.SendChatMessage(userMessage, currentSessionId);
            
            messageCount++;
        }
        catch (Exception e)
        {
            currentStatus = $"Error processing message: {e.Message}";
            Debug.LogError($"Error processing user message: {e.Message}");
            waitingForAPIResponse = false;
        }
    }
    
    private IEnumerator ProcessUserMessageCoroutine(string userMessage)
    {
        if (string.IsNullOrEmpty(userMessage) || !isInterviewActive || waitingForAPIResponse)
        {
            Debug.Log($"[BLOCKED] User message blocked - Active: {isInterviewActive}, Waiting: {waitingForAPIResponse}, Message: '{userMessage}'");
            yield break;
        }
        
        waitingForAPIResponse = true;
        currentStatus = "Processing your response...";
        
        // Add user message to conversation
        AddToConversation("You", userMessage);
        
        // Increment API request counter
        apiRequestsSent++;
        Debug.Log($"[API REQUEST #{apiRequestsSent}] Sending to Interview API: '{userMessage}'");
        
        // Start the async API call
        var apiTask = apiManager.SendChatMessage(userMessage, currentSessionId);
        
        // Wait for the API task to complete
        while (!apiTask.IsCompleted)
        {
            yield return null;
        }
        
        try
        {
            // Get the result
            var result = apiTask.Result;
            messageCount++;
            Debug.Log($"[API SUCCESS] Request #{apiRequestsSent} completed successfully");
        }
        catch (Exception e)
        {
            currentStatus = $"Error processing message: {e.Message}";
            Debug.LogError($"[API ERROR] Request #{apiRequestsSent} failed: {e.Message}");
            waitingForAPIResponse = false;
        }
    }
    
    // ========================================
    // API EVENT HANDLERS
    // ========================================
    
    private void OnSessionStarted(StartSessionResponse response)
    {
        Debug.Log($"Session started event received: {response.session_id}");
    }
    
    private void OnMessageReceived(ChatMessageResponse response)
    {
        // Increment response counter
        apiResponsesReceived++;
        Debug.Log($"[API RESPONSE #{apiResponsesReceived}] Received from Interview API: '{response.response}' (Session Ended: {response.session_ended})");
        
        waitingForAPIResponse = false;
        
        // Add AI response to conversation
        AddToConversation("AI Interviewer", response.response);
        Debug.Log(response.response);
        // Check if session ended
        if (response.session_ended)
        {
            currentStatus = "Interview completed - Processing final response...";
            isInterviewEnding = true;
            
            // Store feedback separately (don't add to conversation)
            if (!string.IsNullOrEmpty(response.feedback))
            {
                interviewSummary = response.feedback;
                Debug.Log($"[INTERVIEW END] Feedback stored separately: {response.feedback}");
            }
            
            // Start the interview ending process with TTS wait
            if (interviewEndingCoroutine != null)
                StopCoroutine(interviewEndingCoroutine);
            interviewEndingCoroutine = StartCoroutine(HandleInterviewEnding(response.response));
            return;
        }
        
        // Speak the AI response
        if (autoSpeakResponses && ttsManager != null)
        {
            Debug.Log($"[TTS] Speaking AI response: '{response.response}'");
            SpeakText(response.response);
        }
        
        currentStatus = "Listening for your response...";
    }
    
    private void OnAPIError(APIError error)
    {
        waitingForAPIResponse = false;
        currentStatus = $"API Error: {error.message}";
        AddToConversation("Error", $"API Error: {error.message}");
        Debug.LogError($"API Error: {error.message} (Code: {error.status_code})");
    }
    
    // ========================================
    // TEXT-TO-SPEECH
    // ========================================
    
    private void SpeakText(string text)
    {
        if (ttsManager != null && ttsManager.ttsEnabled)
        {
            try
            {
                // Pause listening if enabled
                if (pauseListeningDuringTTS)
                {
                    StartTTSListeningControl(text);
                }
                
                // Use the dedicated TTS manager
                ttsManager.SpeakTextImmediate(text);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error speaking text: {e.Message}");
            }
        }
    }
    
    // ========================================
    // INTERVIEW ENDING CONTROL
    // ========================================
    
    private IEnumerator HandleInterviewEnding(string finalResponse)
    {
        Debug.Log($"[INTERVIEW END] Starting interview ending process with final response: '{finalResponse}'");
        
        // Speak the final AI response if TTS is enabled
        if (autoSpeakResponses && ttsManager != null && ttsManager.ttsEnabled)
        {
            Debug.Log("[INTERVIEW END] Speaking final response...");
            SpeakText(finalResponse);
            
            // Calculate estimated TTS duration for the final response
            string[] words = finalResponse.Split(' ');
            float estimatedDuration = (words.Length / 150f) * 60f; // 150 words per minute
            float minDuration = 3f; // Minimum wait time for final response
            float waitDuration = Mathf.Max(estimatedDuration, minDuration) + 2f; // Extra buffer for final response
            
            Debug.Log($"[INTERVIEW END] Waiting {waitDuration:F1}s for final TTS to complete");
            currentStatus = $"Interview ending - Speaking final response... ({waitDuration:F0}s remaining)";
            
            // Wait for TTS to complete
            float remainingTime = waitDuration;
            while (remainingTime > 0)
            {
                currentStatus = $"Interview ending - Speaking final response... ({remainingTime:F0}s remaining)";
                yield return new WaitForSeconds(1f);
                remainingTime -= 1f;
            }
        }
        else
        {
            Debug.Log("[INTERVIEW END] TTS disabled, ending interview immediately");
            yield return new WaitForSeconds(1f); // Brief pause for user to read
        }
        
        // Now end the interview
        Debug.Log("[INTERVIEW END] TTS completed, ending interview session");
        currentStatus = "Interview completed - Thank you!";
        EndInterviewSession();
        
        interviewEndingCoroutine = null;
    }
    
    // ========================================
    // TTS LISTENING CONTROL
    // ========================================
    
    private void StartTTSListeningControl(string textToSpeak)
    {
        // Stop any existing TTS control coroutine
        if (ttsListeningControlCoroutine != null)
        {
            StopCoroutine(ttsListeningControlCoroutine);
        }
        
        // Start new TTS control coroutine
        ttsListeningControlCoroutine = StartCoroutine(ManageTTSListening(textToSpeak));
    }
    
    private IEnumerator ManageTTSListening(string textToSpeak)
    {
        // Pause conversation mode listening
        isTTSSpeaking = true;
        isListeningPausedForTTS = true;
        
        if (sstManager != null && sstManager.conversationModeEnabled)
        {
            sstManager.StopConversationMode();
            Debug.Log($"[TTS CONTROL] Paused listening for TTS: '{textToSpeak}'");
        }
        
        // Calculate estimated speech duration (rough estimate: 150 words per minute)
        string[] words = textToSpeak.Split(' ');
        float estimatedDuration = (words.Length / 150f) * 60f; // Convert to seconds
        float minDuration = 2f; // Minimum pause duration
        float totalPauseDuration = Mathf.Max(estimatedDuration, minDuration) + ttsPauseBuffer;
        
        Debug.Log($"[TTS CONTROL] Estimated TTS duration: {estimatedDuration:F1}s, Total pause: {totalPauseDuration:F1}s (including {ttsPauseBuffer:F1}s buffer)");
        
        // Wait for estimated TTS duration plus buffer
        yield return new WaitForSeconds(totalPauseDuration);
        
        // Resume conversation mode listening
        isTTSSpeaking = false;
        isListeningPausedForTTS = false;
        
        if (sstManager != null && sstManager.conversationModeEnabled && isInterviewActive)
        {
            sstManager.StartConversationMode();
            Debug.Log("[TTS CONTROL] Resumed listening after TTS completed");
        }
        
        ttsListeningControlCoroutine = null;
    }
    
    private void StopTTSListeningControl()
    {
        if (ttsListeningControlCoroutine != null)
        {
            StopCoroutine(ttsListeningControlCoroutine);
            ttsListeningControlCoroutine = null;
        }
        
        // Ensure listening is resumed
        isTTSSpeaking = false;
        isListeningPausedForTTS = false;
        
        if (sstManager != null && sstManager.conversationModeEnabled && isInterviewActive)
        {
            sstManager.StartConversationMode();
            Debug.Log("[TTS CONTROL] Force resumed listening");
        }
    }
    
    // ========================================
    // CONVERSATION DISPLAY
    // ========================================
    public int conversation;
    private void AddToConversation(string sender, string message)
    {
        conversation++;
        string timestamp = showTimestamps ? $"[{DateTime.Now:HH:mm:ss}] " : "";
        string formattedMessage = $"{timestamp}{sender}: {message}";
        
        conversationHistory.Add(formattedMessage);
        
        // Limit conversation history
        if (conversationHistory.Count > maxConversationLines)
        {
            conversationHistory.RemoveAt(0);
        }
        
        // Update display
        if (conversationDisplay != null)
        {
            conversationDisplay.text = string.Join("\n", conversationHistory);
        }
        
        Debug.Log($"Conversation: {formattedMessage}");
    }
    
    // ========================================
    // DROPDOWN VALUE PROVIDERS
    // ========================================
    
    private static IEnumerable<string> GetInterviewTypes()
    {
        return new[] { "general", "technical", "behavioral", "case_study" };
    }
    
    private static IEnumerable<string> GetDifficultyLevels()
    {
        return new[] { "easy", "medium", "hard", "expert" };
    }
    
    // ========================================
    // PUBLIC METHODS
    // ========================================
    
    /// <summary>
    /// Manually send a message (for testing or UI integration)
    /// </summary>
    public async Task SendMessage(string message)
    {
        if (isInterviewActive)
        {
            await ProcessUserMessage(message);
        }
    }
    
    /// <summary>
    /// Get the current conversation as a formatted string
    /// </summary>
    public string GetConversationHistory()
    {
        return string.Join("\n", conversationHistory);
    }
    
    /// <summary>
    /// Check if the manager is ready to start an interview
    /// </summary>
    public bool IsReadyToStart()
    {
        return sstManager != null && apiManager != null && ttsManager != null && !isInterviewActive;
    }
    
    private string GetSSTListeningStatus()
    {
        if (sstManager == null) return "SST Manager not assigned";
        
        string status = "";
        status += $"Conv Mode: {(sstManager.conversationModeEnabled ? "✅" : "❌")} | ";
        status += $"Active: {(sstManager.isConversationActive ? "✅" : "❌")} | ";
        status += $"Listening: {(sstManager.isListeningForVoice ? "✅" : "❌")} | ";
        status += $"Voice: {(sstManager.isVoiceDetected ? "✅" : "❌")}";
        
        return status;
    }
    
    private string GetEventConnectionStatus()
    {
        string status = "";
        
        if (sstManager != null)
        {
            int listenerCount = sstManager.OnSpeechRecognized.GetPersistentEventCount();
            status += $"SST Events: {(listenerCount > 0 ? "✅" : "❌")} ({listenerCount}) | ";
        }
        else
        {
            status += "SST Events: ❌ (No SST Manager) | ";
        }
        
        if (apiManager != null)
        {
            status += "API Events: ✅ | ";
        }
        else
        {
            status += "API Events: ❌ | ";
        }
        
        if (ttsManager != null)
        {
            status += "TTS: ✅";
        }
        else
        {
            status += "TTS: ❌";
        }
        
        return status;
    }
} 