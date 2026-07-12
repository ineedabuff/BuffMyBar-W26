# Winget manifest — IneedABUFF.BuffMyBar

Prêt à soumettre à [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs)
pour permettre `winget install IneedABUFF.BuffMyBar`.

## À chaque release

1. Publier `Buffmybar-W26.exe` (produit par `make-installer.bat`) comme asset de
   la release GitHub `vX.Y.Z`.
2. Mettre à jour `PackageVersion` dans les trois fichiers `.yaml`.
3. Vérifier que l'`InstallerUrl` pointe vers l'asset de cette release.
4. Calculer le SHA256 de l'exe et le coller dans `InstallerSha256` :

   ```powershell
   (Get-FileHash .\installer\Buffmybar-W26.exe -Algorithm SHA256).Hash.ToLower()
   ```

## Valider puis soumettre

```powershell
winget validate --manifest .\installer\winget
winget install --manifest .\installer\winget   # test local
```

Puis ouvrir une PR sur `microsoft/winget-pkgs` avec les fichiers rangés sous
`manifests/i/IneedABUFF/BuffMyBar/<version>/`. L'outil
[`wingetcreate`](https://github.com/microsoft/winget-create) peut automatiser
la soumission (`wingetcreate update` récupère l'URL/hash tout seul).

## Notes

- `InstallerType: inno` : winget connaît les commutateurs silencieux d'Inno Setup.
- `Scope: user` : l'installeur pose l'app sous `%LocalAppData%` (aucun UAC), ce qui
  correspond à `PrivilegesRequired=lowest` dans `Buffmybar-W26.iss`.
