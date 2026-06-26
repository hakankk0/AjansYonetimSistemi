Write-Host "====================================="
Write-Host "     AJANS YONETIM SISTEMI"
Write-Host "     Otomatik Cikti (Publish) Araci"
Write-Host "====================================="
Write-Host ""
Write-Host "Islem 1/3: Derleme temizleniyor..." -ForegroundColor Yellow
dotnet clean AjansYonetim.csproj

$deskPath = [Environment]::GetFolderPath("Desktop")
$outDir = "$deskPath\AjansYonetim_Yayin"

if (Test-Path $outDir) {
    Remove-Item -Path $outDir -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

Write-Host "Islem 2/3: Uygulama Self-Contained Olarak Paketleniyor (Lutfen bekleyin...)" -ForegroundColor Yellow
# Self-Contained ve Single File (tek exe dosyası) olarak derleniyor.
dotnet publish AjansYonetim.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o $outDir

Write-Host ""
Write-Host "====================================="
Write-Host "    PUBLISH ISLEMI TAMAMLANDI !      " -ForegroundColor Green
Write-Host "====================================="
Write-Host "Hazir olan Exe Dosyanizin Konumu: " -ForegroundColor Cyan
Write-Host $outDir
Write-Host ""
Write-Host "Bu klasordeki 'AjansYonetim.exe' dosyasini Inno Setup ile kurulum (.exe) dosyasina cevirebilirsiniz."
Write-Host ""
Write-Host "Kapatmak icin bir tusa basin..."
$host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
