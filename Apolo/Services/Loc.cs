using Microsoft.Windows.ApplicationModel.Resources;
using Microsoft.Windows.Globalization;

namespace Apolo.Services
{
    public class LanguageService : ILanguageService
    {
        public void ApplyLanguage(string language) => Loc.ApplyLanguage(language);
    }
    public class StringLocalizer : IStringLocalizer
    {
        public string Get(string key) => Loc.S(key);

        public string Get(string key, params object[] args)
        {
            string template = Loc.S(key);
            return string.Format(template, args);
        }
    }
    public static class Loc
    {
        private static ResourceLoader? _loader;

        private static ResourceLoader Loader
        {
            get => _loader ??= new ResourceLoader();
        }

        /// <summary>
        /// Updates the current application UI language context safely at runtime.
        /// </summary>
        /// <param name="languageCode"></param>
        public static void ApplyLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                // Fallback policy: Detect OS culture. If Spanish, use es-ES. Otherwise, fallback strictly to en-US.
                var osLang = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                languageCode = osLang == "es" ? "es-ES" : "en-US";
            }

            ApplicationLanguages.PrimaryLanguageOverride = languageCode;

            _loader = null;
        }

        // Get a string by key
        public static string S(string key) => Loader.GetString(key);
        public static string F(string key, params object[] args) => string.Format(S(key), args);

        // Menu
        public static string Menu_Invoicing => S("Menu/Invoicing");
        public static string Menu_Lessons => S("Menu/Lessons");
        public static string Menu_Payers => S("Menu/Payers");
        public static string Menu_Services => S("Menu/Services");
        public static string Menu_Settings => S("Menu/Settings");
        public static string Menu_Specifications => S("Menu/Specifications");
        public static string Menu_Students => S("Menu/Students");
        public static string Menu_Dashboard => S("Menu/Dashboard");
        public static string Menu_Proposal => S("Menu/Proposal");

        

        // Buttons
        public static string Buttons_Delete => S("Buttons/Delete/Label");
        public static string Buttons_Cancel => S("Buttons/Cancel/Content");
        public static string Buttons_Create => S("Buttons/Create");
        public static string Buttons_Edit => S("Buttons/Edit");
        public static string Buttons_Save => S("Buttons/Save/Label");
        public static string Buttons_Understood => S("Buttons/Understood");
        public static string Buttons_Archive => S("Buttons/Archive");
        public static string Buttons_Retrieve => S("Buttons/Retrieve");
        public static string Buttons_PickFile => S("Buttons/PickFile");
        public static string Buttons_PickFolder => S("Buttons/PickFolder");
        public static string Buttons_Search => S("Buttons/Search");

        // Common labels
        public static string Common_AppTitle => S("Common/AppTitle");
        public static string Common_InvoiceManager => S("Common/InvoiceManager");
        public static string Common_Date => S("Common/Date/Text");

        public static string Common_FirstName => S("Common/FirstName");
        public static string Common_LastName => S("Common/LastName");
        public static string Common_Address => S("Common/Address/Header");
        public static string Common_ZipCode => S("Common/ZipCode/Header");
        public static string Common_City => S("Common/City/Header");
        public static string Common_Name => S("Common/Name/Header");
        public static string Common_Tip => S("Common/Tip");
        public static string Common_Notes => S("Common/Notes");
        public static string Common_Price => S("Common/Price/Text");
        public static string Common_PerHour => S("Common/PerHour/Text");
        public static string Common_Payer => S("Common/Payer/Header");


        // Settings
        public static string Settings_ArchiveOldData => S("Settings/ArchiveOldData");
        public static string Settings_SelectPayersArchive => S("Settings/SelectPayersArchive");

        // Action phrases
        public static string Action_DeleteDatabase => S("Actions/DeleteDatabase");
        public static string Action_DeleteArchive => S("Actions/DeleteArchive");
        public static string Action_RemoveSelectedLessons => S("Actions/RemoveSelectedLessons");
        public static string Action_DeleteBill => S("Actions/DeleteBill");
        public static string Action_DeletePayer => S("Actions/DeletePayer");
        public static string Action_DeleteService => S("Actions/DeleteService");
        public static string Action_DeleteStudent => S("Actions/DeleteStudent");
        public static string Action_DeleteSpecification => S("Actions/DeleteSpecification");
        public static string Action_DeleteLesson => S("Actions/DeleteLesson");

        // App / dialogs
        public static string App_UnexpectedErrorTitle => S("App/UnexpectedErrorTitle");

    }
}
