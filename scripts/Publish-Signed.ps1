param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$CertificateThumbprint,
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$TimestampUrl = 'http://timestamp.digicert.com'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$certificate = Get-Item "Cert:\CurrentUser\My\$CertificateThumbprint"
if (-not $certificate.HasPrivateKey) { throw '코드 서명 인증서에 개인 키가 없습니다.' }
if ($certificate.NotAfter -le (Get-Date)) { throw '코드 서명 인증서가 만료되었습니다.' }
if ($certificate.PublicKey.Oid.FriendlyName -notmatch 'RSA') { throw 'Smart App Control용 RSA 인증서가 필요합니다.' }
$codeSigningOid = '1.3.6.1.5.5.7.3.3'
$enhancedKeyUsage = $certificate.Extensions | Where-Object { $_.Oid.Value -eq '2.5.29.37' } | Select-Object -First 1
if (-not $enhancedKeyUsage -or -not ($enhancedKeyUsage.EnhancedKeyUsages | Where-Object Value -eq $codeSigningOid)) {
    throw "인증서에 Code Signing EKU($codeSigningOid)가 없습니다."
}

$signTool = Get-ChildItem 'C:\Program Files (x86)\Windows Kits\10\bin' -Recurse -Filter signtool.exe |
    Where-Object FullName -Match '\\x64\\signtool\.exe$' | Sort-Object FullName | Select-Object -Last 1
if (-not $signTool) { throw 'Windows SDK의 x64 signtool.exe를 찾지 못했습니다.' }

$output = Join-Path $repositoryRoot "artifacts\publish\signed-$RuntimeIdentifier"
dotnet publish (Join-Path $repositoryRoot 'src\Teyemer.App\Teyemer.App.csproj') -c Release -r $RuntimeIdentifier `
    --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None -p:DebugSymbols=false -o $output
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 실패: $LASTEXITCODE" }

$executable = Join-Path $output 'Teyemer.App.exe'
& $signTool.FullName sign /sha1 $CertificateThumbprint /fd SHA256 /tr $TimestampUrl /td SHA256 $executable
if ($LASTEXITCODE -ne 0) { throw "SignTool 서명 실패: $LASTEXITCODE" }
& $signTool.FullName verify /pa /v $executable
if ($LASTEXITCODE -ne 0) { throw "SignTool 검증 실패: $LASTEXITCODE" }

Write-Host "서명된 배포 파일: $executable"
