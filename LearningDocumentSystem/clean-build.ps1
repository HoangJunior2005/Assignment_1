# Chay script nay trong PowerShell khi gap loi Conflicting assets site.css
$root = $PSScriptRoot
Get-Process LearningDocumentSystem.Web -ErrorAction SilentlyContinue | Stop-Process -Force

Get-ChildItem $root -Recurse -Directory -Filter obj -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
Get-ChildItem $root -Recurse -Directory -Filter bin -ErrorAction SilentlyContinue |
    Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Da xoa bin/obj. Dang build lai..."
Set-Location $root
dotnet build LearningDocumentSystem.Web
if ($LASTEXITCODE -eq 0) {
    Write-Host "Build thanh cong."
} else {
    Write-Host "Build van loi - thu dong VS, xoa thu muc copy o D:\200k neu con ton tai."
}
