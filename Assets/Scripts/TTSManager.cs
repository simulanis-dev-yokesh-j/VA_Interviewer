using UnityEngine;
using Microsoft.CognitiveServices.Speech;
using TMPro;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Collections;
using System.IO;

public class TTSManager : SerializedMonoBehaviour
{
    [Title("Text-to-Speech Manager")]
    [InfoBox("Dedicated manager for Azure Speech Services Text-to-Speech functionality.")]
    
    // ========================================
    // TTS SETTINGS
    // ========================================
    
    [TitleGroup("TTS Settings")]
    [LabelText("Enable TTS")]
    public bool ttsEnabled = true;
    
    [TitleGroup("TTS Settings")]
    [ShowIf("ttsEnabled")]
    [ValueDropdown("GetVoiceOptions")]
    [LabelText("Voice Selection")]
    public string selectedVoice = "en-IN-AaravNeural";
    
    [TitleGroup("TTS Settings")]
    [ShowIf("ttsEnabled")]
    [LabelText("Text to Synthesize")]
    [TextArea(2, 4)]
    public string textToSynthesize = "Hello! This is a test of the speech synthesis system.";
    
    [TitleGroup("TTS Settings")]
    [ShowIf("ttsEnabled")]
    [ShowInInspector, ReadOnly]
    [LabelText("TTS Status")]
    public string ttsStatus = "TTS Ready";
    
    [TitleGroup("TTS Settings")]
    [ShowIf("ttsEnabled")]
    [LabelText("Save Audio for Debug")]
    [InfoBox("Save received audio data to Resources folder for debugging")]
    public bool saveAudioForDebug = false;
    
    // ========================================
    // AUDIO COMPONENTS
    // ========================================
    
    [TitleGroup("Audio Setup")]
    [Required]
    [LabelText("Audio Source")]
    [InfoBox("AudioSource component for playing TTS audio. Required for Unity/SALSA integration.")]
    public AudioSource audioSource;
    
    [TitleGroup("Audio Setup")]
    [LabelText("TTS Input Field")]
    [InfoBox("Optional: TMP Input Field for manual text entry.")]
    public TMP_InputField ttsInputField;
    
    // ========================================
    // TTS CONTROLS
    // ========================================
    
    [TitleGroup("TTS Controls")]
    [Button(ButtonSizes.Large, ButtonStyle.Box)]
    [GUIColor(0.6f, 0.2f, 0.8f)]
    [ShowIf("ttsEnabled")]
    [EnableIf("@!waitingForTTS && !string.IsNullOrEmpty(textToSynthesize)")]
    public void SpeakText()
    {
        SynthesizeTextInternal();
    }
    
    [TitleGroup("TTS Controls")]
    [Button("Speak from Input Field", ButtonSizes.Medium)]
    [GUIColor(0.5f, 0.3f, 0.7f)]
    [ShowIf("@ttsEnabled && ttsInputField != null")]
    [EnableIf("@!waitingForTTS && ttsInputField != null && !string.IsNullOrEmpty(ttsInputField.text)")]
    public void SpeakFromInputField()
    {
        if (ttsInputField != null && !string.IsNullOrEmpty(ttsInputField.text))
        {
            textToSynthesize = ttsInputField.text;
            SynthesizeTextInternal();
        }
    }
    
