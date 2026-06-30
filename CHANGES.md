# CHANGES — revue d'ingénierie BuffMyBar-W26

> ⚠️ **Non compilé.** Ces modifs ont été faites sous Linux (pas de toolchain WPF).
> Lance `dotnet build BuffBar.sln -c Release` et `dotnet test` sous Windows avant de pousser.

## Fait

### 1. `.gitignore` + purge des artefacts de build
- Ajout d'un `.gitignore` .NET (couvre `bin/`, `obj/`, `publish/`, `.vs/`, et `google_token.json`).
- Les ~640 fichiers `obj/` et `bin/` ont été **supprimés du disque** dans cette archive.
- **À faire sur ton clone** pour purger le suivi distant (le disque local ne suffit pas) :
  ```bash
  git rm -r --cached --quiet $(git ls-files | grep -E '(^|/)(obj|bin)/')
  git add .gitignore
  git commit -m "chore: ignore build artifacts, purge obj/bin from tracking"
  ```

### 2. ~~HttpClient partagé~~ — rien à faire
Mon point initial était **faux** : `WeatherService`, `NetworkService` et `NewsService`
utilisent déjà un `static readonly HttpClient` initialisé une fois. Déjà propre.

### 3. Capture audio partagée (le vrai gain multi-écrans)
Avant : chaque `VisualizerWidget` (donc chaque moniteur) créait sa propre capture
loopback WASAPI + FFT → N threads MTA calculant le même spectre.
- Nouveau `Services/SharedAudioCapture.cs` : capture unique à comptage de références,
  démarrée au 1er visualiseur, arrêtée au dernier.
- `VisualizerWidget` branché dessus (`Acquire`/`Release` au lieu de `new`/`Start`/`Stop`).
- Gain : **une seule** capture + FFT quel que soit le nombre d'écrans.

### 4. Fiabilité du nettoyage (`Unloaded`)
- `VisualizerWidget` : gardes anti double-`Loaded`/double-`Unloaded` (`_capture ??=` et
  libération conditionnelle) — `Unloaded` n'est pas déterministe et peut se rejouer
  lors des recompositions, ou sur `RestartBars()` (réveil de veille).

### 5. Token Google chiffré au repos (DPAPI natif)
- Nouveau `Interop/Dpapi.cs` : `CryptProtectData`/`CryptUnprotectData` en P/Invoke sur
  `crypt32.dll`. **Pas de NuGet** (`System.Security.Cryptography.ProtectedData`
  aurait cassé le zéro-dépendance).
- `GoogleCalendarService.SaveToken/LoadToken` chiffrent désormais le refresh token,
  avec **migration douce** : un ancien `google_token.json` en clair est lu une fois
  puis ré-écrit chiffré.

### 6. CI/CD — `.github/workflows/build.yml`
- Build + test sur chaque push/PR (`windows-latest`, requis pour WPF).
- Sur tag `v*` : `dotnet publish` (single-file), zip, et release GitHub auto.

### 7. Tests — `BuffBar.Tests/`
- Projet xUnit (dev-only, n'entre pas dans l'app livrée), ajouté à `BuffBar.sln`.
- `FftTests` : sinusoïde pure → pic au bon bin, silence → énergie nulle, n≤1 → no-op.
- `InternalsVisibleTo("BuffBar.Tests")` ajouté à `BuffBar.csproj` (Fft est `internal`).

## Pas fait (volontairement — à valider en compilant)

- **Dédup de Weather/Network par moniteur** : moins coûteux que l'audio, mais sur 2+ écrans
  tu fais 2× les appels wttr.in (qui *rate-limite*). Même patron que `SharedAudioCapture`
  applicable, ou un petit `BarData` qui poll une fois et diffuse par `event`. Refactor
  transversal → je préfère le faire avec toi en compilant plutôt qu'à l'aveugle.
- **`IDisposable` généralisé + nettoyage sur `Window.Closed`** : pour rendre tout le
  cycle de vie déterministe. À faire en même temps que la dédup ci-dessus.

## Points mineurs notés
- Licence **WTFPL** : OK pour du perso ; si diffusion large, MIT/Apache-2.0 est plus
  attendu par les contributeurs. À ta guise.
- `BuffBar.csproj` `<Version>0.1.0</Version>` alors que le README parle de v2.1 —
  pense à aligner pour que les releases taguées soient cohérentes.
