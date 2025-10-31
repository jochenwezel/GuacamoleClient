# GuacamoleClient – Avalonia Modul & Fehlerbehandlung für WebView-Runtime

Dieses Paket enthält den plattformübergreifenden Avalonia‑Client inkl.:
- Speichern der Start‑URL (Windows: Registry HKCU, Linux/macOS: JSON im User‑Config‑Pfad)
- Dialog zum Abfragen der URL, falls nicht konfiguriert
- Menü/Hotkey zum Zurücksetzen der Start‑URL
- **Fehlerbehandlung**, wenn die WebView‑Runtime fehlt (z. B. WebView2 unter Windows)

## Integration (Kurz)
1. Neues Projekt (z. B. `GuacamoleClient.Avalonia`) anlegen oder in ein bestehendes integrieren.
2. Die Dateien aus `src/GuacClient/` in Ihr Projekt kopieren (Namespaces ggf. anpassen).
3. Folgende NuGet‑Pakete ins `.csproj` aufnehmen:
   ```xml
   <ItemGroup>
     <PackageReference Include="Avalonia" Version="11.1.*" />
     <PackageReference Include="Avalonia.Desktop" Version="11.1.*" />
     <PackageReference Include="Avalonia.Themes.Fluent" Version="11.1.*" />
     <PackageReference Include="Avalonia.WebView" Version="11.0.*" />
     <PackageReference Include="Microsoft.Web.WebView2" Version="1.*" />
   </ItemGroup>
   ```
   > `Microsoft.Web.WebView2` wird nur unter Windows verwendet; auf Linux/macOS greift Avalonia.WebView die System‑Engine.

4. `Program.cs`/`App.axaml(.cs)` gemäß Beispiel einfügen.

## Fehlerbehandlung WebView‑Runtime
- Beim Laden der URL wird ein `try/catch` verwendet.
- Bei typischen Fehlern (z. B. fehlende **WebView2 Runtime** unter Windows, fehlendes **WebKitGTK** unter Linux)
  erscheint eine klare Meldung mit kurzer Anleitung.

## Tastatur/Bedienung
- **Strg+U**: Start‑URL zurücksetzen und neu abfragen
- **Strg+Q**: Anwendung schließen

---

## Dateien
- `src/GuacClient/Program.cs`
- `src/GuacClient/App.axaml` + `App.axaml.cs`
- `src/GuacClient/MainWindow.axaml` + `MainWindow.axaml.cs`
- `src/GuacClient/UrlInputDialog.axaml` + `UrlInputDialog.axaml.cs`
- `src/GuacClient/MessageBoxSimple.cs`
- `src/GuacClient/IStartUrlStore.cs`
- `src/GuacClient/StartUrlStoreFactory.cs`
- `src/GuacClient/WindowsRegistryStartUrlStore.cs`
- `src/GuacClient/JsonFileStartUrlStore.cs`
- `guac.ico`
