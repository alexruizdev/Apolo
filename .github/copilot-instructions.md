# Copilot instructions for Apolo

## Build, test, and validation commands

- Build the full solution from the repo root with the WinUI app included:
  - `dotnet build .\Apolo.sln -c Debug -p:Platform=x64`
- Run the full automated test suite:
  - `dotnet test .\Apolo.sln -c Debug -p:Platform=x64`
- Run a single MSTest when iterating on one behavior:
  - `dotnet test .\Apolo.Tests.ViewModels\Apolo.Tests.ViewModels.csproj -c Debug -p:Platform=x64 --filter "FullyQualifiedName~LessonsViewModelTests.LoadAsync_WhenAlreadyBusy"`
- There is no dedicated repo lint command checked in. `dotnet build` is the main analyzer/compiler validation pass.

## High-level architecture

- `Apolo\` is the WinUI 3 desktop app. `App.xaml.cs` wires dependency injection, creates the EF Core contexts, enables SQLite WAL mode, and stores the main and archive databases under `ApplicationData.Current.LocalFolder` as `app.context` and `archive.context`.
- `MainWindow.xaml` hosts a top `NavigationView`; each page under `Apolo\Views\` is a thin shell that resolves its ViewModel from `Ioc.Default` and keeps code-behind focused on UI concerns such as navigation, `ContentDialog`s, and file/folder pickers.
- `ViewModels\` contains most application behavior. ViewModels use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`) and coordinate repositories, document services, and UI state.
- `Repository\` is the data access layer over EF Core + SQLite. It exposes repository interfaces plus two DbContexts: the main operational database and a separate archive database. Archive/retrieve flows move related payer/student/lesson/billing data across both contexts inside transactions.
- `Models\` holds both EF entities and the summary/option records consumed by repositories and the UI. Business rules that must stay consistent across app, export, and tests live here (for example lesson price calculation and billing number-related display data).
- `Excel\` and `PDF\` are shared document services. Settings/import/export flows call the Excel reader/writer, while billing/proposal flows generate PDFs through QuestPDF.
- Tests are split by layer: `Apolo.Tests.Models`, `Apolo.Tests.Data`, and `Apolo.Tests.ViewModels`.

## Key repository conventions

- Preserve the current layering: pages should stay thin, ViewModels should own workflow/state, repositories should own EF queries and persistence, and reusable domain rules should stay in `Models\`.
- New pages should follow the existing pattern of setting `DataContext = Ioc.Default.GetService<...ViewModel>()` in code-behind and using `x:Bind` in XAML. Shared WinUI styles live in `Apolo\Themes\Styles.xaml`.
- Read paths in repositories usually project directly to summary/option records with `AsNoTracking()` rather than returning tracked entities. Keep list screens bound to those summaries instead of EF entities.
- `BaseViewModel` and `UserProfileViewModel` define the standard UI state contract: guard operations with `IsBusy`, call `SetEnterFunction()`/`SetExitFunction(...)`, and surface user feedback through the shared `InfoBar` properties instead of introducing ad hoc status flags.
- Tests assert exact `InfoMessage` text and `InfoBarType` values in many ViewModel scenarios. If you change user-facing workflow messages or busy/error handling, update the matching ViewModel tests.
- The app uses two persistence mechanisms on purpose: operational/archive business data lives in SQLite via EF Core, while user profile/settings data is stored separately in local app settings through `IUserProfileService`.
- For user-visible shared strings that already have localization support (menu labels, common buttons, picker labels), go through the `Apolo.Services.Loc` wrapper in `Apolo\Service\Loc.cs` and `Apolo\Strings\en-US\Resources.resw` instead of hardcoding another duplicate source.
