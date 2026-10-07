# Antistar.Exporter

Internal tool for the Antistar team. Exports every pack in Antistar Store as a ready-to-ship .unitypackage, with the support window included. Open it from **Antistar Assets > Export Packs**.

Get it from the [Antistar listing](https://theamazingcobra.github.io/Antistar-VCC-Listing/) in VCC. It installs Antistar.Tooltip too, and removes the old exporter in `Assets/Antistar Store/Tooltip`.

## Releasing

1. Raise `version` in `Packages/com.antistar.exporter/package.json`.
2. Run **Build Release** from the Actions tab.

The listing picks the new version up within the hour.

Setup, once: add the repository variable `PACKAGE_NAME` with the value `com.antistar.exporter` (Settings > Secrets and variables > Actions > Variables).
