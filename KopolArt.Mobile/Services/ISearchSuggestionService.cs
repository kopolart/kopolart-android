using KopolArt.Mobile.Models;

namespace KopolArt.Mobile.Services
{
    public interface ISearchSuggestionService
    {
        Task<List<SearchSuggestionItem>> GetSuggestionsAsync(string term, SearchCategoryType category, CancellationToken cancellationToken = default);
    }
}
