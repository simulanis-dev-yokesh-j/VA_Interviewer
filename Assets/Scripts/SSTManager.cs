using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;
using Sirenix.OdinInspector;

#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif
#if PLATFORM_IOS
using UnityEngine.iOS;
using System.Collections;
#endif

public class SSTManager : SerializedMonoBehaviour
{
    [Title("Speech-to-Text Manager with Odin Inspector Testing")]
    [InfoBox("This script allows you to test microphone recording and Azure Speech recognition directly from the Inspector. No UI setup required for testing.")]
    
    // ========================================
    // MICROPHONE SETTINGS
    // ========================================
    
    [TitleGroup("Microphone Settings")]
    [ShowInInspector, ReadOnly]
    [LabelText("Available Microphones")]
    public List<string> availableMicrophones = new List<string>();
    
    [TitleGroup("Microphone Settings")]
    [ValueDropdown("availableMicrophones")]
    [LabelText("Selected Microphone")]
    [OnValueChanged("OnMicrophoneChanged")]
    public string selectedMicrophone;
    
    [TitleGroup("Microphone Settings")]
    [LabelText("Preferred Default Microphone")]
    [InfoBox("Set your preferred microphone here. It will be automatically selected when available.")]
    [ValueDropdown("availableMicrophones")]
    [OnValueChanged("OnPreferredMicrophoneChanged")]
    public string preferredDefaultMicrophone;
    
    [TitleGroup("Microphone Settings")]
    [Button("Save as Default")]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [EnableIf("@!string.IsNullOrEmpty(selectedMicrophone)")]
    public void SaveCurrentMicrophoneAsDefault()
    {
        preferredDefaultMicrophone = selectedMicrophone;
        PlayerPrefs.SetString("PreferredMicrophone", preferredDefaultMicrophone);
        PlayerPrefs.Save();
        Debug.Log($"Saved '{preferredDefaultMicrophone}' as default microphone");
    }
    
    [TitleGroup("Microphone Settings")]
    [Button("Clear Default")]
    [GUIColor(0.8f, 0.6f, 0.4f)]
    [EnableIf("@!string.IsNullOrEmpty(preferredDefaultMicrophone)")]
    public void ClearDefaultMicrophone()
    {
        preferredDefaultMicrophone = "";
        PlayerPrefs.DeleteKey("PreferredMicrophone");
        PlayerPrefs.Save();
        Debug.Log("Cleared default microphone preference");
    }
    
    [TitleGroup("Microphone Settings")]
    [ShowInInspector, ReadOnly]
    [LabelText("Microphone Status")]
    public string microphoneStatus = "Initializing...";
    
    [TitleGroup("Microphone Settings")]
    [Range(8000, 48000)]
    [LabelText("Sample Rate (Hz)")]
    public int sampleRate = 16000; // Azure Speech Service preferred sample rate
    
    [TitleGroup("Microphone Settings")]
    [Range(5, 60)]
    [LabelText("Max Recording Length (seconds)")]
    public int recordingLength = 15;
    
    // ========================================
    // CONVERSATION MODE SETTINGS
    // ========================================
    
    [TitleGroup("Conversation Mode")]
    [InfoBox("Dynamic conversation mode allows for natural speech interaction with voice activity detection.")]
    [LabelText("Enable Conversation Mode")]
    [OnValueChanged("OnConversationModeChanged")]
    public bool conversationModeEnabled = false;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [Range(0.01f, 0.5f)]
    [LabelText("Voice Activity Threshold")]
    [InfoBox("Minimum volume level to trigger voice detection. Lower = more sensitive.")]
    public float voiceThreshold = 0.02f;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [Range(0.5f, 5.0f)]
    [LabelText("Silence Duration (seconds)")]
    [InfoBox("How long to wait in silence before stopping recording.")]
    public float silenceDuration = 2.0f;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [Range(0.01f, 3.0f)]
    [LabelText("Voice Start Delay (seconds)")]
    [InfoBox("How long voice must be detected before starting to record.")]
    public float voiceStartDelay = 0.5f;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Auto-send to Azure")]
    [InfoBox("Automatically send completed recordings to Azure Speech Service.")]
    public bool autoSendToAzure = true;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Continuous Listening")]
    [InfoBox("Keep listening for new speech after processing current input.")]
    public bool continuousListening = true;
    
    [TitleGroup("Conversation Mode")]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Enable Debug Logging")]
    [InfoBox("Show detailed debug information about voice detection in console.")]
    public bool enableDebugLogging = true;
    
    [TitleGroup("Quick Tuning")]
    [ShowIf("conversationModeEnabled")]
    [HorizontalGroup("Quick Tuning/Threshold")]
    [Button("-", ButtonSizes.Small)]
    public void DecreaseThreshold()
    {
        voiceThreshold = Mathf.Max(0.01f, voiceThreshold - 0.01f);
        Debug.Log($"Voice threshold decreased to: {voiceThreshold:F3}");
    }
    
    [HorizontalGroup("Quick Tuning/Threshold")]
    [Button("+", ButtonSizes.Small)]
    public void IncreaseThreshold()
    {
        voiceThreshold = Mathf.Min(0.5f, voiceThreshold + 0.01f);
        Debug.Log($"Voice threshold increased to: {voiceThreshold:F3}");
    }
    
