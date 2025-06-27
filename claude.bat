@echo off
REM Claude CLI Launcher for Windows
REM Usage: claude "your message here" or claude --interactive

python "%~dp0claude_chat.py" %* 