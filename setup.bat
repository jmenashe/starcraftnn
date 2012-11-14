STARCRAFTDIR=C:\Program Files (x86)\StarCraft
cd "%STARCRAFTDIR%\bwapi-data"
rm bwapi.ini
mklink bwapi.ini "%USERPROFILE%\starcraftnn\bwapi.ini"
cd "%STARCRAFTDIR%"
mklink /D training "%USERPROFILE%\starcraftnn\maps"
REM       Now install boost::filesystem for vc++ 9.0