# CodeMonkey.UI/ViewModels Index

> ViewModels bridging the gap between UI and Core services.

## 📐 Interaction Map
**`ViewModels`** ➡️ `Core Services` (Calls) / `Components` (Provides State)

## ☢️ Blast Radius
- **MEDIUM**: Changes here affect how the UI interacts with the core system and how state is managed.

## 🚀 Primary Entry Points
- **`ChatViewModel.cs`**: The main state controller for the chat experience.

## 📜 Local Rules & Conventions
- Implement `INotifyPropertyChanged` for data binding.
- Avoid direct references to UI components within ViewModels.
