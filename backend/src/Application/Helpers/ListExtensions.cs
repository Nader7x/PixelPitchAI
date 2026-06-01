namespace Application.Helpers;

public static class ListExtensions
{
    public static void Deconstruct<T>(this IList<T>? list, out T? item1, out T? item2)
    {
        if (list is null)
        {
            item1 = default;
            item2 = default;
            return;
        }
        switch (list.Count)
        {
            case 2:
                item1 = list[0];
                item2 = list[1];
                break;
            case 1:
                item1 = list[0];
                item2 = default;
                break;
            default:
                item1 = default;
                item2 = default;
                break;
        }
    }
}