    [TitleGroup("Quick Tuning")]
    [ShowIf("conversationModeEnabled")]
    [HorizontalGroup("Quick Tuning/Silence")]
    [Button("Shorter Silence", ButtonSizes.Medium)]
    public void ShorterSilence()
    {
        silenceDuration = Mathf.Max(0.5f, silenceDuration - 0.5f);
        Debug.Log($"Silence duration decreased to: {silenceDuration:F1}s");
    }
    
    [HorizontalGroup("Quick Tuning/Silence")]
    [Button("Longer Silence", ButtonSizes.Medium)]
    public void LongerSilence()
    {
        silenceDuration = Mathf.Min(5.0f, silenceDuration + 0.5f);
        Debug.Log($"Silence duration increased to: {silenceDuration:F1}s");
    }
    
    [TitleGroup("Manual Testing")]
    [ShowIf("conversationModeEnabled")]
    [Button("🧪 Force Stop Recording", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.4f, 0.8f)]
    [EnableIf("isRecording")]
    public void ForceStopRecording()
    {
        Debug.Log("🧪 MANUAL TEST: Force stopping recording...");
        StopSpeechRecording();
    }
    
    [TitleGroup("Manual Testing")]
    [ShowIf("conversationModeEnabled")]
    [Button("🧪 Test Silence Detection", ButtonSizes.Medium)]
    [GUIColor(0.6f, 0.8f, 0.4f)]
    public void TestSilenceDetection()
    {
        Debug.Log($"🧪 SILENCE TEST: Current voice: {voiceActivity:F3}, Threshold: {voiceThreshold:F3}, Is Silent: {voiceActivity < voiceThreshold}");
        Debug.Log($"🧪 SILENCE TEST: Silence timer: {silenceTimer:F1}s, Duration needed: {silenceDuration:F1}s");
        Debug.Log($"🧪 SILENCE TEST: Is recording: {isRecording}, Is conversation active: {isConversationActive}");
        Debug.Log($"🧪 SILENCE TEST: Microphone position: {(string.IsNullOrEmpty(selectedMicrophone) ? "No mic" : Microphone.GetPosition(selectedMicrophone).ToString())}");
    }
    

    
    // ========================================
    // RECORDING CONTROLS
    // ========================================
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [ShowIf("@!conversationModeEnabled && !isRecording && micPermissionGranted")]
    public void StartRecording()
    {
        StartRecordingInternal();
    }
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [ShowIf("@!conversationModeEnabled && isRecording")]
    public void StopRecording()
    {
        StopRecordingInternal();
    }
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.2f, 0.8f, 0.2f)]
    [ShowIf("@conversationModeEnabled && !isConversationActive && micPermissionGranted")]
    public void StartConversationMode()
    {
        StartConversationModeInternal();
    }
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.2f, 0.2f)]
    [ShowIf("@conversationModeEnabled && isConversationActive")]
    public void StopConversationMode()
    {
        StopConversationModeInternal();
    }
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, 1, ColorMember = "GetVolumeColor")]
    [LabelText("Recording Volume")]
    public float currentVolume;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [LabelText("Recording Status")]
    public string recordingStatus = "Ready to record";
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Conversation Status")]
    public string conversationStatus = "Conversation mode ready";
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [ProgressBar(0, 1, ColorMember = "GetVoiceActivityColor")]
    [LabelText("Voice Activity")]
    public float voiceActivity;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Voice Detection Debug")]
    public string voiceDetectionDebug = "";
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Voice Detected")]
    public bool voiceDetectedStatus => isVoiceDetected;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Voice Timer")]
    public float voiceTimerDisplay => voiceDetectionTimer;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Silence Timer")]
    public float silenceTimerDisplay => silenceTimer;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Is Silent (Below Threshold)")]
    public bool isSilent => voiceActivity < voiceThreshold;
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Silence Detection Debug")]
    public string silenceDetectionDebug = "";
    
    [TitleGroup("Recording Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Recording Duration")]
    public float recordingDurationDisplay => GetCurrentRecordingDuration();
    
    // ========================================
    // PLAYBACK CONTROLS
    // ========================================
    
    [TitleGroup("Playback Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.4f, 0.8f)]
    [EnableIf("@recordedClip != null && !isRecording")]
    public void PlayRecordedAudio()
    {
        PlayRecordedAudioInternal();
    }
    
    [TitleGroup("Playback Controls")]
    [ShowInInspector, ReadOnly]
    [ShowIf("@recordedClip != null")]
    [LabelText("Recorded Audio Length")]
    public string recordedAudioInfo => recordedClip != null ? $"{recordedClip.length:F2}s" : "No recording";
    
    // ========================================
    // AZURE SPEECH SERVICE
    // ========================================
    
    [TitleGroup("Azure Speech Service")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.6f, 0.2f)]
    [EnableIf("@recordedClip != null && !isRecording && !waitingForReco")]
    public void SendToAzureSpeechService()
    {
        _ = SendToAzureInternal(); // Fire and forget for UI button
    }
    
    [TitleGroup("Azure Speech Service")]
    [ShowInInspector, ReadOnly]
    [TextArea(3, 10)]
    [LabelText("Recognition Result")]
    public string recognitionResult = "No recognition performed yet";
    
    [TitleGroup("Azure Speech Service")]
    [ShowInInspector, ReadOnly]
    [LabelText("Azure Status")]
    public string azureStatus = "Ready";
    
    
    // ========================================
    // DEBUG INFORMATION
    // ========================================
    
    [TitleGroup("Debug Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Permission Status")]
    public string permissionStatus => micPermissionGranted ? "✅ Granted" : "❌ Not Granted";
    
    [TitleGroup("Debug Information")]
    [ShowInInspector, ReadOnly]
    [LabelText("Current State")]
    public string currentState => GetCurrentState();
    
    [TitleGroup("Debug Information")]
    [Button("Refresh Microphone List")]
    public void RefreshMicrophoneList()
    {
        RefreshMicrophoneListInternal();
    }
    
    [TitleGroup("Debug Information")]
    [Button("Test Microphone Permissions")]
    public void TestMicrophonePermissions()
    {
        CheckMicrophonePermission();
    }
    
    // ========================================
    // EVENTS AND CALLBACKS
    // ========================================
    
    [System.Serializable]
    public class SpeechRecognizedEvent : UnityEngine.Events.UnityEvent<string> { }
    
    [TitleGroup("Events")]
    [InfoBox("Event triggered when speech is successfully recognized. Subscribe to this for real-time speech processing.")]
    public SpeechRecognizedEvent OnSpeechRecognized = new SpeechRecognizedEvent();
    
    // ========================================
    // ESSENTIAL COMPONENTS
    // ========================================
    
    [TitleGroup("Text Output")]
    [InfoBox("TMP Text field to append all recognition results. Creates a running conversation log.")]
    public TextMeshProUGUI recognitionAppendText;
    
    [TitleGroup("Audio Component")]
    [InfoBox("AudioSource for audio playback. Will be auto-created if not assigned.")]
    public AudioSource audioSource;
    
    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    
    private object threadLocker = new object();
    public bool waitingForReco;
    private bool isRecording;
    private bool micPermissionGranted = false;
    
    // Audio recording variables
    private AudioClip recordedClip;
    private float[] recordingData;
    private int recordingPosition;
    
    // Conversation mode variables
    public bool isConversationActive = false;
    public bool isListeningForVoice = false;
    public bool isVoiceDetected = false;
    private float voiceDetectionTimer = 0f;
    private float silenceTimer = 0f;
    private float lastVoiceLevel = 0f;
    private AudioClip listeningClip;
    private List<string> conversationHistory = new List<string>();
    
    // Conversation clips storage
    private List<AudioClip> conversationClips = new List<AudioClip>();
    private List<string> conversationClipTimestamps = new List<string>();
    private List<string> conversationRecognitionResults = new List<string>();
    
    // Azure Speech Config
    private readonly string subscriptionKey = "9v2mwusnyHO6LBsDWvcc6xh6M9WsiJIuqEbwe95kHTABg7bAiEs3JQQJ99BFACGhslBXJ3w3AAAYACOGOxq2";
    private readonly string region = "centralindia";
    

    
    // Thread-safe UI update queue
    private Queue<string> pendingRecognitionTexts = new Queue<string>();
    private readonly object uiUpdateLock = new object();
    


#if PLATFORM_ANDROID || PLATFORM_IOS
    private Microphone mic;
#endif

    void Start()
    {
        InitializeComponents();
        CheckMicrophonePermission();
        RefreshMicrophoneListInternal();
        InitializeAudioSource();
    }

    void Update()
    {
        UpdateMicrophonePermission();
        UpdateOdinInspectorValues();
        
        // Handle conversation mode
        if (conversationModeEnabled && isConversationActive)
        {
            UpdateConversationMode();
        }
        
        // Process pending UI updates on main thread
        ProcessPendingUIUpdates();
    }
    
    // ========================================
    // ODIN INSPECTOR HELPER METHODS
    // ========================================

    private Color GetVolumeColor()
    {
        if (currentVolume < 0.3f) return Color.green;
        if (currentVolume < 0.7f) return Color.yellow;
        return Color.red;
    }
    
    private Color GetVoiceActivityColor()
    {
        if (voiceActivity < voiceThreshold) return Color.gray;
        if (voiceActivity < voiceThreshold * 2) return Color.yellow;
        return Color.green;
    }
    
    private string GetCurrentState()
    {
        if (waitingForReco) return "🔄 Processing Azure Request";
        if (conversationModeEnabled && isConversationActive)
        {
            if (isRecording) return "🎤 Recording Speech";
            if (isListeningForVoice) return "👂 Listening for Voice";
            return "💬 Conversation Active";
        }
        if (isRecording) return "🎤 Recording";
        if (recordedClip != null) return "✅ Audio Ready";
        return "⏸️ Idle";
    }
    
    private void UpdateOdinInspectorValues()
    {
        // Update real-time values for Odin Inspector
        if (isRecording)
        {
            currentVolume = GetMicrophoneLevel();
            recordingStatus = $"Recording... {GetRecordingDuration():F1}s";
        }
        else
        {
            currentVolume = 0f;
            recordingStatus = recordedClip != null ? $"Ready - {recordedClip.length:F1}s recorded" : "Ready to record";
        }
        
        microphoneStatus = GetMicrophoneStatusText();
    }
    
    private float GetRecordingDuration()
    {
        if (!isRecording || recordedClip == null) return 0f;
        return (float)Microphone.GetPosition(selectedMicrophone) / sampleRate;
    }
    
    private float GetCurrentRecordingDuration()
    {
        if (!isRecording || string.IsNullOrEmpty(selectedMicrophone)) return 0f;
        return (float)Microphone.GetPosition(selectedMicrophone) / sampleRate;
    }
    
    private string GetMicrophoneStatusText()
    {
        if (!micPermissionGranted) return "❌ Microphone permission not granted";
        if (string.IsNullOrEmpty(selectedMicrophone)) return "⚠️ No microphone selected";
        
        string status = $"✅ {selectedMicrophone}";
        
        if (!string.IsNullOrEmpty(selectedMicrophone) && selectedMicrophone != "Default Microphone")
        {
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedMicrophone, out minFreq, out maxFreq);
            status += $" | {minFreq}-{maxFreq}Hz";
        }
        
        return status;
    }
    
    private void OnMicrophoneChanged()
    {
        Debug.Log($"Microphone changed to: {selectedMicrophone}");
        UpdateOdinInspectorValues();
    }
    
    private void OnPreferredMicrophoneChanged()
    {
        if (!string.IsNullOrEmpty(preferredDefaultMicrophone))
        {
            PlayerPrefs.SetString("PreferredMicrophone", preferredDefaultMicrophone);
            PlayerPrefs.Save();
            Debug.Log($"Preferred microphone set to: {preferredDefaultMicrophone}");
        }
    }
    
    private void OnConversationModeChanged()
    {
        if (!conversationModeEnabled && isConversationActive)
        {
            StopConversationModeInternal();
        }
        Debug.Log($"Conversation mode: {(conversationModeEnabled ? "Enabled" : "Disabled")}");
    }
    
    // ========================================
    // CORE FUNCTIONALITY (renamed internal methods)
    // ========================================

    private void InitializeComponents()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void CheckMicrophonePermission()
    {
#if PLATFORM_ANDROID
        recordingStatus = "Checking microphone permission...";
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
#elif PLATFORM_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Application.RequestUserAuthorization(UserAuthorization.Microphone);
        }
