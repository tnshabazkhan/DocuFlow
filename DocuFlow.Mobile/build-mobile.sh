#!/bin/bash

# DocuFlow Mobile Executable Build Assistant
# This script guides you through generating native APK (Android) and IPA (iOS) files using Expo Application Services (EAS).

# Colors for terminal output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

clear
echo -e "${BLUE}===================================================================${NC}"
echo -e "${CYAN}             DocuFlow Mobile Executable Build Assistant             ${NC}"
echo -e "${BLUE}===================================================================${NC}"
echo ""

# Ensure we are in the mobile directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# 1. Check for Node.js
if ! command -v node &> /dev/null; then
    echo -e "${RED}[ERROR] Node.js is not installed. Please install Node.js to continue.${NC}"
    exit 1
fi

# 2. Check for EAS CLI (we run it via npx to avoid global install issues)
echo -e "${BLUE}[1/5] Checking Expo Application Services (EAS) CLI...${NC}"
EAS_VERSION=$(npx eas-cli --version 2>/dev/null)
if [ $? -eq 0 ]; then
    echo -e "${GREEN}[OK] EAS CLI is ready: ${EAS_VERSION}${NC}"
else
    echo -e "${YELLOW}[WARN] EAS CLI is not globally installed. We will run it via 'npx'.${NC}"
fi

# 3. Check EAS Login Status
echo -e "\n${BLUE}[2/5] Checking Expo Login Status...${NC}"
WHOAMI=$(npx eas-cli whoami 2>/dev/null)
if [[ $WHOAMI == *"Not logged in"* || -z "$WHOAMI" ]]; then
    echo -e "${YELLOW}[!] You are not logged into your Expo account.${NC}"
    echo -e "${CYAN}Please log in or register now. A terminal prompt will appear below:${NC}"
    echo ""
    npx eas-cli login
    if [ $? -ne 0 ]; then
        echo -e "\n${RED}[ERROR] Expo login failed. You must log in to build mobile executables via EAS.${NC}"
        exit 1
    fi
else
    echo -e "${GREEN}[OK] Logged in as: ${WHOAMI}${NC}"
fi

# 4. Check if Project is Initialized on EAS
echo -e "\n${BLUE}[3/5] Checking Expo Project Setup...${NC}"
# If app.json doesn't contain a projectId, configure it
if ! grep -q "projectId" app.json; then
    echo -e "${YELLOW}[!] Expo Project ID not found in app.json. Initializing...${NC}"
    npx eas-cli project:init
    if [ $? -ne 0 ]; then
        echo -e "${RED}[ERROR] Failed to initialize project on Expo. Make sure you have permission or choose an existing project.${NC}"
        exit 1
    fi
    echo -e "${GREEN}[OK] Project initialized and linked successfully!${NC}"
else
    echo -e "${GREEN}[OK] Project is already linked to Expo.${NC}"
fi

# 5. Build Options
echo -e "\n${BLUE}[4/5] Select build platform & configuration:${NC}"
echo -e "1) ${GREEN}Android APK${NC} (For direct testing/installation on your device - Recommended)"
echo -e "2) ${YELLOW}Android AAB${NC} (For uploading to Google Play Store)"
echo -e "3) ${CYAN}iOS Simulator${NC} (For local testing on Mac simulator)"
echo -e "4) ${RED}iOS App Store / TestFlight${NC} (Requires Apple Developer Account)"
echo -e "5) Exit"
echo -n "Choose an option (1-5): "
read -r choice

case $choice in
    1)
        echo -e "\n${BLUE}[5/5] Building Android APK (Preview Profile)...${NC}"
        npx eas-cli build --platform android --profile preview
        ;;
    2)
        echo -e "\n${BLUE}[5/5] Building Android AAB (Production Profile)...${NC}"
        npx eas-cli build --platform android --profile production
        ;;
    3)
        echo -e "\n${BLUE}[5/5] Building iOS Simulator App (Preview Profile)...${NC}"
        npx eas-cli build --platform ios --profile preview
        ;;
    4)
        echo -e "\n${BLUE}[5/5] Building iOS IPA (Production Profile)...${NC}"
        npx eas-cli build --platform ios --profile production
        ;;
    5)
        echo -e "\n${GREEN}Build cancelled. Have a great day!${NC}"
        exit 0
        ;;
    *)
        echo -e "\n${RED}[ERROR] Invalid option selected.${NC}"
        exit 1
        ;;
esac

if [ $? -eq 0 ]; then
    echo -e "\n${GREEN}===================================================================${NC}"
    echo -e "${GREEN}✓ Build command triggered successfully!${NC}"
    echo -e "${CYAN}You can monitor your build progress directly in the terminal above or"
    echo -e "on your Expo dashboard. Once completed, a download link/QR code will be shown.${NC}"
    echo -e "${GREEN}===================================================================${NC}"
else
    echo -e "\n${RED}[ERROR] Build failed or was interrupted.${NC}"
    exit 1
fi
