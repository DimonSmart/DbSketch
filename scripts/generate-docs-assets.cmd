@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0generate-docs-assets.ps1" %*
