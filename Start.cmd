@echo off
cd /d "%~dp0"
if not exist "%~dp0HelenaPet.exe" (
  echo HelenaPet.exe was not found. Extract the complete ZIP before running.
  pause
  exit /b 1
)
if not exist "%~dp0assets\props.json" (
  echo The assets folder is missing. Extract the complete ZIP before running.
  pause
  exit /b 1
)
if not exist "%~dp0skins\original-no-bangs\rig.json" (
  echo The skins folder is missing. Keep all extracted files together.
  pause
  exit /b 1
)
start "" "%~dp0HelenaPet.exe"
