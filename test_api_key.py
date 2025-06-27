#!/usr/bin/env python3
import anthropic
import sys

def test_api_key():
    # Your API key
    API_KEY = "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"
    
    print("🔧 Testing Claude API Connection")
    print("=" * 40)
    print(f"API Key: {API_KEY[:20]}...{API_KEY[-10:]}")
    print(f"API Key Length: {len(API_KEY)}")
    print()
    
    try:
        # Create client
        print("📡 Creating Anthropic client...")
        client = anthropic.Anthropic(api_key=API_KEY)
        print("✅ Client created successfully")
        
        # Test with the latest Claude model
        print("\n🧪 Testing API call...")
        response = client.messages.create(
            model="claude-3-5-sonnet-20241022",
            max_tokens=100,
            messages=[
                {"role": "user", "content": "Hello! Please respond with just 'Hello from Claude!' to confirm the connection works."}
            ]
        )
        
        print("✅ API call successful!")
        print(f"Response: {response.content[0].text}")
        return True
        
    except anthropic.AuthenticationError as e:
        print(f"❌ Authentication Error: {e}")
        print("\nPossible causes:")
        print("1. API key is invalid or expired")
        print("2. API key doesn't have the right permissions")
        print("3. Account billing/credits issue")
        return False
        
    except anthropic.PermissionDeniedError as e:
        print(f"❌ Permission Error: {e}")
        print("You may need to add credits to your Anthropic account")
        return False
        
    except Exception as e:
        print(f"❌ Unexpected Error: {e}")
        print(f"Error type: {type(e).__name__}")
        return False

if __name__ == "__main__":
    success = test_api_key()
    if success:
        print("\n🎉 Your API key is working! You can now use Claude.")
    else:
        print("\n❌ API key test failed. Please check:")
        print("1. Go to https://console.anthropic.com")
        print("2. Check your API keys section")
        print("3. Make sure you have credits/billing set up")
        print("4. Try creating a new API key if needed") 