# Code signing

Unsigned executables trigger the SmartScreen "Windows protected your PC —
unknown publisher" prompt on download/run. Signing the app and the installer
with a trusted certificate removes it (and, with a reputable certificate, builds
SmartScreen reputation).

The release workflow (`.github/workflows/release.yml`) **signs automatically as
soon as the signing secrets exist** — both `BuffBar.exe` (before it is packaged)
and the final `Buffmybar-W26.exe` installer. Without the secrets, signing steps
are skipped and the release is produced unsigned (still works, just prompts).

## Secrets to configure

In the repo: **Settings → Secrets and variables → Actions**.

| Secret | Value |
|--------|-------|
| `SIGNING_PFX_BASE64` | The signing certificate (`.pfx`) as a base64 string |
| `SIGNING_PFX_PASSWORD` | The password protecting that `.pfx` |

Encode a `.pfx` to base64:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes(".\mycert.pfx")) | Set-Content cert.b64
```

Paste the content of `cert.b64` into `SIGNING_PFX_BASE64`.

## Which certificate?

- **Self-signed** — free, but gives **no** SmartScreen benefit (users must trust
  it manually). Use it only to test that the pipeline signs correctly.
- **OV (Organization Validation)** — trusted chain; SmartScreen reputation builds
  up over downloads/time.
- **Azure Trusted Signing** *(recommended for real distribution)* — Microsoft's
  low-cost (~$10/mo) cloud signing with good SmartScreen standing. Since June 2023
  new public code-signing keys must live on certified hardware/cloud HSM, so a
  plain exportable `.pfx` is mostly limited to self-signed or pre-existing certs;
  for a brand-new public cert, prefer Azure Trusted Signing (or a cloud-HSM +
  `azuresigntool`). See <https://learn.microsoft.com/azure/trusted-signing/>.

To use Azure Trusted Signing instead of the PFX path, replace the two "Sign"
steps with `azure/trusted-signing-action` and set the Azure identity secrets it
documents; the rest of the workflow is unchanged.

## Make a self-signed cert (pipeline test only)

```powershell
$c = New-SelfSignedCertificate -Type CodeSigningCert `
  -Subject "CN=BuffMyBar Test" -CertStoreLocation Cert:\CurrentUser\My
$pw = ConvertTo-SecureString "test123" -AsPlainText -Force
Export-PfxCertificate -Cert $c -FilePath .\mycert.pfx -Password $pw
```

Then set `SIGNING_PFX_BASE64` (base64 of `mycert.pfx`) and
`SIGNING_PFX_PASSWORD=test123`. The workflow will sign; SmartScreen will still
warn (self-signed), but a valid signature confirms the plumbing works.

## Verify a signature

```powershell
Get-AuthenticodeSignature .\installer\Buffmybar-W26.exe | Format-List
```

`Status` should be `Valid`.
