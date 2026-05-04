namespace Models
{
    public static class Helper
    {
        public static string GetFullName(string firstName, string lastName) => $"{firstName} {lastName}".Trim();
    }
}
