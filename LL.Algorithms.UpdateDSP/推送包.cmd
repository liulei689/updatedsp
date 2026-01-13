@echo off  
setlocal  
  
:: 初始化一个变量来存储第一个找到的.nupkg文件的文件名（不包括路径）  
set "firstNupkgName="  
  
:: 遍历当前目录下所有.nupkg文件  
for %%F in (*.nupkg) do (  
    :: 如果firstNupkgName尚未设置（即为空），则设置它  
    if not defined firstNupkgName (  
        set "firstNupkgName=%%~nxF"  
        goto :found  :: 可选，但可以提高效率，特别是当有很多.nupkg文件时  
    )  
)  
  
:found  
:: 检查是否找到了文件  
if defined firstNupkgName (  
dotnet nuget push %firstNupkgName% --api-key oy2du44cs3yvj4fdvx4qaharrmy2gwmulpsxms4mlnf4fe --source https://api.nuget.org/v3/index.json
    echo Found the first .nupkg file name: %firstNupkgName%  
) else (  
    echo No .nupkg files were found.  
)  
  
:: 暂停执行以查看结果（可选）  
pause  
  :: 删除当前目录下所有.nupkg文件  
del *.nupkg  
  
:: 如果需要确认是否真的删除了文件，可以使用 /P 参数，但这里我们直接删除  
:: 注意：使用 /P 参数会要求用户对每个文件都进行确认，这在实际操作中可能不太方便  
:: del /P *.nupkg  
  
:: 验证是否还有.nupkg文件（可选）  
dir *.nupkg > nul 2>&1  
if %errorlevel% equ 1 (  
    echo All .nupkg files in the current directory have been deleted.  
) else (  
    echo There are still .nupkg files in the current directory. This should not happen.  
) 
pause  
endlocal