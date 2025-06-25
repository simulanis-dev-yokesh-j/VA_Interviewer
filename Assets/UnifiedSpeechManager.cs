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

public class UnifiedSpeechManager : SerializedMonoBehaviour
{
    [Title("🎤🔊 Unified Speech Manager - STT & TTS")]
    [InfoBox("Complete Speech-to-Text and Text-to-Speech solution using Azure Cognitive Services. Test directly from Inspector or connect to UI.")]
    
    // ========================================
    // AZURE CONFIGURATION
    // ========================================
    
    [TitleGroup("Azure Configuration")]
    [InfoBox("Azure Speech Service credentials - shared for both STT and TTS")]
    [SerializeField, ReadOnly]
    private readonly string subscriptionKey = "9v2mwusnyHO6LBsDWvcc6xh6M9WsiJIuqEbwe95kHTABg7bAiEs3JQQJ99BFACGhslBXJ3w3AAAYACOGOxq2";
    
    [TitleGroup("Azure Configuration")]
    [SerializeField, ReadOnly]
    private readonly string region = "centralindia";
    
    [TitleGroup("Azure Configuration")]
    [ShowInInspector, ReadOnly]
    [LabelText("Service Status")]
    public string serviceStatus = "Initializing...";
    
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
    public int sampleRate = 16000;
    
    [TitleGroup("Microphone Settings")]
    [Range(5, 60)]
    [LabelText("Max Recording Length (seconds)")]
    public int recordingLength = 15;
    
    // ========================================
    // SPEECH-TO-TEXT CONTROLS
    // ========================================
    
