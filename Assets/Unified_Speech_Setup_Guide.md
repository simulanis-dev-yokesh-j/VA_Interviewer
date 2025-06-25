# 🎤🔊 Unified Speech Manager Setup Guide

## Overview
The **UnifiedSpeechManager** combines both Speech-to-Text (STT) and Text-to-Speech (TTS) functionality into a single, powerful script that includes:
- ✅ Voice recording and recognition
- ✅ Text synthesis with multiple voices  
- ✅ Conversation mode (record → recognize → speak back)
- ✅ Complete UI system
- ✅ Odin Inspector testing tools

## Quick Setup (3 Steps)

### Step 1: Add the Unified Speech Manager
1. Create an empty GameObject in your scene
2. Name it "UnifiedSpeechManager"
3. Add the `UnifiedSpeechManager.cs` script to it

### Step 2: Add the UI Generator
1. Create another empty GameObject in your scene
2. Name it "SpeechUI_Generator"
3. Add the `UnifiedSpeechUI.cs` script to it
4. In the inspector, drag your UnifiedSpeechManager GameObject to the "Speech Manager" field

### Step 3: Generate the UI
1. Click the **"Create Complete UI"** button in the SpeechUI_Generator inspector
2. The complete UI will be automatically generated and connected
3. You're ready to test!

## Testing the System

### Using Odin Inspector (Development/Testing)
1. **Test Microphone**: Click "🎤 Test Permissions" and "🔄 Refresh Microphones"
2. **Initialize Services**: Click "🔧 Initialize Services" 
3. **Record Audio**: Click "Start Recording" → speak → "Stop Recording"
4. **Play Back**: Click "Play Recorded Audio" to hear your recording
5. **Recognize Speech**: Click "Process Speech Recognition" to convert to text
6. **Synthesize Speech**: Enter text and click "Speak Text"
7. **Conversation Mode**: Click "Start Conversation Mode" for automatic workflow

### Using Generated UI (Runtime)
- **🎤 Start Recording**: Toggle recording on/off
- **▶️ Play**: Playback recorded audio
- **🧠 Recognize**: Process speech recognition
- **🔊 Speak Text**: Synthesize entered text
- **💬 Conversation Mode**: Automatic record → recognize → speak cycle

## Features

### Speech Recognition (STT)
- Records audio from selected microphone
- Real-time volume level indicator
- Converts speech to text using Azure Speech Service
- Supports multiple microphones
- High-quality 16kHz recording

### Text-to-Speech (TTS)
- Multiple voice options (Indian English, US English)
- Natural-sounding neural voices
- Text input with placeholder support
- Real-time synthesis status

### Conversation Mode
- Automated workflow: Record → Recognize → Speak back
- Perfect for testing complete speech pipelines
- Visual feedback for each stage

### UI Components
- Clean, modern interface
- Real-time status updates
- Scrollable text displays
- Responsive button states
- Volume visualization

## Voice Options Available
- **en-IN-AaravNeural** (Indian English Male) - Default
- **en-IN-PrabhatNeural** (Indian English Male)
- **en-US-AriaNeural** (US English Female)
- **en-US-JennyNeural** (US English Female)
- **en-US-GuyNeural** (US English Male)
- **en-US-DavisNeural** (US English Male)

## Troubleshooting

### No Microphone Detected
1. Check "Permission Status" in Debug Information
2. Click "🎤 Test Permissions" 
3. Click "🔄 Refresh Microphones"
4. Ensure microphone is connected and working

### Speech Recognition Not Working
1. Verify "Service Status" shows "✅ Azure Speech Services initialized"
2. Click "🔧 Initialize Services" if needed
3. Check your internet connection
4. Ensure you have a valid recording (play it back first)

### Text-to-Speech Not Working
1. Check that text input is not empty
2. Verify "Service Status" is initialized
3. Try different voices from the dropdown
4. Check console for any error messages

### UI Not Working
1. Make sure UI components are connected (check Debug Information)
2. Try clearing and regenerating the UI
3. Ensure UnifiedSpeechManager is properly assigned

## Architecture Benefits

### Clean Separation
- **UnifiedSpeechManager**: Core functionality
- **UnifiedSpeechUI**: UI generation and connection
- No mixing of UI and logic code

### Development Friendly
- Odin Inspector controls for easy testing
- Detailed debug information
- Real-time status monitoring
- Console logging for troubleshooting

### Production Ready
- Complete UI system
- Error handling
- Permission management
- Memory cleanup

## Performance Notes
- Azure Speech Service requires internet connection
- Recording uses minimal CPU resources
- UI updates are optimized for real-time performance
- Memory usage is controlled with proper disposal

## Migration from Old Scripts
If you were using the separate `HelloWorld.cs` and `SSTManager.cs`:

1. **Remove old scripts** from your scene
2. **Delete old UI elements** if any
3. **Follow the Quick Setup** above
4. **All functionality is preserved** and enhanced

The unified system provides everything the old scripts had, plus:
- Better organization
- Improved UI
- Conversation mode
- Enhanced error handling
- Real-time feedback

## Advanced Usage
- Extend voice options by modifying the `voiceOptions` list
- Customize UI appearance by editing `UnifiedSpeechUI.cs` 
- Add custom conversation logic in conversation mode
- Integrate with other systems using the public methods 