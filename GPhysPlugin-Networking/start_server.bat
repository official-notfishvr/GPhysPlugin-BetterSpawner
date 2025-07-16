@echo off
echo Starting GPhys Networking Server...
echo.
echo Make sure you have Python installed and dependencies installed:
echo pip install -r server/requirements.txt
echo.
cd server
python app.py
pause 