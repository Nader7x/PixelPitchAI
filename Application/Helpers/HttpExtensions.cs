using System.Collections;
using System.Reflection;

namespace Application.Helpers;

public static class HttpExtensions
{
    /// <summary>
    ///     Converts a given object's public properties into a MultipartFormDataContent.
    ///     This method is designed for DTOs containing primitive types, strings, enums,
    ///     and collections of these types. It does not handle nested complex objects
    ///     or file streams automatically.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert.</typeparam>
    /// <param name="obj">The object instance to convert.</param>
    /// <returns>A MultipartFormDataContent containing the object's properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the input object is null.</exception>
    public static MultipartFormDataContent ToMultipartFormDataContent<T>(this T obj)
        where T : class
    {
        if (obj == null)
            throw new ArgumentNullException(nameof(obj), "Object cannot be null.");

        var formData = new MultipartFormDataContent();

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);

            switch (value)
            {
                case null:
                    continue;
                case IEnumerable enumerable when !(value is string):
                {
                    foreach (var item in enumerable)
                        if (item != null)
                            formData.Add(
                                new StringContent(item.ToString() ?? string.Empty),
                                property.Name
                            );

                    break;
                }
                default:
                    if (value is DateTime time)
                        formData.Add(
                            new StringContent(time.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")),
                            property.Name
                        );
                    formData.Add(
                        new StringContent(value.ToString() ?? string.Empty),
                        property.Name
                    );
                    break;
            }
        }

        return formData;
    }
}