    [TitleGroup("TTS Controls")]
    [Button("Stop Current Speech", ButtonSizes.Medium)]
    [GUIColor(0.8f, 0.4f, 0.4f)]
    [ShowIf("ttsEnabled")]
    [EnableIf("@audioSource != null && audioSource.isPlaying")]
    public void StopSpeech()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            ttsStatus = "Speech stopped";
        }
    }
    
    // ========================================
    // PRIVATE VARIABLES
    // ========================================
    
    private SpeechSynthesizer speechSynthesizer;
    private SpeechConfig speechConfig;
    public bool waitingForTTS = false;
    
    // Azure Speech Config
    private readonly string subscriptionKey = "9v2mwusnyHO6LBsDWvcc6xh6M9WsiJIuqEbwe95kHTABg7bAiEs3JQQJ99BFACGhslBXJ3w3AAAYACOGOxq2";
    private readonly string region = "centralindia";
    
    // Voice options for dropdown
    private List<string> voiceOptions = new List<string>
    {
        "en-IN-AaravNeural",   // Indian Male
        "en-IN-AnanyaNeural",  // Indian Female  
        "en-US-DavisNeural",   // US Male
        "en-US-AriaNeural",    // US Female
        "en-GB-RyanNeural",    // British Male
        "en-GB-SoniaNeural"    // British Female
    };
    
    // Thread-safe audio playback queue
    private Queue<(byte[] audioData, string text)> pendingAudioPlayback = new Queue<(byte[], string)>();
    private readonly object audioUpdateLock = new object();
    
    // ========================================
    // UNITY LIFECYCLE
    // ========================================
    
    void Start()
    {
        InitializeTTSServices();
        ValidateComponents();
    }
    
    void Update()
    {
        // Process pending audio playback on main thread
        ProcessPendingAudioPlayback();
    }
    
    // ========================================
    // INITIALIZATION
    // ========================================
    
    private void ValidateComponents()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("TTSManager: AudioSource component added automatically");
            }
        }
        
        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = 1.0f;
        }
    }
    
    private void InitializeTTSServices()
    {
        if (!ttsEnabled) return;
        
        try
        {
            ttsStatus = "Initializing TTS services...";
            
            // Create speech configuration
            speechConfig = SpeechConfig.FromSubscription(subscriptionKey, region);
            speechConfig.SpeechSynthesisLanguage = "en-IN";
            speechConfig.SpeechSynthesisVoiceName = selectedVoice;
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Riff24Khz16BitMonoPcm);
            
            // Initialize synthesizer (null = no direct audio output, we handle it manually)
            speechSynthesizer = new SpeechSynthesizer(speechConfig, null);
            
            ttsStatus = "✅ TTS services initialized successfully";
            Debug.Log("TTSManager: TTS services initialized successfully");
        }
        catch (Exception e)
        {
            ttsStatus = $"❌ TTS initialization failed: {e.Message}";
            Debug.LogError($"TTSManager: TTS initialization error: {e}");
        }
    }
    
    // ========================================
    // PUBLIC API METHODS
    // ========================================
    
    /// <summary>
    /// Speak the provided text using TTS
    /// </summary>
    /// <param name="text">Text to synthesize and speak</param>
    public async Task SpeakTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TTSManager: Cannot speak empty text");
            return;
        }
        
        textToSynthesize = text;
        await SynthesizeTextInternalAsync();
    }
    
    /// <summary>
    /// Speak text immediately (fire and forget)
    /// </summary>
    /// <param name="text">Text to synthesize and speak</param>
    public void SpeakTextImmediate(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            Debug.LogWarning("TTSManager: Cannot speak empty text");
            return;
        }
        
        textToSynthesize = text;
        SynthesizeTextInternal();
    }
    
    /// <summary>
    /// Check if TTS is currently processing
    /// </summary>
    public bool IsSpeaking()
    {
        return waitingForTTS || (audioSource != null && audioSource.isPlaying);
    }
    
    /// <summary>
    /// Stop any current speech playback
    /// </summary>
    public void StopCurrentSpeech()
    {
        StopSpeech();
    }
    
    // ========================================
    // TTS IMPLEMENTATION
    // ========================================
    
    private void SynthesizeTextInternal()
    {
        StartCoroutine(SynthesizeTextCoroutine());
    }
    
    private async Task SynthesizeTextInternalAsync()
    {
        if (!ttsEnabled || speechSynthesizer == null)
        {
            ttsStatus = "TTS not initialized!";
            return;
        }
        
        if (string.IsNullOrEmpty(textToSynthesize))
        {
            ttsStatus = "No text to synthesize!";
            return;
        }

        try
        {
            waitingForTTS = true;
            ttsStatus = "🔄 Synthesizing speech...";

            // Update voice if changed
            speechConfig.SpeechSynthesisVoiceName = selectedVoice;
            speechSynthesizer.Dispose();
            speechSynthesizer = new SpeechSynthesizer(speechConfig, null);

            // Synthesize to audio data
            using var result = await speechSynthesizer.SpeakTextAsync(textToSynthesize).ConfigureAwait(false);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                ttsStatus = "✅ Speech synthesis completed";
                
                // Save audio for debugging if enabled
                if (saveAudioForDebug)
                {
                    SaveAudioDataToResources(result.AudioData, textToSynthesize);
                }
                
                // Queue audio data for main thread playback
                lock (audioUpdateLock)
                {
                    pendingAudioPlayback.Enqueue((result.AudioData, textToSynthesize));
                }
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                ttsStatus = $"❌ TTS canceled: {cancellation.Reason}";
                if (cancellation.Reason == CancellationReason.Error)
                {
                    ttsStatus += $" - {cancellation.ErrorDetails}";
                }
                Debug.LogError($"TTSManager: TTS canceled: {cancellation.Reason} - {cancellation.ErrorDetails}");
            }
            
            waitingForTTS = false;
        }
        catch (Exception e)
        {
            ttsStatus = $"❌ TTS error: {e.Message}";
            waitingForTTS = false;
            Debug.LogError($"TTSManager: TTS error: {e}");
        }
    }
    
    private IEnumerator SynthesizeTextCoroutine()
    {
        var task = SynthesizeTextInternalAsync();
        
        while (!task.IsCompleted)
        {
            yield return null;
        }
        
        if (task.IsFaulted)
        {
            Debug.LogError($"TTSManager: Coroutine synthesis failed: {task.Exception?.GetBaseException()?.Message}");
        }
    }
    
    private void ProcessPendingAudioPlayback()
    {
        // Process all queued audio playback on the main thread
        lock (audioUpdateLock)
        {
            while (pendingAudioPlayback.Count > 0)
            {
                var (audioData, text) = pendingAudioPlayback.Dequeue();
                PlayTTSAudioThroughUnity(audioData, text);
            }
        }
    }
    
    private void PlayTTSAudioThroughUnity(byte[] audioData, string text)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("TTSManager: AudioSource not assigned! Cannot play TTS audio.");
            ttsStatus = "⚠️ AudioSource not assigned";
            return;
        }

        if (audioData == null || audioData.Length == 0)
        {
            Debug.LogError("TTSManager: No audio data received from Azure TTS!");
            ttsStatus = "❌ No audio data received";
            return;
        }

        try
        {
            // Convert Azure audio data (WAV format) to Unity AudioClip
            AudioClip ttsClip = ConvertWavBytesToAudioClip(audioData, $"TTS_{DateTime.Now:HHmmss}");
            
            if (ttsClip != null)
            {
                // Play through Unity AudioSource (this will work with SALSA)
                audioSource.clip = ttsClip;
                audioSource.Play();
                
                Debug.Log($"TTSManager: Playing '{text}' ({ttsClip.length:F2}s)");
                ttsStatus = $"▶️ Playing: {text} ({ttsClip.length:F2}s)";
            }
            else
            {
                Debug.LogError("TTSManager: Failed to convert TTS audio data to AudioClip");
                ttsStatus = "❌ Failed to convert audio data";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"TTSManager: Error playing TTS audio: {e.Message}");
            ttsStatus = $"❌ Audio playback error: {e.Message}";
        }
    }
    
    private AudioClip ConvertWavBytesToAudioClip(byte[] wavBytes, string clipName)
    {
        try
        {
            if (wavBytes.Length < 44)
            {
                Debug.LogError("TTSManager: WAV data too short - missing header");
                return null;
            }
            
            // Parse WAV header to get audio properties
            int channels = System.BitConverter.ToInt16(wavBytes, 22);
            int sampleRate = System.BitConverter.ToInt32(wavBytes, 24);
            int bitsPerSample = System.BitConverter.ToInt16(wavBytes, 34);
            
            if (bitsPerSample != 16)
            {
                Debug.LogError($"TTSManager: Unsupported bit depth: {bitsPerSample}. Expected 16-bit.");
                return null;
            }
            
            // Find data chunk (skip header)
            int dataStart = 44; // Standard WAV header size
            int dataSize = wavBytes.Length - dataStart;
            
            // Convert 16-bit PCM to float samples
            int sampleCount = dataSize / (bitsPerSample / 8);
            float[] samples = new float[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                int byteIndex = dataStart + (i * 2);
                if (byteIndex + 1 < wavBytes.Length)
                {
                    short sample16 = System.BitConverter.ToInt16(wavBytes, byteIndex);
                    samples[i] = sample16 / 32768f; // Convert to -1.0 to 1.0 range
                }
            }
            
            // Create Unity AudioClip
            AudioClip clip = AudioClip.Create(clipName, sampleCount / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            
            return clip;
        }
        catch (Exception e)
        {
            Debug.LogError($"TTSManager: Error converting WAV bytes to AudioClip: {e.Message}");
            return null;
        }
    }
    
    // ========================================
    // HELPER METHODS
    // ========================================
    
    private List<string> GetVoiceOptions()
    {
        return voiceOptions;
    }
    
    /// <summary>
    /// Save audio data to Resources folder for debugging
    /// </summary>
    /// <param name="audioData">Raw audio data from Azure TTS</param>
    /// <param name="text">Text that was synthesized</param>
    private void SaveAudioDataToResources(byte[] audioData, string text)
    {
        try
        {
            // Ensure Resources folder exists
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }
            
            // Create TTSDebug subfolder
            string debugPath = Path.Combine(resourcesPath, "TTSDebug");
            if (!Directory.Exists(debugPath))
            {
                Directory.CreateDirectory(debugPath);
            }
            
            // Create safe filename from text and timestamp
            string safeText = SanitizeFilename(text);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"TTS_{timestamp}_{safeText}.wav";
            string fullPath = Path.Combine(debugPath, filename);
            
            // Save the audio data
            File.WriteAllBytes(fullPath, audioData);
            
            Debug.Log($"TTSManager: Audio saved to {fullPath}");
            Debug.Log($"TTSManager: Audio data size: {audioData.Length} bytes");
        }
        catch (Exception e)
        {
            Debug.LogError($"TTSManager: Failed to save audio data: {e.Message}");
        }
    }
    
    /// <summary>
    /// Sanitize text for use as filename
    /// </summary>
    /// <param name="text">Text to sanitize</param>
    /// <returns>Safe filename string</returns>
    private string SanitizeFilename(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "empty";
            
        // Remove invalid characters and limit length
        string sanitized = text;
        char[] invalidChars = Path.GetInvalidFileNameChars();
        
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        
        // Replace spaces and special characters
        sanitized = sanitized.Replace(' ', '_')
                             .Replace('.', '_')
                             .Replace(',', '_')
                             .Replace('!', '_')
                             .Replace('?', '_');
        
        // Limit length
        if (sanitized.Length > 30)
        {
            sanitized = sanitized.Substring(0, 30);
        }
        
        return sanitized;
    }
    
    // ========================================
    // CLEANUP
    // ========================================
    
    private void OnDestroy()
    {
        // Clean up TTS resources
        if (speechSynthesizer != null)
        {
            speechSynthesizer.Dispose();
            speechSynthesizer = null;
        }
        
        if (speechConfig != null)
        {
            speechConfig = null;
        }
    }
} 