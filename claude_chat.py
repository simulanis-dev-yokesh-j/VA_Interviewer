#!/usr/bin/env python3
"""
Simple Claude CLI Chat Tool
Usage: 
  python claude_chat.py "Your message here"
  python claude_chat.py --interactive  (for chat mode)
  python claude_chat.py --setup        (to set up API key)
"""

import anthropic
import sys
import os
import json
from pathlib import Path

CONFIG_FILE = Path.home() / ".claude_config.json"

def save_api_key(api_key):
    """Save API key to config file"""
    config = {"api_key": api_key}
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f)
    print(f"✅ API key saved to {CONFIG_FILE}")

def load_api_key():
    """Load API key from config file or environment"""
    # First try environment variable
    api_key = os.getenv('ANTHROPIC_API_KEY')
    if api_key:
        return api_key
    
    # Then try config file
    if CONFIG_FILE.exists():
        try:
            with open(CONFIG_FILE, 'r') as f:
                config = json.load(f)
                return config.get('api_key')
        except:
            pass
    
    return None

def setup_api_key():
    """Interactive API key setup"""
    print("🔧 Claude API Key Setup")
    print("=" * 30)
    print("1. Go to https://console.anthropic.com")
    print("2. Sign up or log in")
    print("3. Create an API key")
    print("4. Copy the API key and paste it below")
    print()
    
    api_key = input("Enter your Anthropic API key: ").strip()
    if api_key:
        save_api_key(api_key)
        print("✅ Setup complete! You can now use Claude.")
    else:
        print("❌ No API key provided.")

def chat_with_claude(message, api_key):
    """Send a message to Claude and return the response"""
    try:
        client = anthropic.Anthropic(api_key=api_key)
        
        response = client.messages.create(
            model="claude-3-5-sonnet-20241022",
            max_tokens=1000,
            messages=[
                {"role": "user", "content": message}
            ]
        )
        
        return response.content[0].text
    except Exception as e:
        return f"❌ Error: {str(e)}"

def interactive_mode(api_key):
    """Interactive chat mode"""
    print("🤖 Claude Interactive Chat")
    print("=" * 30)
    print("Type 'quit', 'exit', or 'bye' to exit")
    print("Type 'clear' to clear screen")
    print()
    
    while True:
        try:
            message = input("You: ").strip()
            
            if message.lower() in ['quit', 'exit', 'bye']:
                print("👋 Goodbye!")
                break
            elif message.lower() == 'clear':
                os.system('cls' if os.name == 'nt' else 'clear')
                continue
            elif not message:
                continue
            
            print("Claude: ", end="", flush=True)
            response = chat_with_claude(message, api_key)
            print(response)
            print()
            
        except KeyboardInterrupt:
            print("\n👋 Goodbye!")
            break

def main():
    if len(sys.argv) == 1:
        print("Usage:")
        print('  python claude_chat.py "Your message here"')
        print("  python claude_chat.py --interactive")
        print("  python claude_chat.py --setup")
        return
    
    # Handle setup command
    if sys.argv[1] == "--setup":
        setup_api_key()
        return
    
    # Load API key
    api_key = load_api_key()
    if not api_key:
        print("❌ No API key found!")
        print("Run: python claude_chat.py --setup")
        return
    
    # Handle interactive mode
    if sys.argv[1] == "--interactive":
        interactive_mode(api_key)
        return
    
    # Handle single message
    message = " ".join(sys.argv[1:])
    response = chat_with_claude(message, api_key)
    print(response)

if __name__ == "__main__":
    main() 