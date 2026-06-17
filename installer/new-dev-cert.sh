#!/usr/bin/env bash
# new-dev-cert.sh — create a self-signed code-signing certificate for development
#                   sideloading of the X271Viewer MSIX package.
#
# Usage (run once per developer machine, from an elevated Git Bash terminal):
#   ./installer/new-dev-cert.sh
#
# What it does:
#   1. Calls PowerShell's New-SelfSignedCertificate to create a cert whose
#      Subject matches the Publisher in AppxManifest.xml ("CN=Bill Oliver").
#   2. Exports the cert as installer/DevCert.pfx (password: dev).
#   3. Installs the public cert into LocalMachine\TrustedPeople so Windows
#      will accept MSIX packages signed with it (requires elevation).
#
# After running, use the printed thumbprint with build-msix.sh:
#   ./installer/build-msix.sh --thumbprint <thumbprint>
#
# To install the signed package (PowerShell):
#   Add-AppxPackage ./installer/out/X271Viewer_*.msix

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PFX_PATH="$SCRIPT_DIR/DevCert.pfx"
PFX_PATH_WIN="$(cygpath -w "$PFX_PATH")"
SUBJECT="CN=Bill Oliver"
PFX_PASS="dev"

echo "Creating self-signed certificate: $SUBJECT"

# New-SelfSignedCertificate and certificate store operations are Windows-only
# APIs; PowerShell is the portable way to reach them from a Bash script.
THUMBPRINT=$(powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "
\$cert = New-SelfSignedCertificate \`
    -Type Custom \`
    -Subject '$SUBJECT' \`
    -KeyUsage DigitalSignature \`
    -FriendlyName 'X271Viewer Dev Signing Cert' \`
    -CertStoreLocation 'Cert:\CurrentUser\My' \`
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3','2.5.29.19={text}')

\$pwd = ConvertTo-SecureString '$PFX_PASS' -AsPlainText -Force
Export-PfxCertificate -Cert \$cert -FilePath '$PFX_PATH_WIN' -Password \$pwd | Out-Null

try {
    \$store = [System.Security.Cryptography.X509Certificates.X509Store]::new(
        'TrustedPeople',
        [System.Security.Cryptography.X509Certificates.StoreLocation]::LocalMachine)
    \$store.Open('ReadWrite')
    \$store.Add(\$cert)
    \$store.Close()
} catch {
    Write-Warning 'Could not install into LocalMachine\TrustedPeople — re-run elevated.'
}

Write-Output \$cert.Thumbprint
" 2>/dev/null | tr -d '\r')

echo "Thumbprint: $THUMBPRINT"
echo "PFX exported to: $PFX_PATH (password: $PFX_PASS)"
echo ""
echo "To build and sign the MSIX:"
echo "  ./installer/build-msix.sh --thumbprint $THUMBPRINT"
echo ""
echo "To install the signed package (PowerShell):"
echo "  Add-AppxPackage ./installer/out/X271Viewer_*.msix"
