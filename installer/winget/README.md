# Winget manifest — IneedABUFF.BuffMyBar

Permet `winget install IneedABUFF.BuffMyBar` une fois soumis à
[microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).

## Automatisé (recommandé)

Le workflow `.github/workflows/winget.yml` soumet automatiquement la mise à jour
du manifeste à chaque release GitHub publiée (via
[winget-releaser](https://github.com/vedantmgoyal2009/winget-releaser)). Il faut
juste, une fois :

1. **Forker** `microsoft/winget-pkgs` sur le compte qui soumettra.
2. Créer un **PAT classique** (scope `public_repo`) et l'ajouter en secret de dépôt
   nommé **`WINGET_TOKEN`**.
3. `release.yml` attache déjà `Buffmybar-W26.exe` à la release → le workflow s'occupe du reste.

La **toute première** soumission d'un nouveau package peut devoir être faite à la
main (voir ci-dessous) ; ensuite le workflow maintient winget à jour tout seul.

## Soumission manuelle (première fois / secours)

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
