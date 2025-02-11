@echo off

REM Set the name of the output file
set OUTPUT_FILE=combined.txt

REM Check if the old file exists and delete it
if exist "%OUTPUT_FILE%" del "%OUTPUT_FILE%"

REM Loop through all .ts files in all subfolders
for /r %%f in (*.ts) do (
    echo Processing %%f...
    type "%%f" >> "%OUTPUT_FILE%"
    echo. >> "%OUTPUT_FILE%" 
)

REM Completion notification
if exist "%OUTPUT_FILE%" (
    echo Combining completed. Content saved to "%OUTPUT_FILE%".
) else (
    echo Error: Files not found or combining failed.
)

pause