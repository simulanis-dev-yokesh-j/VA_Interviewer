using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Sirenix.OdinInspector;

[System.Serializable]
public class StartSessionRequest
{
    public string candidate_name;
    public string position;
    public string interview_type = "general";
    public string difficulty_level = "medium";
}

[System.Serializable]
public class StartSessionResponse
{
    public string session_id;
    public string welcome_message;
    public string status;
}

[System.Serializable]
public class ChatMessageRequest
{
    public string message;
    public string session_id;
}

[System.Serializable]
public class ChatMessageResponse
{
    public string response;
    public bool session_ended;
    public string feedback;
}

[System.Serializable]
public class SessionInfo
{
    public string session_id;
    public string candidate_name;
    public string position;
    public string status;
    public string start_time;
    public string end_time;
    public int message_count;
}

[System.Serializable]
public class SessionMessage
{
    public string sender;
    public string message;
    public string timestamp;
}

[System.Serializable]
public class SessionFeedback
{
    public string overall_score;
    public string strengths;
    public string improvements;
    public string detailed_feedback;
}

[System.Serializable]
public class APIError
{
    public string message;
    public int status_code;
}

public class InterviewAPIManager : SerializedMonoBehaviour
{
    [Title("Interview Chatbot API Manager")]
    [InfoBox("Manages communication with the Interview Chatbot API. Configure your API base URL below.")]
    
    // ========================================
    // API CONFIGURATION
    // ========================================
    
    [TitleGroup("API Configuration")]
    [LabelText("Base API URL")]
    public string baseURL = "https://interview-chat-dybpgtgjd8h8ddba.centralindia-01.azurewebsites.net";
    
    [TitleGroup("API Configuration")]
    [LabelText("Request Timeout (seconds)")]
    [Range(5, 60)]
    public int requestTimeout = 30;
    
    // ========================================
    // CURRENT SESSION DATA
    // ========================================
    
    [TitleGroup("Current Session")]
    [ShowInInspector, ReadOnly]
    [LabelText("Session ID")]
    public string currentSessionId;
    
    [TitleGroup("Current Session")]
    [ShowInInspector, ReadOnly]
    [LabelText("Session Status")]
    public string sessionStatus = "No active session";
    
    [TitleGroup("Current Session")]
    [ShowInInspector, ReadOnly]
    [LabelText("Last Response")]
    [TextArea(3, 8)]
    public string lastResponse;
    
    // ========================================
    // TEST CONTROLS
    // ========================================
    
    [TitleGroup("Test Controls")]
    [LabelText("Test Candidate Name")]
    public string testCandidateName = "John Doe";
    
    [TitleGroup("Test Controls")]
    [LabelText("Test Position")]
    public string testPosition = "Software Developer";
    
    [TitleGroup("Test Controls")]
    [LabelText("Test Message")]
    [TextArea(2, 5)]
    public string testMessage = "Hello, I'm ready for the interview.";
    
