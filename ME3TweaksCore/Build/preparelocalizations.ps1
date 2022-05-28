$projectRoot = (Get-Item $PSScriptRoot).parent.FullName
$lzmaExe = Join-Path -Path $projectRoot -ChildPath "Build" | Join-Path -ChildPath "lzma.exe"
$localizationDir = Join-Path -Path $projectRoot -ChildPath "Localization" | Join-Path -ChildPath "Dictionaries"
$filesToCompress = Get-ChildItem $localizationDir -Filter *.xaml 


foreach ($xaml in $filesToCompress){
    $inname = "`"" + $xaml.FullName + "`""
    $inname
    $outname = "`"" + $xaml.FullName + ".lzma`""
    $outname
    $processOptions = @{
        FilePath = $lzmaExe
        Wait = $true
        NoNewWindow = $true
        ArgumentList = "e", $inname, $outname
    }
    $processOptions.FilePath
    Start-Process @processOptions
}
