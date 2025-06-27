#!/usr/bin/env python3
import json
from pathlib import Path
import anthropic

# The API key provided by the user
API_KEY = "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"

CONFIG_FILE = Path.home() / ".claude_config.json"

def setup_api_key():
    print("🔧 Setting up Claude API key...")
    
    # Save to config file
    config = {"api_key": API_KEY}
    with open(CONFIG_FILE, 'w') as f:
        json.dump(config, f)
    print(f"✅ API key saved to {CONFIG_FILE}")
    
    # Test the connection
    print("\n🧪 Testing connection...")
    try:
        client = anthropic.Anthropic(api_key=API_KEY)
        response = client.messages.create(
            model="claude-3-5-sonnet-20241022",
            max_tokens=50,
            messages=[{"role": "user", "content": "Say hello in one sentence"}]
        )
        print("✅ Connection successful!")
        print(f"Claude says: {response.content[0].text}")
        return True
    except Exception as e:
        print(f"❌ Connection failed: {e}")
        return False

if __name__ == "__main__":
    if setup_api_key():
        print("\n🎉 Setup complete! You can now use:")
        print("  python claude_chat.py \"Your message here\"")
        print("  python claude_chat.py --interactive")
    else:
        print("\n❌ Setup failed. Please check your API key.") 