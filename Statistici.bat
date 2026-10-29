@echo off
cls 
call "%~dp0ml_env\Scripts\activate"
cd /d "%~dp0"
echo Pentru vizualizarea statisticilor acceseaza: http://localhost:6006
tensorboard --logdir results
pause