# Project: Apolo Bill 

## Application Goal

The Apolo Bill app is a desktop application built with WinUI 3 that helps freelancers and small business owners manage their clients, services and invoices. This application is an alternative for those who uses Excel and strugle with consistency and organization. 

## Project Organization
* `Apolo/Views/`: Contains the XAML pages for the application.
* `Apolo/Controls/`: Contains reusable components for the UI, e.g. form dialogs.
* `Apolo/Themes/`: Contains reusable styles for the UI.
* `Apolo/Strings/`: Contains the text resources for the different languages used in the UI.
* `Apolo/Services/`: Contains the internal services for the application.
* `Apolo/Converters/`: Contains converters function used to convert view model data for the UI.
* `Apolo/App.xaml.cs`: The entry point of the application.
* `Apolo/MainWindow.xaml`: The main window of the application.
* `CSV/`: Contains CSV reader and writer used to export/import the database/archive to/from CSV files.
* `../Apolo.Wiki/`: Contains the Wiki for the application.
* `Models/`: Contains the models definitions.
* `Repository/`: Contains the database (SQLite) implementation.
* `ViewModels/`: Contains the view models that connects the UI with the database.
* `PDF/`: Contains the PDF writer to generate business proposal, invoices or tickets.
* `Apolo.sln`: The solution file for the application.
* `Apolo.Tests.CSV`: Unit tests for CSV library.
* `Apolo.Tests.PDF`: Unit tests for PDF library.
* `Apolo.Tests.Data`: Unit tests for Repository library.
* `Apolo.Tests.ViewModels`: Unit tests for ViewModels library.
* `Apolo.Tests.Models`: Unit tests for Models library.

## Architecture & Tech Stack
Windows App SDK is the development platform for this application.
SQLite is used as the database for this application.
.NET 8 is used as the runtime for this application.
MVVM is used as the architectural pattern for this application.
MVVM Community Toolkit is used as the MVVM framework for this application.
C# is used as the programming language for this application.
XAML is used as the UI language for this application.
CSVHelper is used as the CSV reader and writer for this application.
QuestPDF is used as the PDF writer for this application.

## Code Guidelines
* Write clean, readable, and maintainable code.
* Always prefer using standard librearies over 3rd party libraries. If a new 3rd party library is needed, ask for permission before adding it.
* Make sure code is consistent with the existing code.
* Prioritize **simplicity over cleverness** and prefer **explicit code over magic**.

## Models
* Service: core of the application, it contains the business offer for the user.
* Payer: is one of the 2 roles the client can have, it is the one who pays the bill. A payer can have associated multiple clients.
* Client/Student: is one of the 2 roles the client can have, it is the one who receives the service. A client can have only one payer associated.
* Lesson/Session (both name can be used for the same thing): an instance of a service that has been delivered.
* Specification: a preloaded description of a lesson, it is used to create lessons with the same characteristics.
* Billing document: is a group of lessons/sessions associated to a payer. Can be a ticket (simplified invoice) or a invoice.