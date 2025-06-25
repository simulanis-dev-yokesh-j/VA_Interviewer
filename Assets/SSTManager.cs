using UnityEngine;
using UnityEngine.UI;
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
    // RECORDING CONTROLS
    // ========================================
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [ShowIf("@!isRecording && micPermissionGranted")]
    public void StartRecording()
    {
        StartRecordingInternal();
    }
    
    [TitleGroup("Recording Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [ShowIf("isRecording")]
    public void StopRecording()
    {
        StopRecordingInternal();
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
        SendToAzureInternal();
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
    // UI COMPONENTS (Optional - for later UI implementation)
    // ========================================
    
    [FoldoutGroup("UI Components (Optional)")]
    [InfoBox("These are optional. Leave empty when testing with Odin Inspector.")]
    public TextMeshProUGUI outputText;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TextMeshProUGUI microphoneStatusText;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button startRecordButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button playbackButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button confirmAndSendButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TMP_Dropdown microphoneDropdown;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Slider volumeIndicator;
    
    [FoldoutGroup("UI Components (Optional)")]
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
    
    // Azure Speech Config
    private readonly string subscriptionKey = "9v2mwusnyHO6LBsDWvcc6xh6M9WsiJIuqEbwe95kHTABg7bAiEs3JQQJ99BFACGhslBXJ3w3AAAYACOGOxq2";
    private readonly string region = "centralindia";

#if PLATFORM_ANDROID || PLATFORM_IOS
    private Microphone mic;
#endif

    void Start()
    {
        InitializeComponents();
        CheckMicrophonePermission();
        RefreshMicrophoneListInternal();
        SetupUI();
    }

    void Update()
    {
        UpdateMicrophonePermission();
        UpdateUI();
        UpdateVolumeIndicator();
        UpdateOdinInspectorValues();
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
    
    private string GetCurrentState()
    {
        if (waitingForReco) return "🔄 Processing Azure Request";
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

        // Set the first microphone as selected if none selected
        if (string.IsNullOrEmpty(selectedMicrophone) && availableMicrophones.Count > 0)
        {
            selectedMicrophone = availableMicrophones[0];
        }
        
        Debug.Log($"Found {availableMicrophones.Count} microphones: {string.Join(", ", availableMicrophones)}");
    }

    private void SetupUI()
    {
        // Only setup UI if components are assigned
        if (startRecordButton != null)
        {
            startRecordButton.onClick.AddListener(() => ToggleRecording());
        }
        
        if (playbackButton != null)
        {
            playbackButton.onClick.AddListener(PlayRecordedAudioInternal);
            playbackButton.interactable = false;
        }
        
        if (confirmAndSendButton != null)
        {
            confirmAndSendButton.onClick.AddListener(SendToAzureInternal);
            confirmAndSendButton.interactable = false;
        }
        
        if (microphoneDropdown != null)
        {
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDropdownChanged);
        }
    }

    private void UpdateUI()
    {
        // Only update UI if components are assigned
        if (outputText != null)
        {
            outputText.text = recognitionResult;
        }
        
        if (microphoneStatusText != null)
        {
            microphoneStatusText.text = microphoneStatus;
        }
        
        if (startRecordButton != null)
        {
            startRecordButton.interactable = micPermissionGranted && !waitingForReco;
            var buttonText = startRecordButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isRecording ? "Stop Recording" : "Start Recording";
            }
        }
        
        if (playbackButton != null)
        {
            playbackButton.interactable = recordedClip != null && !isRecording && !waitingForReco;
        }
        
        if (confirmAndSendButton != null)
        {
            confirmAndSendButton.interactable = recordedClip != null && !isRecording && !waitingForReco;
        }
        
        if (microphoneDropdown != null)
        {
            microphoneDropdown.interactable = !isRecording && !waitingForReco;
        }
    }

    private void UpdateVolumeIndicator()
    {
        if (volumeIndicator != null && isRecording && recordedClip != null)
        {
            volumeIndicator.value = GetMicrophoneLevel();
        }
        else if (volumeIndicator != null)
        {
            volumeIndicator.value = 0f;
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

    private void OnMicrophoneDropdownChanged(int index)
    {
        if (index >= 0 && index < availableMicrophones.Count)
        {
            selectedMicrophone = availableMicrophones[index];
            Debug.Log($"Microphone changed via dropdown to: {selectedMicrophone}");
        }
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

    private async void SendToAzureInternal()
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
                return $"✅ Recognized: {result.Text}";
                
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
    // PUBLIC METHODS FOR UI DEBUGGING
    // ========================================
    
    [TitleGroup("Manual UI Connection")]
    [Button("Force UI Setup")]
    [InfoBox("Click this if UI buttons are not working. This will re-setup the UI connections.")]
    public void ForceUISetup()
    {
        SetupUI();
        Debug.Log("🔄 UI setup forced. Check if UI components are assigned in the 'UI Components (Optional)' section.");
    }
    
    [TitleGroup("Manual UI Connection")]
    [Button("Test UI Connections")]
    public void TestUIConnections()
    {
        Debug.Log("=== UI Connection Status ===");
        Debug.Log($"outputText: {(outputText != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"microphoneStatusText: {(microphoneStatusText != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"startRecordButton: {(startRecordButton != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"playbackButton: {(playbackButton != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"confirmAndSendButton: {(confirmAndSendButton != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"microphoneDropdown: {(microphoneDropdown != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"volumeIndicator: {(volumeIndicator != null ? "✅ Connected" : "❌ Missing")}");
        Debug.Log($"audioSource: {(audioSource != null ? "✅ Connected" : "❌ Missing")}");
    }

    private void OnDestroy()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
        }
    }
}