#else
        micPermissionGranted = true;
        recordingStatus = "Microphone ready. Select microphone and start recording.";
#endif
    }

    private void UpdateMicrophonePermission()
    {
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
            recordingStatus = "Microphone permission granted. Select microphone and start recording.";
            RefreshMicrophoneListInternal();
        }
#elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
            recordingStatus = "Microphone permission granted. Select microphone and start recording.";
            RefreshMicrophoneListInternal();
        }
#endif
    }

    private void RefreshMicrophoneListInternal()
    {
        if (!micPermissionGranted) return;

        availableMicrophones.Clear();
        
        // Get all available microphones
        foreach (var device in Microphone.devices)
        {
            availableMicrophones.Add(device);
        }

        // If no microphones found, add default
        if (availableMicrophones.Count == 0)
        {
            availableMicrophones.Add("Default Microphone");
        }

        // Load preferred microphone from PlayerPrefs
        string savedPreferredMic = PlayerPrefs.GetString("PreferredMicrophone", "");
        if (!string.IsNullOrEmpty(savedPreferredMic))
        {
            preferredDefaultMicrophone = savedPreferredMic;
        }

        // Auto-select microphone based on preference
        if (string.IsNullOrEmpty(selectedMicrophone) && availableMicrophones.Count > 0)
        {
            // First, try to select the preferred microphone if it's available
            if (!string.IsNullOrEmpty(preferredDefaultMicrophone) && availableMicrophones.Contains(preferredDefaultMicrophone))
            {
                selectedMicrophone = preferredDefaultMicrophone;
                Debug.Log($"Auto-selected preferred microphone: {selectedMicrophone}");
            }
            else
            {
                // If preferred microphone is not available, select the first one
                selectedMicrophone = availableMicrophones[0];
                Debug.Log($"Preferred microphone not available, selected: {selectedMicrophone}");
            }
        }
        
        Debug.Log($"Found {availableMicrophones.Count} microphones: {string.Join(", ", availableMicrophones)}");
        
        if (!string.IsNullOrEmpty(preferredDefaultMicrophone))
        {
            Debug.Log($"Preferred microphone setting: {preferredDefaultMicrophone}");
        }
    }

    private void InitializeAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("SSTManager: AudioSource component added automatically");
            }
        }
        
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
        }
    }



    private float GetMicrophoneLevel()
    {
        if (recordedClip == null || !isRecording) return 0f;
        
        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(selectedMicrophone);
        
        if (micPosition < 128) return 0f;
        
        recordedClip.GetData(samples, micPosition - 128);
        
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        
        return sum / samples.Length;
    }



    public void ToggleRecording()
    {
        if (isRecording)
        {
            StopRecordingInternal();
        }
        else
        {
            StartRecordingInternal();
        }
    }

    private void StartRecordingInternal()
    {
        if (!micPermissionGranted)
        {
            recordingStatus = "Microphone permission not granted!";
            return;
        }

        if (string.IsNullOrEmpty(selectedMicrophone))
        {
            recordingStatus = "No microphone selected!";
            return;
        }

        try
        {
            // Stop any existing recording
            if (recordedClip != null)
            {
                Microphone.End(selectedMicrophone);
            }

            // Start recording
            recordedClip = Microphone.Start(selectedMicrophone, false, recordingLength, sampleRate);
            isRecording = true;
            recordingStatus = $"Recording with {selectedMicrophone}... Speak now!";
            
            Debug.Log($"Started recording with microphone: {selectedMicrophone}");
            Debug.Log($"Sample rate: {sampleRate}Hz, Max length: {recordingLength}s");
        }
        catch (Exception e)
        {
            recordingStatus = $"Failed to start recording: {e.Message}";
            Debug.LogError($"Recording error: {e}");
        }
    }

    private void StopRecordingInternal()
    {
        if (!isRecording) return;

        try
        {
            int recordingEndPosition = Microphone.GetPosition(selectedMicrophone);
            Microphone.End(selectedMicrophone);
            
            // Trim the recorded clip to actual length
            if (recordingEndPosition > 0)
            {
                float[] samples = new float[recordingEndPosition * recordedClip.channels];
                recordedClip.GetData(samples, 0);
                
                recordedClip = AudioClip.Create("RecordedAudio", recordingEndPosition, recordedClip.channels, sampleRate, false);
                recordedClip.SetData(samples, 0);
                
                recordingData = samples;
            }
            
            isRecording = false;
            recordingStatus = $"Recording stopped. Duration: {recordedClip.length:F1}s. Ready to play back or send to Azure.";
            
            Debug.Log($"Recording completed. Length: {recordedClip.length}s, Samples: {recordingData?.Length}");
        }
        catch (Exception e)
        {
            recordingStatus = $"Failed to stop recording: {e.Message}";
            Debug.LogError($"Stop recording error: {e}");
        }
    }

    private void PlayRecordedAudioInternal()
    {
        if (recordedClip == null)
        {
            recordingStatus = "No recorded audio to play!";
            return;
        }

        audioSource.clip = recordedClip;
        audioSource.Play();
        recordingStatus = "Playing recorded audio...";
        
        Debug.Log("Playing back recorded audio");
    }

    private async Task SendToAzureInternal()
    {
        if (recordedClip == null)
        {
            azureStatus = "No recorded audio to send!";
            return;
        }

        try
        {
            lock (threadLocker)
            {
                waitingForReco = true;
                azureStatus = "Sending audio to Azure Speech Service...";
                recognitionResult = "Processing...";
            }

            // Convert AudioClip to WAV format for Azure
            byte[] wavData = ConvertAudioClipToWav(recordedClip);
            
            // Create speech config
            var config = SpeechConfig.FromSubscription(subscriptionKey, region);
            config.SpeechRecognitionLanguage = "en-US";

            // Create audio config from the recorded data
            using (var audioInputStream = AudioInputStream.CreatePushStream())
            using (var audioConfig = AudioConfig.FromStreamInput(audioInputStream))
            using (var recognizer = new SpeechRecognizer(config, audioConfig))
            {
                // Push the audio data
                audioInputStream.Write(wavData);
                audioInputStream.Close();

                // Perform speech recognition
                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                // Process results
                string newMessage = ProcessRecognitionResult(result);

                lock (threadLocker)
                {
                    recognitionResult = newMessage;
                    azureStatus = "Recognition completed";
                    waitingForReco = false;
                }
            }
        }
        catch (Exception e)
        {
            lock (threadLocker)
            {
                recognitionResult = $"Azure Speech Service error: {e.Message}";
                azureStatus = "Error occurred";
                waitingForReco = false;
            }
            Debug.LogError($"Azure Speech error: {e}");
        }
    }

    private string ProcessRecognitionResult(SpeechRecognitionResult result)
    {
        switch (result.Reason)
        {
            case ResultReason.RecognizedSpeech:
                Debug.Log($"Recognized: {result.Text}");
                
                // Append to TMP text field if assigned (for both conversation and regular mode)
                AppendRecognitionResult(result.Text);
                
                // Trigger the speech recognition event for callbacks
                OnSpeechRecognized.Invoke(result.Text);
                
                return $"{result.Text}";
                
            case ResultReason.NoMatch:
                Debug.Log("No speech could be recognized");
                return "❌ No speech could be recognized. Try speaking more clearly.";
                
            case ResultReason.Canceled:
                var cancellation = CancellationDetails.FromResult(result);
                Debug.LogError($"Recognition canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
                return $"❌ Recognition failed: {cancellation.Reason}\nDetails: {cancellation.ErrorDetails}";
                
            default:
                return $"⚠️ Unknown result: {result.Reason}";
        }
    }

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        // Convert float samples to 16-bit PCM
        short[] pcmData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            pcmData[i] = (short)(samples[i] * 32767f);
        }

        // Create WAV file in memory
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + pcmData.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1); // PCM format
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(pcmData.Length * 2);

            // Audio data
            foreach (short sample in pcmData)
            {
                writer.Write(sample);
            }

            return stream.ToArray();
        }
    }
    
    // ========================================
    // AUDIO CONVERSION UTILITIES
    // ========================================



    // ========================================
    // CONVERSATION MODE METHODS
    // ========================================
    
    private void StartConversationModeInternal()
    {
        if (!micPermissionGranted || string.IsNullOrEmpty(selectedMicrophone))
        {
            conversationStatus = "Cannot start - microphone not ready";
            return;
        }
        
        isConversationActive = true;
        isListeningForVoice = true;
        isVoiceDetected = false;
        voiceDetectionTimer = 0f;
        silenceTimer = 0f;
        
        // Start continuous microphone input for voice detection
        listeningClip = Microphone.Start(selectedMicrophone, true, 1, sampleRate);
        
        conversationStatus = "👂 Listening for voice...";
        conversationHistory.Clear();
        
        Debug.Log("Conversation mode started - listening for voice activity");
    }
    
    private void StopConversationModeInternal()
    {
        if (isRecording)
        {
            StopRecordingInternal();
        }
        
        if (listeningClip != null)
        {
            Microphone.End(selectedMicrophone);
            listeningClip = null;
        }
        
        isConversationActive = false;
        isListeningForVoice = false;
        isVoiceDetected = false;
        voiceDetectionTimer = 0f;
        silenceTimer = 0f;
        voiceActivity = 0f;
        
        conversationStatus = "Conversation mode stopped";
        
        Debug.Log("Conversation mode stopped");
    }
    
    private void UpdateConversationMode()
    {
        if (!isConversationActive) return;
        
        // Get current voice level for activity detection
        float currentVoiceLevel = GetListeningMicrophoneLevel();
        voiceActivity = currentVoiceLevel;
        
        if (isListeningForVoice && !isRecording)
        {
            // Check for voice activity
            if (currentVoiceLevel > voiceThreshold)
            {
                if (!isVoiceDetected)
                {
                    voiceDetectionTimer += Time.deltaTime;
                    voiceDetectionDebug = $"Voice detected! Timer: {voiceDetectionTimer:F1}s / {voiceStartDelay:F1}s";
                    
                    if (enableDebugLogging)
                    {
                        Debug.Log($"Voice Activity: {currentVoiceLevel:F3} > {voiceThreshold:F3} | Timer: {voiceDetectionTimer:F1}s");
                    }
                    
                    if (voiceDetectionTimer >= voiceStartDelay)
                    {
                        isVoiceDetected = true;
                        voiceDetectionDebug = "Starting recording!";
                        if (enableDebugLogging)
                        {
                            Debug.Log("Voice detection timer reached - starting recording!");
                        }
                        StartSpeechRecording();
                    }
                }
                else
                {
                    voiceDetectionDebug = "Voice detected - ready to record";
                }
                silenceTimer = 0f;
            }
            else
            {
                voiceDetectionTimer = 0f;
                isVoiceDetected = false;
                voiceDetectionDebug = $"Listening... Voice: {currentVoiceLevel:F3} (need: {voiceThreshold:F3})";
                
                if (enableDebugLogging && Time.frameCount % 120 == 0) // Log every 2 seconds
                {
                    Debug.Log($"Listening for voice - Current: {currentVoiceLevel:F3} | Threshold: {voiceThreshold:F3}");
                }
            }
        }
        else if (isRecording)
        {
            float recordingTime = GetCurrentRecordingDuration();
            
            // Check for silence to stop recording
            if (currentVoiceLevel < voiceThreshold)
            {
                silenceTimer += Time.deltaTime;
                voiceDetectionDebug = $"Recording... Silence: {silenceTimer:F1}s / {silenceDuration:F1}s";
                silenceDetectionDebug = $"🔇 SILENT: Voice {currentVoiceLevel:F3} < {voiceThreshold:F3} | Timer: {silenceTimer:F1}s/{silenceDuration:F1}s | Recording: {recordingTime:F1}s";
                
                if (enableDebugLogging)
                {
                    //Debug.Log($"🔇 SILENCE DETECTED: Timer {silenceTimer:F1}s / {silenceDuration:F1}s | Voice: {currentVoiceLevel:F3} | Recording time: {recordingTime:F1}s");
                }
                
                if (silenceTimer >= silenceDuration)
                {
                    voiceDetectionDebug = "Silence threshold reached - stopping recording!";
                    silenceDetectionDebug = $"🛑 STOPPING: Silence timer {silenceTimer:F1}s >= {silenceDuration:F1}s";
                    
                    Debug.Log($"🛑 SILENCE THRESHOLD REACHED! Timer: {silenceTimer:F1}s >= {silenceDuration:F1}s | Calling StopSpeechRecording()");
                    
                    StopSpeechRecording();
                }
            }
            else
            {
                silenceTimer = 0f;
                voiceDetectionDebug = $"Recording speech... Voice: {currentVoiceLevel:F3}";
                silenceDetectionDebug = $"🎤 ACTIVE: Voice {currentVoiceLevel:F3} > {voiceThreshold:F3} | Recording: {recordingTime:F1}s | Silence timer reset";
                
                if (enableDebugLogging && Time.frameCount % 60 == 0) // Log every second
                {
                    Debug.Log($"🎤 RECORDING ACTIVE SPEECH - Voice: {currentVoiceLevel:F3} | Duration: {recordingTime:F1}s");
                }
            }
        }
        
        lastVoiceLevel = currentVoiceLevel;
    }
    
    private void StartSpeechRecording()
    {
        Debug.Log($"🎙️ Starting speech recording. Microphone: {selectedMicrophone}");
        
        // Stop the listening clip
        if (listeningClip != null)
        {
            Microphone.End(selectedMicrophone);
            listeningClip = null;
            Debug.Log("Stopped listening clip");
        }
        
        try
        {
            // Start actual recording
            recordedClip = Microphone.Start(selectedMicrophone, false, recordingLength, sampleRate);
            isRecording = true;
            isListeningForVoice = false;
            silenceTimer = 0f;
            
            if (recordedClip != null)
            {
                conversationStatus = "🎤 Recording speech...";
                Debug.Log($"✅ Recording started successfully. Clip: {recordedClip.name}, Length: {recordingLength}s, Sample Rate: {sampleRate}Hz");
            }
            else
            {
                Debug.LogError("❌ Failed to start recording - recordedClip is null!");
                conversationStatus = "❌ Failed to start recording";
                isRecording = false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Exception starting recording: {e.Message}");
            conversationStatus = "❌ Recording start failed";
            isRecording = false;
        }
    }
    
    private async void StopSpeechRecording()
    {
        Debug.Log($"🛑 StopSpeechRecording() CALLED! IsRecording: {isRecording}");
        
        if (!isRecording) 
        {
            Debug.LogWarning("❌ StopSpeechRecording called but isRecording = false!");
            return;
        }
        
        // Stop recording
        int recordingEndPosition = Microphone.GetPosition(selectedMicrophone);
        Debug.Log($"📍 Microphone position before stopping: {recordingEndPosition}");
        
        Microphone.End(selectedMicrophone);
        Debug.Log($"🔇 Microphone.End() called for: {selectedMicrophone}");
        
        Debug.Log($"📊 Recording stopped. Position: {recordingEndPosition}, Recorded clip null: {recordedClip == null}");
        
        // Trim the recorded clip to actual length
        if (recordingEndPosition > 0 && recordedClip != null)
        {
            float[] samples = new float[recordingEndPosition * recordedClip.channels];
            recordedClip.GetData(samples, 0);
            
            // Create a new clip with the actual recorded length
            AudioClip finalClip = AudioClip.Create($"ConversationClip_{conversationClips.Count}", recordingEndPosition, recordedClip.channels, sampleRate, false);
            finalClip.SetData(samples, 0);
            
            recordedClip = finalClip;
            recordingData = samples;
            
            // Store in conversation clips array
            conversationClips.Add(finalClip);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            conversationClipTimestamps.Add(timestamp);
            
            Debug.Log($"✅ Clip stored! Total clips: {conversationClips.Count}, Length: {finalClip.length:F1}s");
        }
        else
        {
            Debug.LogError($"❌ Failed to create clip! Position: {recordingEndPosition}, Clip exists: {recordedClip != null}");
        }
        
        isRecording = false;
        conversationStatus = "✅ Speech recorded - processing...";
        
        // Add to conversation history
        string historyTimestamp = DateTime.Now.ToString("HH:mm:ss");
        string clipLength = recordedClip != null ? $"{recordedClip.length:F1}s" : "FAILED";
        conversationHistory.Add($"[{historyTimestamp}] Recorded: {clipLength} audio (Clip #{conversationClips.Count})");
        
        Debug.Log($"Speech recording completed in conversation mode. Clips stored: {conversationClips.Count}");
        
        // Auto-send to Azure if enabled
        if (autoSendToAzure && recordedClip != null)
        {
            await ProcessSpeechWithAzure();
        }
        else if (recordedClip == null)
        {
            Debug.LogError("Cannot send to Azure - no recorded clip!");
            conversationStatus = "❌ Recording failed - no clip created";
        }
        
        // Restart listening if continuous mode is enabled
        if (continuousListening)
        {
            RestartListening();
        }
        else
        {
            conversationStatus = "💬 Ready for next input";
        }
    }
    
    private async Task ProcessSpeechWithAzure()
    {
        conversationStatus = "🔄 Processing with Azure...";
        
        try
        {
            await SendToAzureInternal();
            
            // Add recognition result to conversation history
            if (!string.IsNullOrEmpty(recognitionResult))
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                conversationHistory.Add($"[{timestamp}] Recognized: {recognitionResult}");
                
                // Note: AppendRecognitionResult is already called in ProcessRecognitionResult()
                // No need to call it again here to avoid duplication
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Azure processing failed: {e.Message}");
            conversationStatus = "❌ Azure processing failed";
        }
    }
    
    private void RestartListening()
    {
        isListeningForVoice = true;
        isVoiceDetected = false;
        voiceDetectionTimer = 0f;
        silenceTimer = 0f;
        
        // Restart continuous microphone input
        listeningClip = Microphone.Start(selectedMicrophone, true, 1, sampleRate);
        
        conversationStatus = "👂 Listening for next speech...";
        
        Debug.Log("Restarted listening in conversation mode");
    }
    
    private float GetListeningMicrophoneLevel()
    {
        if (listeningClip == null && !isRecording) return 0f;
        
        AudioClip clipToUse = isRecording ? recordedClip : listeningClip;
        if (clipToUse == null) return 0f;
        
        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(selectedMicrophone);
        
        if (micPosition < 128) return 0f;
        
        clipToUse.GetData(samples, micPosition - 128);
        
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += Mathf.Abs(samples[i]);
        }
        
        return sum / samples.Length;
    }
    
    // ========================================
    // CONVERSATION HISTORY DISPLAY
    // ========================================
    
    [TitleGroup("Conversation History")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [ListDrawerSettings(Expanded = true, ShowIndexLabels = true)]
    [LabelText("Conversation Log")]
    public List<string> ConversationLog => conversationHistory;
    
    [TitleGroup("Conversation History")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [LabelText("Stored Clips Count")]
    public int StoredClipsCount => conversationClips.Count;
    
    [TitleGroup("Conversation History")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)]
    [LabelText("Clip Timestamps")]
    public List<string> ClipTimestamps => conversationClipTimestamps;
    
    [TitleGroup("Conversation History")]
    [ShowInInspector, ReadOnly]
    [ShowIf("conversationModeEnabled")]
    [ListDrawerSettings(Expanded = false, ShowIndexLabels = true)]
    [LabelText("Recognition Results")]
    public List<string> RecognitionResults => conversationRecognitionResults;
    
    [TitleGroup("Conversation History")]
    [Button("Clear Conversation History")]
    [ShowIf("conversationModeEnabled")]
    [GUIColor(0.8f, 0.6f, 0.4f)]
    public void ClearConversationHistory()
    {
        conversationHistory.Clear();
        conversationClips.Clear();
        conversationClipTimestamps.Clear();
        conversationRecognitionResults.Clear();
        Debug.Log("Conversation history and clips cleared");
    }
    
    [TitleGroup("Text Output")]
    [Button("Clear Recognition Log")]
    [GUIColor(0.6f, 0.8f, 0.6f)]
    [EnableIf("@recognitionAppendText != null")]
    public void ClearRecognitionLog()
    {
        if (recognitionAppendText != null)
        {
            recognitionAppendText.text = "";
            Debug.Log("📝 Recognition log cleared");
        }
    }
    
    private void AppendRecognitionResult(string result)
    {
        if (!string.IsNullOrEmpty(result))
        {
            // Queue the text update for main thread processing
            lock (uiUpdateLock)
            {
                pendingRecognitionTexts.Enqueue(result);
            }
        }
    }
    
    private void ProcessPendingUIUpdates()
    {
        // Process all queued recognition text updates on the main thread
        lock (uiUpdateLock)
        {
            while (pendingRecognitionTexts.Count > 0)
            {
                string result = pendingRecognitionTexts.Dequeue();
                
                if (recognitionAppendText != null)
                {
                    // Add timestamp for each entry
                    string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
                    string formattedResult = $"[{timestamp}] {result}\n";
                    
                    recognitionAppendText.text += formattedResult;
                    
                    // Optional: Scroll to bottom if the text is in a scroll view
                    // Canvas.ForceUpdateCanvases();
                }
            }
        }
    }
    

    
    [TitleGroup("Conversation History")]
    [Button("Play Last Clip")]
    [ShowIf("@conversationModeEnabled && conversationClips.Count > 0")]
    [GUIColor(0.4f, 0.4f, 0.8f)]
    public void PlayLastClip()
    {
        if (conversationClips.Count > 0)
        {
            audioSource.clip = conversationClips[conversationClips.Count - 1];
            audioSource.Play();
            Debug.Log($"Playing last recorded clip ({conversationClips.Count - 1})");
        }
    }
    
    [TitleGroup("Conversation History")]
    [ShowIf("@conversationModeEnabled && conversationClips.Count > 0")]
   
    [LabelText("Clip Index to Play")]
    public int clipIndexToPlay = 0;
    
    [TitleGroup("Conversation History")]
    [Button("Play Selected Clip")]
    [ShowIf("@conversationModeEnabled && conversationClips.Count > 0")]
    [GUIColor(0.6f, 0.4f, 0.8f)]
    public void PlaySelectedClip()
    {
        if (clipIndexToPlay >= 0 && clipIndexToPlay < conversationClips.Count)
        {
            audioSource.clip = conversationClips[clipIndexToPlay];
            audioSource.Play();
            Debug.Log($"Playing clip {clipIndexToPlay}: {conversationClipTimestamps[clipIndexToPlay]}");
        }
    }
    
    [TitleGroup("Conversation History")]
    [Button("Export Conversation")]
    [ShowIf("@conversationModeEnabled && conversationHistory.Count > 0")]
    public void ExportConversation()
    {
        string export = "=== CONVERSATION EXPORT ===\n";
        export += $"Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
        export += $"Total entries: {conversationHistory.Count}\n\n";
        
        foreach (string entry in conversationHistory)
        {
            export += entry + "\n";
        }
        
        Debug.Log(export);
    }
    
    private void OnDestroy()
    {
        // Clean up any active recordings
        if (isRecording && !string.IsNullOrEmpty(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
        }
        
        if (isConversationActive && listeningClip != null)
        {
            Microphone.End(selectedMicrophone);
        }
        

    }
}