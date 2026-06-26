using System.Linq;

namespace FUNewsTradingSystem_MVC.Helpers;

public static class PaginationSettings
{
    public const int DefaultPageSize = 10;
    public const int MinPageSize = 5;
    public const int MaxPageSize = 100;

    public static int ValidatePageNumber(int? pageNumber) => pageNumber ?? 1;

    public static int ValidatePageSize(int? pageSize)
    {
        if (pageSize == null || pageSize < MinPageSize) return DefaultPageSize;
        if (pageSize > MaxPageSize) return MaxPageSize;
        return pageSize.Value;
    }

    public static X.PagedList.StaticPagedList<T> ToPagedList<T>(
        this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        return new X.PagedList.StaticPagedList<T>(
            source.Skip((pageNumber - 1) * pageSize).Take(pageSize),
            pageNumber, pageSize,
            source.Count());
    }
}
