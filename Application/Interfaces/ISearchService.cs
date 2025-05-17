using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Services;

public interface ISearchService
{
    Task<SearchResultDto> SearchAsync(string query, int page = 1, int pageSize = 10);
}
