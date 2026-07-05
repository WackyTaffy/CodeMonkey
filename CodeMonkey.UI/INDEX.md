# CodeMonkey.UI Index

> The main user interface project implementing a .NET MAUI shell with a Blazor Hybrid core.

## 📐 Interaction Map
User ➡️ **`CodeMonkey.UI`** ➡️ `CodeMonkey.UI.Rendering` ➡️ `CodeMonkey.Core`

## 🔄 Common Workflows
* **Updating a UI Feature**:
  1. Modify state logic in `ViewModels/`.
  2. Update presentation in `Components/`.
  3. Verify via MAUI build and run.

* **Adding a New Component**:
  1. Create `.razor` file in `Components/`.
  2. Define necessary data models in `Models/`.
  3. Bind to a ViewModel in `ViewModels/`.

## 📂 Directory Mappings
* **`Components/`**: Reusable Blazor UI components.
* **`Models/`**: UI-specific data models.
* **`ViewModels/`**: Logic for managing UI state.
* **`Platforms/`**: Platform-specific bootstrapper code (Android, iOS, Windows).
* **`Resources/`**: Static assets, fonts, and styles.

## 🔑 Key Files
* **`MauiProgram.cs`**: Application entry point and service registration.
* **`Main.razor`**: Root Blazor component.
* **`AppShell.xaml`**: MAUI shell layout and navigation.

## 📜 Local Rules & Conventions
* UI logic must be kept in ViewModels; components should be purely presentational.
* Use Blazor for cross-platform UI elements and MAUI for platform-native integration.
