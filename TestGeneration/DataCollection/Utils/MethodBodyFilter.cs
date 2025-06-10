using System.Text.RegularExpressions;

namespace DataCollection.Utils
{
    public static class MethodBodyFilter
    {
        public static bool HasRealCode(string methodBody)
        {
            if (string.IsNullOrWhiteSpace(methodBody))
                return false;

            var noSingleLineComments = Regex.Replace(methodBody, @"//.*", "");

            var noComments = Regex.Replace(noSingleLineComments, @"/\*.*?\*/", "", RegexOptions.Singleline);

            var match = Regex.Match(noComments, @"\{(.*)\}", RegexOptions.Singleline);
            if (!match.Success)
                return false;

            var innerContent = match.Groups[1].Value;

            return !string.IsNullOrWhiteSpace(innerContent);
        }
    }
}