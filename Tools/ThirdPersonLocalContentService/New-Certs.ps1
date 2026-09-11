$ErrorActionPreference = 'Stop'

$toolsRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$certsRoot = Join-Path $toolsRoot 'certs'
New-Item -ItemType Directory -Force -Path $certsRoot | Out-Null

$openssl = Get-Command openssl -ErrorAction SilentlyContinue
if (-not $openssl) {
    $gitOpenSsl = Join-Path $env:ProgramFiles 'Git\mingw64\bin\openssl.exe'
    if (-not (Test-Path $gitOpenSsl)) { throw 'openssl not found.' }
    $openssl = @{ Source = $gitOpenSsl }
}

$caKey = Join-Path $certsRoot 'local-root.key'
$caCrt = Join-Path $certsRoot 'local-root.crt'
$leafKey = Join-Path $certsRoot 'localhost.key'
$leafCsr = Join-Path $certsRoot 'localhost.csr'
$leafCrt = Join-Path $certsRoot 'localhost.crt'
$pfx = Join-Path $certsRoot 'localhost.pfx'
$password = 'thirdperson-local'
$extFile = Join-Path $certsRoot 'localhost.ext'

@'
basicConstraints=critical,CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1
'@ | Set-Content -Encoding ascii -Path $extFile

& $openssl.Source req -x509 -newkey rsa:2048 -nodes -keyout $caKey -out $caCrt -days 3650 -subj '/CN=ThirdPerson Local Dev CA' -addext 'basicConstraints=critical,CA:TRUE' -addext 'keyUsage=critical,keyCertSign,cRLSign'
& $openssl.Source req -new -newkey rsa:2048 -nodes -keyout $leafKey -out $leafCsr -subj '/CN=localhost'
& $openssl.Source x509 -req -in $leafCsr -CA $caCrt -CAkey $caKey -CAcreateserial -out $leafCrt -days 825 -extfile $extFile
& $openssl.Source pkcs12 -export -out $pfx -inkey $leafKey -in $leafCrt -certfile $caCrt -passout "pass:$password"

Write-Host "certificates written to $certsRoot"
Write-Host "install the CA into the machine trust store (admin):"
Write-Host "  certutil -addstore -f Root `"$caCrt`""
