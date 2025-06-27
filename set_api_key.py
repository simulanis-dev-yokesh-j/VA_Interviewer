#!/usr/bin/env python3
"""
Simple script to set Claude API key
"""

import json
from pathlib import Path

CONFIG_FILE = Path.home() / ".claude_config.json"

def set_api_key():
    print("🔧 Claude API Key Setup")
    print("=" * 30)
    print("Paste your API key below (it will be saved to ~/.claude_config.json)")
    
    # For Windows, we'll use a simple approach
    api_key = input("Enter your Anthropic API key: ").strip()
    
    if api_key:
        config = {"api_key": api_key}
        with open(CONFIG_FILE, 'w') as f:
            json.dump(config, f)
        print(f"✅ API key saved to {CONFIG_FILE}")
        print("✅ Setup complete! You can now use Claude.")
        
        # Test the setup
        print("\nTesting connection...")
        try:
            import anthropic
            client = anthropic.Anthropic(api_key=api_key)
            response = client.messages.create(
                model="claude-3-5-sonnet-20241022",
                max_tokens=50,
                messages=[{"role": "user", "content": "Say hello in one sentence"}]
            )
            print("✅ Connection successful!")
            print(f"Claude says: {response.content[0].text}")
        except Exception as e:
            print(f"❌ Connection failed: {e}")
    else:
        print("❌ No API key provided.")

if __name__ == "__main__":
    set_api_key() 