    [TitleGroup("Speech-to-Text Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.8f, 0.4f)]
    [ShowIf("@!isRecording && micPermissionGranted && speechRecognizer != null")]
    public void StartRecording()
    {
        StartRecordingInternal();
    }
    
    [TitleGroup("Speech-to-Text Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [ShowIf("isRecording")]
    public void StopRecording()
    {
        StopRecordingInternal();
    }
    
    [TitleGroup("Speech-to-Text Controls")]
    [ShowInInspector, ReadOnly]
    [ProgressBar(0, 1, ColorMember = "GetVolumeColor")]
    [LabelText("Recording Volume")]
    public float currentVolume;
    
    [TitleGroup("Speech-to-Text Controls")]
    [ShowInInspector, ReadOnly]
    [LabelText("Recording Status")]
    public string recordingStatus = "Ready to record";
    
    [TitleGroup("Speech-to-Text Controls")]
    [Button(ButtonSizes.Medium, ButtonStyle.Box)]
    [GUIColor(0.4f, 0.4f, 0.8f)]
    [EnableIf("@recordedClip != null && !isRecording")]
    public void PlayRecordedAudio()
    {
        PlayRecordedAudioInternal();
    }
    
    [TitleGroup("Speech-to-Text Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.8f, 0.6f, 0.2f)]
    [EnableIf("@recordedClip != null && !isRecording && !waitingForRecognition")]
    public void ProcessSpeechRecognition()
    {
        ProcessRecognitionInternal();
    }
    
    [TitleGroup("Speech-to-Text Controls")]
    [ShowInInspector, ReadOnly]
    [TextArea(3, 5)]
    [LabelText("Recognition Result")]
    public string recognitionResult = "No recognition performed yet";
    
    // ========================================
    // TEXT-TO-SPEECH CONTROLS
    // ========================================
    
    [TitleGroup("Text-to-Speech Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.2f, 0.8f, 0.6f)]
    [EnableIf("@speechSynthesizer != null && !waitingForSynthesis && !string.IsNullOrEmpty(textToSynthesize)")]
    public void SpeakText()
    {
        SynthesizeTextInternal();
    }
    
    [TitleGroup("Text-to-Speech Controls")]
    [TextArea(2, 4)]
    [LabelText("Text to Synthesize")]
    public string textToSynthesize = "Hello! This is a test of the speech synthesis system.";
    
    [TitleGroup("Text-to-Speech Controls")]
    [ValueDropdown("GetVoiceOptions")]
    [LabelText("Voice Selection")]
    public string selectedVoice = "en-IN-AaravNeural";
    
    [TitleGroup("Text-to-Speech Controls")]
    [ShowInInspector, ReadOnly]
    [LabelText("Synthesis Status")]
    public string synthesisStatus = "Ready to synthesize";
    
    // ========================================
    // COMBINED WORKFLOW
    // ========================================
    
    [TitleGroup("Combined Workflow")]
    [Button(ButtonSizes.Gigantic, ButtonStyle.Box)]
    [GUIColor(0.9f, 0.6f, 0.9f)]
    [EnableIf("@speechRecognizer != null && speechSynthesizer != null && micPermissionGranted")]
    public void StartConversationMode()
    {
        StartConversationModeInternal();
    }
    
    [TitleGroup("Combined Workflow")]
    [ShowInInspector, ReadOnly]
    [LabelText("Conversation Status")]
    public string conversationStatus = "Ready for conversation mode";
    
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
    [Button("🔄 Refresh Microphones")]
    public void RefreshMicrophones()
    {
        RefreshMicrophoneListInternal();
    }
    
    [TitleGroup("Debug Information")]
    [Button("🎤 Test Permissions")]
    public void TestPermissions()
    {
        CheckMicrophonePermission();
    }
    
    [TitleGroup("Debug Information")]
    [Button("🔧 Initialize Services")]
    public void InitializeServices()
    {
        InitializeSpeechServicesInternal();
    }
    
    // ========================================
    // UI COMPONENTS (Optional)
    // ========================================
    
    [FoldoutGroup("UI Components (Optional)")]
    [InfoBox("Connect UI elements here if you want to use buttons instead of Inspector controls.")]
    
    [FoldoutGroup("UI Components (Optional)")]
    public TextMeshProUGUI statusDisplay;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TextMeshProUGUI recognitionDisplay;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TMP_InputField textInput;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button recordButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button playButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button recognizeButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button synthesizeButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Button conversationButton;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TMP_Dropdown microphoneDropdown;
    
    [FoldoutGroup("UI Components (Optional)")]
    public TMP_Dropdown voiceDropdown;
    
    [FoldoutGroup("UI Components (Optional)")]
    public Slider volumeIndicator;
    
    [FoldoutGroup("UI Components (Optional)")]
    public AudioSource audioSource;
    
    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    
    private object threadLocker = new object();
    private bool waitingForRecognition;
    private bool waitingForSynthesis;
    private bool isRecording;
    private bool micPermissionGranted = false;
    private bool conversationMode = false;
    
    // Audio recording variables
    private AudioClip recordedClip;
    private float[] recordingData;
    
    // Azure Speech Services
    private SpeechRecognizer speechRecognizer;
    private SpeechSynthesizer speechSynthesizer;
    private SpeechConfig speechConfig;
    
    // Voice options
    private List<string> voiceOptions = new List<string>
    {
        "en-IN-AaravNeural",
        "en-IN-PrabhatNeural", 
        "en-US-AriaNeural",
        "en-US-JennyNeural",
        "en-US-GuyNeural",
        "en-US-DavisNeural"
    };

    void Start()
    {
        InitializeComponents();
        CheckMicrophonePermission();
        RefreshMicrophoneListInternal();
        InitializeSpeechServicesInternal();
        SetupUI();
    }

    void Update()
    {
        UpdateMicrophonePermission();
        UpdateUI();
        UpdateVolumeIndicator();
        UpdateInspectorValues();
    }
    
    // ========================================
    // INITIALIZATION METHODS
    // ========================================
    
    private void InitializeComponents()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    private async void InitializeSpeechServicesInternal()
    {
        try
        {
            serviceStatus = "Initializing Azure Speech Services...";
            
            // Create speech configuration
            speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechRecognitionLanguage = "en-US";
            speechConfig.SpeechSynthesisLanguage = "en-IN";
            speechConfig.SpeechSynthesisVoiceName = selectedVoice;
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
            
            // Initialize recognizer
            speechRecognizer = new SpeechRecognizer(speechConfig);
            
            // Initialize synthesizer
            speechSynthesizer = new SpeechSynthesizer(speechConfig);
            
            serviceStatus = "✅ Azure Speech Services initialized successfully";
            Debug.Log("Speech services initialized successfully");
        }
        catch (Exception e)
        {
            serviceStatus = $"❌ Failed to initialize: {e.Message}";
            Debug.LogError($"Speech service initialization error: {e}");
        }
    }
    
    // ========================================
    // MICROPHONE METHODS
    // ========================================
    
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
        recordingStatus = "Microphone ready";
#endif
    }

    private void UpdateMicrophonePermission()
    {
#if PLATFORM_ANDROID
        if (!micPermissionGranted && Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micPermissionGranted = true;
            recordingStatus = "Microphone permission granted";
            RefreshMicrophoneListInternal();
        }
#elif PLATFORM_IOS
        if (!micPermissionGranted && Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            micPermissionGranted = true;
            recordingStatus = "Microphone permission granted";
            RefreshMicrophoneListInternal();
        }
#endif
    }

    private void RefreshMicrophoneListInternal()
    {
        if (!micPermissionGranted) return;

        availableMicrophones.Clear();
        
        foreach (var device in Microphone.devices)
        {
            availableMicrophones.Add(device);
        }

        if (availableMicrophones.Count == 0)
        {
            availableMicrophones.Add("Default Microphone");
        }

        if (string.IsNullOrEmpty(selectedMicrophone) && availableMicrophones.Count > 0)
        {
            selectedMicrophone = availableMicrophones[0];
        }
        
        Debug.Log($"Found {availableMicrophones.Count} microphones: {string.Join(", ", availableMicrophones)}");
    }
    
    // ========================================
    // RECORDING METHODS
    // ========================================
    
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
            if (recordedClip != null)
            {
                Microphone.End(selectedMicrophone);
            }

            recordedClip = Microphone.Start(selectedMicrophone, false, recordingLength, sampleRate);
            isRecording = true;
            recordingStatus = $"🎤 Recording with {selectedMicrophone}... Speak now!";
            
            Debug.Log($"Started recording with: {selectedMicrophone}");
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
            
            if (recordingEndPosition > 0)
            {
                float[] samples = new float[recordingEndPosition * recordedClip.channels];
                recordedClip.GetData(samples, 0);
                
                recordedClip = AudioClip.Create("RecordedAudio", recordingEndPosition, recordedClip.channels, sampleRate, false);
                recordedClip.SetData(samples, 0);
                
                recordingData = samples;
            }
            
            isRecording = false;
            recordingStatus = $"✅ Recording complete: {recordedClip.length:F1}s";
            
            Debug.Log($"Recording completed. Length: {recordedClip.length}s");
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
        recordingStatus = "▶️ Playing recorded audio...";
    }
    
    // ========================================
    // SPEECH RECOGNITION METHODS
    // ========================================
    
    private async void ProcessRecognitionInternal()
    {
        if (recordedClip == null)
        {
            recognitionResult = "No recorded audio to process!";
            return;
        }

        if (speechRecognizer == null)
        {
            recognitionResult = "Speech recognizer not initialized!";
            return;
        }

        try
        {
            lock (threadLocker)
            {
                waitingForRecognition = true;
                recognitionResult = "🔄 Processing speech recognition...";
            }

            byte[] wavData = ConvertAudioClipToWav(recordedClip);
            
            using (var audioInputStream = AudioInputStream.CreatePushStream())
            using (var audioConfig = AudioConfig.FromStreamInput(audioInputStream))
            using (var recognizer = new SpeechRecognizer(speechConfig, audioConfig))
            {
                audioInputStream.Write(wavData);
                audioInputStream.Close();

                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);
                string resultText = ProcessRecognitionResult(result);

                lock (threadLocker)
                {
                    recognitionResult = resultText;
                    waitingForRecognition = false;
                    
                    // In conversation mode, automatically synthesize the recognized text
                    if (conversationMode && result.Reason == ResultReason.RecognizedSpeech)
                    {
                        textToSynthesize = result.Text;
                        conversationStatus = "🔄 Auto-synthesizing recognized text...";
                        // Note: We'd call synthesis here in a real conversation mode
                    }
                }
            }
        }
        catch (Exception e)
        {
            lock (threadLocker)
            {
                recognitionResult = $"❌ Recognition error: {e.Message}";
                waitingForRecognition = false;
            }
            Debug.LogError($"Recognition error: {e}");
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
                return $"❌ Recognition failed: {cancellation.Reason}";
                
            default:
                return $"⚠️ Unknown result: {result.Reason}";
        }
    }
    
    // ========================================
    // SPEECH SYNTHESIS METHODS
    // ========================================
    
    private async void SynthesizeTextInternal()
    {
        if (speechSynthesizer == null)
        {
            synthesisStatus = "Speech synthesizer not initialized!";
            return;
        }
        
        if (string.IsNullOrEmpty(textToSynthesize))
        {
            synthesisStatus = "No text to synthesize!";
            return;
        }

        try
        {
            lock (threadLocker)
            {
                waitingForSynthesis = true;
                synthesisStatus = "🔄 Synthesizing speech...";
            }

            // Update voice if changed
            speechConfig.SpeechSynthesisVoiceName = selectedVoice;
            speechSynthesizer.Dispose();
            speechSynthesizer = new SpeechSynthesizer(speechConfig);

            using var result = await speechSynthesizer.SpeakTextAsync(textToSynthesize).ConfigureAwait(false);

            lock (threadLocker)
            {
                if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                {
                    synthesisStatus = "✅ Speech synthesis completed successfully";
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                    synthesisStatus = $"❌ Synthesis failed: {cancellation.Reason}";
                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        synthesisStatus += $" - {cancellation.ErrorDetails}";
                    }
                }
                
                waitingForSynthesis = false;
                
                if (conversationMode)
                {
                    conversationStatus = "✅ Conversation cycle complete. Ready for next input.";
                }
            }
        }
        catch (Exception e)
        {
            lock (threadLocker)
            {
                synthesisStatus = $"❌ Synthesis error: {e.Message}";
                waitingForSynthesis = false;
            }
            Debug.LogError($"Synthesis error: {e}");
        }
    }
    
    // ========================================
    // CONVERSATION MODE METHODS
    // ========================================
    
    private void StartConversationModeInternal()
    {
        conversationMode = !conversationMode;
        
        if (conversationMode)
        {
            conversationStatus = "🎙️ Conversation mode ACTIVE - Speak and I'll repeat it back!";
            Debug.Log("Conversation mode activated");
        }
        else
        {
            conversationStatus = "⏸️ Conversation mode STOPPED";
            Debug.Log("Conversation mode deactivated");
        }
    }
    
    // ========================================
    // UI METHODS
    // ========================================
    
    private void SetupUI()
    {
        if (recordButton != null)
        {
            recordButton.onClick.AddListener(() => ToggleRecording());
        }
        
        if (playButton != null)
        {
            playButton.onClick.AddListener(PlayRecordedAudioInternal);
        }
        
        if (recognizeButton != null)
        {
            recognizeButton.onClick.AddListener(ProcessRecognitionInternal);
        }
        
        if (synthesizeButton != null)
        {
            synthesizeButton.onClick.AddListener(SynthesizeTextInternal);
        }
        
        if (conversationButton != null)
        {
            conversationButton.onClick.AddListener(StartConversationModeInternal);
        }
        
        if (microphoneDropdown != null)
        {
            microphoneDropdown.onValueChanged.AddListener(OnMicrophoneDropdownChanged);
        }
        
        if (voiceDropdown != null)
        {
            voiceDropdown.onValueChanged.AddListener(OnVoiceDropdownChanged);
        }
        
        if (textInput != null)
        {
            textInput.onValueChanged.AddListener(OnTextInputChanged);
        }
    }

    private void UpdateUI()
    {
        if (statusDisplay != null)
        {
            statusDisplay.text = $"Service: {serviceStatus}\nMic: {microphoneStatus}\nRecording: {recordingStatus}\nSynthesis: {synthesisStatus}";
        }
        
        if (recognitionDisplay != null)
        {
            recognitionDisplay.text = recognitionResult;
        }
        
        if (recordButton != null)
        {
            recordButton.interactable = micPermissionGranted && !waitingForRecognition && !waitingForSynthesis;
            var buttonText = recordButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isRecording ? "Stop Recording" : "Start Recording";
            }
        }
        
        if (playButton != null)
        {
            playButton.interactable = recordedClip != null && !isRecording;
        }
        
        if (recognizeButton != null)
        {
            recognizeButton.interactable = recordedClip != null && !isRecording && !waitingForRecognition;
        }
        
        if (synthesizeButton != null)
        {
            synthesizeButton.interactable = !string.IsNullOrEmpty(textToSynthesize) && speechSynthesizer != null && !waitingForSynthesis;
        }
        
        if (conversationButton != null)
        {
            conversationButton.interactable = speechRecognizer != null && speechSynthesizer != null && micPermissionGranted;
            var buttonText = conversationButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = conversationMode ? "Stop Conversation" : "Start Conversation";
            }
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
    
    // ========================================
    // HELPER METHODS
    // ========================================
    
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
    
    private Color GetVolumeColor()
    {
        if (currentVolume < 0.3f) return Color.green;
        if (currentVolume < 0.7f) return Color.yellow;
        return Color.red;
    }
    
    private string GetCurrentState()
    {
        if (waitingForRecognition) return "🔄 Processing Recognition";
        if (waitingForSynthesis) return "🔄 Processing Synthesis";
        if (isRecording) return "🎤 Recording";
        if (conversationMode) return "💬 Conversation Mode";
        if (recordedClip != null) return "✅ Audio Ready";
        return "⏸️ Idle";
    }
    
    private void UpdateInspectorValues()
    {
        if (isRecording)
        {
            currentVolume = GetMicrophoneLevel();
        }
        else
        {
            currentVolume = 0f;
        }
        
        microphoneStatus = GetMicrophoneStatusText();
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
    
    private string GetMicrophoneStatusText()
    {
        if (!micPermissionGranted) return "❌ Permission not granted";
        if (string.IsNullOrEmpty(selectedMicrophone)) return "⚠️ No microphone selected";
        
        string status = $"✅ {selectedMicrophone}";
        
        if (!string.IsNullOrEmpty(selectedMicrophone) && selectedMicrophone != "Default Microphone")
        {
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(selectedMicrophone, out minFreq, out maxFreq);
            status += $" ({minFreq}-{maxFreq}Hz)";
        }
        
        return status;
    }
    
    private void OnMicrophoneChanged()
    {
        Debug.Log($"Microphone changed to: {selectedMicrophone}");
    }
    
    private void OnMicrophoneDropdownChanged(int index)
    {
        if (index >= 0 && index < availableMicrophones.Count)
        {
            selectedMicrophone = availableMicrophones[index];
        }
    }
    
    private void OnVoiceDropdownChanged(int index)
    {
        if (index >= 0 && index < voiceOptions.Count)
        {
            selectedVoice = voiceOptions[index];
        }
    }
    
    private void OnTextInputChanged(string newText)
    {
        textToSynthesize = newText;
    }
    
    private List<string> GetVoiceOptions()
    {
        return voiceOptions;
    }
    
    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);

        short[] pcmData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            pcmData[i] = (short)(samples[i] * 32767f);
        }

        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + pcmData.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)clip.channels);
            writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2);
            writer.Write((short)(clip.channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(pcmData.Length * 2);

            foreach (short sample in pcmData)
            {
                writer.Write(sample);
            }

            return stream.ToArray();
        }
    }

    void OnDestroy()
    {
        if (isRecording && !string.IsNullOrEmpty(selectedMicrophone))
        {
            Microphone.End(selectedMicrophone);
        }
        
        speechRecognizer?.Dispose();
        speechSynthesizer?.Dispose();
    }
} 