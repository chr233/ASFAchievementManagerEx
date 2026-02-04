$PROJECT_NAME = "ASFAchievementManagerEx"
$PLUGIN_NAME = "ASFAchievementManagerEx.dll"

dotnet publish $PROJECT_NAME -o ./publish/ -c Release


if (-Not (Test-Path -Path ./dist)) {
    New-Item -ItemType Directory -Path ./dist
}
else {
    Remove-Item -Path ./dist/* -Recurse -Force
}

Copy-Item -Path .\publish\$PLUGIN_NAME -Destination .\dist\ 

$dirs = Get-ChildItem -Path ./publish -Directory
foreach ($dir in $dirs) {
    $subFiles = Get-ChildItem -Path $dir.FullName -File -Filter *.resources.dll
    
    foreach ($file in $subFiles) {
        $resourceName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        $opDir = "./dist/$resourceName"
        if (-Not (Test-Path -Path $opDir)) {
            New-Item -ItemType Directory -Path $opDir
        }

        $destinationPath = ".\dist\$resourceName\$($dir.Name).dll"
        Copy-Item -Path $file -Destination $destinationPath

        Write-Output "Copy resource DLL $($file.FullName) -> $destinationPath"
    }
}

Remove-Item -Path ./publish -Recurse -Force