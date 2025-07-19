namespace Footex.Extensions;

public static class StringExtensions
{
    /// <summary>
    ///     Checks if any of the provided string values are null, empty, or consist only of white-space characters.
    /// </summary>
    /// <param name="values">An array of string values to check.</param>
    /// <returns>True if any of the values are null or white space; otherwise, false.</returns>
    public static bool AreAnyNullOrWhiteSpace(params string[] values)
    {
        return values.Length != 0 && values.Any(string.IsNullOrWhiteSpace);
    }

    /// <summary>
    ///     Checks a collection of string values and returns a list of error messages
    ///     for any strings that are null, empty, or consist only of white-space characters.
    /// </summary>
    /// <param name="values">An array of tuples, where each tuple contains a string value and its descriptive name.</param>
    /// <returns>A list of error messages for invalid strings. Returns an empty list if all strings are valid.</returns>
    public static List<string> GetNullOrWhiteSpaceErrors(
        params (string value, string name)[] values
    )
    {
        var errors = new List<string>();

        if (values.Length == 0)
            return errors;

        errors.AddRange(
            from item in values
            where string.IsNullOrWhiteSpace(item.value)
            select $"'{item.name}' cannot be null or whitespace."
        );

        return errors;
    }
}
