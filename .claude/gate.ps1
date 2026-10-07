$in = [Console]::In.ReadToEnd() | ConvertFrom-Json
Set-Location $in.cwd  # donde trabaja Claude
 
$out = dotnet test --nologo 2>&1
if ($LASTEXITCODE -eq 0) { exit 0 }
 
# En rojo: exit 2 impide que Claude termine
$fallas = $out | Select-String "Failed|error CS" |
  Select-Object -First 15
[Console]::Error.WriteLine(
  "dotnet test en rojo. Corrige y vuelve a probar:")
$fallas | ForEach-Object {
  [Console]::Error.WriteLine($_.Line) }
exit 2
