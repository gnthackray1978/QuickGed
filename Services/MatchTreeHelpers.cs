using System.Text.RegularExpressions;

namespace QuickGed.Services;

public class MatchTreeHelpers
{


        public static DateTime ExtractDate(object? originalText)
    {
        DateTime dt = DateTime.Today;

        if (originalText == null) return dt;

        var text = originalText.ToString();
        if (string.IsNullOrEmpty(text)) return dt;

        var parts = text.Split('/');
        if (parts.Length < 3) return dt;

        if (int.TryParse(parts[1], out int day) &&
            int.TryParse(parts[0], out int month) &&
            int.TryParse(parts[2], out int year))
        {
            try
            {
                dt = new DateTime(year, month, day);
            }
            catch (ArgumentOutOfRangeException) { }
        }

        return dt;

    }

    public static int ExtractYear(object? originalText)
    {

        if (originalText == null) return 0;

        var parsedText = originalText.ToString();
        if (string.IsNullOrEmpty(parsedText)) return 0;

        Regex regex = new Regex(@"\d\d\d\d");
        var v = regex.Match(parsedText);
        
        if (v.Success)
        {
            string anyoString = v.Groups[0].Value;
            return int.TryParse(anyoString, out int year) ? year : 0;
        }
        
        return 0;

    }

    public static double ExtractDouble(object? originalText)
    {
        double retVal = 0;

        if (originalText == null) return retVal;

        var parsedText = originalText.ToString();

        if (parsedText != null)
        {
            double.TryParse(parsedText, out retVal);
        }

        return retVal;
    }

    public static int ExtractInt(object? originalText)
    {
        int retVal = 0;

        if (originalText == null) return retVal;

        var parsedText = originalText.ToString();

        if (parsedText != null)
        {
            int.TryParse(parsedText, out retVal);
        }

        return retVal;
    }

    public static long ExtractLong(object? originalText)
    {
        long retVal = 0;

        if (originalText == null) return retVal;

        var parsedText = originalText.ToString();

        if (parsedText != null)
        {
            long.TryParse(parsedText, out retVal);
        }

        return retVal;
    }

    public static bool ExtractBool(object? originalText)
    {
        bool retVal = false;

        if (originalText == null) return false;

        var parsedText = originalText.ToString();

        if (parsedText != null)
        {
            bool.TryParse(parsedText, out retVal);
        }

        return retVal;
    }

    public static Guid ExtractGuid(object? originalText)
    {
        Guid retVal = Guid.Empty;

        if (originalText == null) return retVal;

        var parsedText = originalText.ToString();

        if (parsedText != null)
        {
            Guid.TryParse(parsedText, out retVal);
        }

        return retVal;
    }
}