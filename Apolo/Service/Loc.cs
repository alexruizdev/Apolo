using Microsoft.Windows.ApplicationModel.Resources;

namespace Apolo.Service
{
    public static class Loc
    {
        private static readonly ResourceLoader _loader = new();

        // Get a string by key
        public static string S(string key) => _loader.GetString(key);

        // Menu
        public static string Menu_Invoicing => S("Menu/Invoicing");
        public static string Menu_Lessons => S("Menu/Lessons");
        public static string Menu_Payers => S("Menu/Payers");
        public static string Menu_Services => S("Menu/Services");
        public static string Menu_Settings => S("Menu/Settings");
        public static string Menu_Specifications => S("Menu/Specifications");
        public static string Menu_Students => S("Menu/Students");

        // Buttons
        public static string Buttons_Delete => S("Buttons/Delete");
        public static string Buttons_Cancel => S("Buttons/Cancel");

        // Boxes
        public static string Box_Payer => S("Box/Payer");

    }
}