    [TitleGroup("Test Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    public async void TestStartSession()
    {
        await StartInterviewSession(testCandidateName, testPosition);
    }
    
    [TitleGroup("Test Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.4f, 0.8f)]
    [EnableIf("@!string.IsNullOrEmpty(currentSessionId)")]
    public async void TestSendMessage()
    {
        if (!string.IsNullOrEmpty(currentSessionId))
        {
            await SendChatMessage(testMessage, currentSessionId);
        }
    }
    
    [TitleGroup("Test Controls")]
    [Button("Get Session Info")]
    [EnableIf("@!string.IsNullOrEmpty(currentSessionId)")]
    public async void TestGetSessionInfo()
    {
        if (!string.IsNullOrEmpty(currentSessionId))
        {
            await GetSessionInfo(currentSessionId);
        }
    }
    
    // ========================================
    // API EVENTS
    // ========================================
    
    [System.Serializable]
    public class SessionStartedEvent : UnityEngine.Events.UnityEvent<StartSessionResponse> { }
    
    [System.Serializable]
    public class MessageReceivedEvent : UnityEngine.Events.UnityEvent<ChatMessageResponse> { }
    
    [System.Serializable]
    public class APIErrorEvent : UnityEngine.Events.UnityEvent<APIError> { }
    
    [FoldoutGroup("Events")]
    public SessionStartedEvent OnSessionStarted = new SessionStartedEvent();
    
    [FoldoutGroup("Events")]
    public MessageReceivedEvent OnMessageReceived = new MessageReceivedEvent();
    
    [FoldoutGroup("Events")]
    public APIErrorEvent OnAPIError = new APIErrorEvent();
    
    // ========================================
    // API METHODS
    // ========================================
    
    /// <summary>
    /// Start a new interview session
    /// </summary>
    public async Task<StartSessionResponse> StartInterviewSession(string candidateName, string position, string interviewType = "general", string difficultyLevel = "medium")
    {
        sessionStatus = "Starting session...";
        
        var request = new StartSessionRequest
        {
            candidate_name = candidateName,
            position = position,
            interview_type = interviewType,
            difficulty_level = difficultyLevel
        };
        
        try
        {
            string endpoint = "/api/sessions/start";
            string response = await PostRequest(endpoint, request);
            
            var sessionResponse = JsonConvert.DeserializeObject<StartSessionResponse>(response);
            
            // Update current session data
            currentSessionId = sessionResponse.session_id;
            sessionStatus = $"Active - {sessionResponse.status}";
            lastResponse = sessionResponse.welcome_message;
            
            Debug.Log($"Session started successfully: {sessionResponse.session_id}");
            OnSessionStarted.Invoke(sessionResponse);
            
            return sessionResponse;
        }
        catch (Exception e)
        {
            var error = new APIError { message = e.Message, status_code = 500 };
            sessionStatus = $"Error: {e.Message}";
            OnAPIError.Invoke(error);
            throw;
        }
    }
    
    /// <summary>
    /// Send a chat message to the AI
    /// </summary>
    public async Task<ChatMessageResponse> SendChatMessage(string message, string sessionId)
    {
        sessionStatus = "Sending message...";
        
        var request = new ChatMessageRequest
        {
            message = message,
            session_id = sessionId
        };
        
        try
        {
            string endpoint = "/api/chat/send";
            string response = await PostRequest(endpoint, request);
            
            var chatResponse = JsonConvert.DeserializeObject<ChatMessageResponse>(response);
            
            // Update session status
            lastResponse = chatResponse.response;
            sessionStatus = chatResponse.session_ended ? "Session ended" : "Active";
            
            Debug.Log($"Message sent successfully. Response: {chatResponse.response}");
            OnMessageReceived.Invoke(chatResponse);
            
            return chatResponse;
        }
        catch (Exception e)
        {
            var error = new APIError { message = e.Message, status_code = 500 };
            sessionStatus = $"Error: {e.Message}";
            OnAPIError.Invoke(error);
            throw;
        }
    }
    
    /// <summary>
    /// Get session information
    /// </summary>
    public async Task<SessionInfo> GetSessionInfo(string sessionId)
    {
        try
        {
            string endpoint = $"/api/sessions/{sessionId}";
            string response = await GetRequest(endpoint);
            
            var sessionInfo = JsonConvert.DeserializeObject<SessionInfo>(response);
            
            Debug.Log($"Session info retrieved: {sessionInfo.candidate_name} - {sessionInfo.status}");
            return sessionInfo;
        }
        catch (Exception e)
        {
            var error = new APIError { message = e.Message, status_code = 500 };
            OnAPIError.Invoke(error);
            throw;
        }
    }
    
    /// <summary>
    /// Get all messages for a session
    /// </summary>
    public async Task<List<SessionMessage>> GetSessionMessages(string sessionId)
    {
        try
        {
            string endpoint = $"/api/sessions/{sessionId}/messages";
            string response = await GetRequest(endpoint);
            
            var messages = JsonConvert.DeserializeObject<List<SessionMessage>>(response);
            
            Debug.Log($"Retrieved {messages.Count} messages for session {sessionId}");
            return messages;
        }
        catch (Exception e)
        {
            var error = new APIError { message = e.Message, status_code = 500 };
            OnAPIError.Invoke(error);
            throw;
        }
    }
    
    /// <summary>
    /// Get feedback for a completed session
    /// </summary>
    public async Task<SessionFeedback> GetSessionFeedback(string sessionId)
    {
        try
        {
            string endpoint = $"/api/sessions/{sessionId}/feedback";
            string response = await GetRequest(endpoint);
            
            var feedback = JsonConvert.DeserializeObject<SessionFeedback>(response);
            
            Debug.Log($"Feedback retrieved for session {sessionId}");
            return feedback;
        }
        catch (Exception e)
        {
            var error = new APIError { message = e.Message, status_code = 500 };
            OnAPIError.Invoke(error);
            throw;
        }
    }
    
    // ========================================
    // HTTP REQUEST HELPERS
    // ========================================
    
    private async Task<string> PostRequest<T>(string endpoint, T data)
    {
        string url = baseURL + endpoint;
        string jsonData = JsonConvert.SerializeObject(data);
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = requestTimeout;
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"POST {endpoint} - Success: {request.responseCode}");
                return request.downloadHandler.text;
            }
            else
            {
                string errorMessage = $"POST {endpoint} failed: {request.error} (Code: {request.responseCode})";
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    errorMessage += $" Response: {request.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
    
    private async Task<string> GetRequest(string endpoint)
    {
        string url = baseURL + endpoint;
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = requestTimeout;
            
            var operation = request.SendWebRequest();
            
            while (!operation.isDone)
            {
                await Task.Yield();
            }
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"GET {endpoint} - Success: {request.responseCode}");
                return request.downloadHandler.text;
            }
            else
            {
                string errorMessage = $"GET {endpoint} failed: {request.error} (Code: {request.responseCode})";
                if (!string.IsNullOrEmpty(request.downloadHandler.text))
                {
                    errorMessage += $" Response: {request.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
    
    // ========================================
    // UTILITY METHODS
    // ========================================
    
    /// <summary>
    /// Clear current session data
    /// </summary>
    [Button("Clear Session Data")]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    public void ClearSessionData()
    {
        currentSessionId = "";
        sessionStatus = "No active session";
        lastResponse = "";
        Debug.Log("Session data cleared");
    }
    
    /// <summary>
    /// Test API connectivity
    /// </summary>
    [Button("Test API Connectivity")]
    [GUIColor(0.8f, 0.8f, 0.4f)]
    public async void TestAPIConnectivity()
    {
        try
        {
            sessionStatus = "Testing connectivity...";
            string response = await GetRequest("/");
            sessionStatus = "API is reachable";
            Debug.Log($"API Test Response: {response}");
        }
        catch (Exception e)
        {
            sessionStatus = $"API unreachable: {e.Message}";
            Debug.LogError($"API connectivity test failed: {e.Message}");
        }
    }
    
    /// <summary>
    /// Check if there's an active session
    /// </summary>
    public bool HasActiveSession()
    {
        return !string.IsNullOrEmpty(currentSessionId) && sessionStatus.Contains("Active");
    }
    
    private void Start()
    {
        // Initialize the API manager
        sessionStatus = "Ready";
        Debug.Log($"Interview API Manager initialized. Base URL: {baseURL}");
    }
} 