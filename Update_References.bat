rd /s /q GameData\Configuration
rd /s /q GameData\LevelEditorOutput
rd /s /q GameData\LocalizationExports
rd /s /q HotMExternal0CommonCode\src
rd /s /q HotMExternalAbstractSim\src
rd /s /q HotMExternalVisualizationCode\src

echo D | xcopy /s /y ..\..\GameData\Configuration GameData\Configuration
echo D | xcopy /s /y ..\..\GameData\LevelEditorOutput GameData\LevelEditorOutput
echo D | xcopy /s /y ..\..\GameData\LocalizationExports GameData\LocalizationExports
echo D | xcopy /s /y ..\..\CodeExternal\HotMExternal0CommonCode\src HotMExternal0CommonCode\src
echo D | xcopy /s /y ..\..\CodeExternal\HotMExternalAbstractSim\src HotMExternalAbstractSim\src
echo D | xcopy /s /y ..\..\CodeExternal\HotMExternalVisualizationCode\src HotMExternalVisualizationCode\src

PAUSE