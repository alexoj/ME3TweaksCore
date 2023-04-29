$projectRoot = (Get-Item $PSScriptRoot).parent.FullName
$buildRoot = Join-Path -Path $projectRoot -ChildPath "Build"
$lzmaExe = Join-Path -Path $projectRoot -ChildPath "Build" | Join-Path -ChildPath "lzma.exe"
$localizationDir = Join-Path -Path $projectRoot -ChildPath "Localization" | Join-Path -ChildPath "Dictionaries"
$filesToCompress = Get-ChildItem $localizationDir -Filter *.xaml 


foreach ($xaml in $filesToCompress){
    $hashFile = Join-Path -Path $buildRoot -ChildPath "$($xaml.Name).hash"
    $needsCompiled = $true

    if ($null -ne $env:TF_BUILD)
    {
        # Azure should always compile these 
    } 
    elseif (Test-Path $hashFile)
    {
        # Local builds can skip compilation if hash file is up to date
        $hashLast = Get-Content $hashFile
        $currentHash = Get-FileHash $xaml.FullName -Algorithm SHA256
        $needsCompiled = $hashLast -ne $currentHash.Hash;
        # If hash file exists we can check if it doesn't need compiled. If it doesn't, it will always need compiled
        if ($needsCompiled -eq $false)
        {
            Write-Output "Skipping compression of localization file $($xaml.Name) as hash is already up to date"
            continue # Skip this compilation
        }
    }

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

    $currentHash = Get-FileHash $xaml.FullName -Algorithm SHA256
    Set-Content -Path $hashFile -Value $currentHash.Hash
}
