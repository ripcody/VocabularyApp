using Microsoft.AspNetCore.Mvc;
using VocabularyApp.WebApi.DTOs;
using VocabularyApp.WebApi.Services;

namespace VocabularyApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WordsController : ControllerBase
{
    private readonly IWordService _wordService;
    private readonly ILogger<WordsController> _logger;

    public WordsController(IWordService wordService, ILogger<WordsController> logger)
    {
        // This is a comment
        _wordService = wordService;
        _logger = logger;
    }

    /// <summary>
    /// Looks up a word definition. Checks local database first, then external dictionary API if needed.
    /// </summary>
    /// <param name="word">The word to look up</param>
    /// <returns>Word definition with parts of speech and examples</returns>
    /// <response code="200">Word found successfully</response>
    /// <response code="404">Word not found in dictionary</response>
    /// <response code="400">Invalid word parameter</response>
    [HttpGet("lookup/{word}")]
    [ProducesResponseType(typeof(ApiResponse<WordLookupResponse>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<WordLookupResponse>>> LookupWord(string word)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                _logger.LogWarning("Word lookup called with empty word parameter");
                return BadRequest(ApiResponse<WordLookupResponse>.ErrorResult("Word parameter is required"));
            }

            if (word.Length > 100)
            {
                _logger.LogWarning("Word lookup called with overly long word: {WordLength} characters", word.Length);
                return BadRequest(ApiResponse<WordLookupResponse>.ErrorResult("Word parameter is too long"));
            }

            _logger.LogInformation("Looking up word: {Word}", word);
            var result = await _wordService.LookupWordAsync(word);

            if (result.Success)
            {
                return Ok(ApiResponse<WordLookupResponse>.SuccessResult(result));
            }
            else
            {
                return NotFound(ApiResponse<WordLookupResponse>.ErrorResult(result.ErrorMessage ?? "Word not found"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error during word lookup: {Word}", word);
            return StatusCode(500, ApiResponse<WordLookupResponse>.ErrorResult("An internal error occurred"));
        }
    }

    /// <summary>
    /// Gets a word from local cache only (no external API call)
    /// </summary>
    /// <param name="word">The word to retrieve from cache</param>
    /// <returns>Word definition if found in local database</returns>
    /// <response code="200">Word found in cache</response>
    /// <response code="404">Word not found in cache</response>
    [HttpGet("cache/{word}")]
    [ProducesResponseType(typeof(ApiResponse<WordDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 404)]
    public async Task<ActionResult<ApiResponse<WordDto>>> GetFromCache(string word)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return BadRequest(ApiResponse<WordDto>.ErrorResult("Word parameter is required"));
            }

            var result = await _wordService.GetWordFromCacheAsync(word);

            if (result != null)
            {
                return Ok(ApiResponse<WordDto>.SuccessResult(result));
            }
            else
            {
                return NotFound(ApiResponse<WordDto>.ErrorResult("Word not found in cache"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving word from cache: {Word}", word);
            return StatusCode(500, ApiResponse<WordDto>.ErrorResult("An internal error occurred"));
        }
    }

    /// <summary>
    /// Searches for words in the local database by partial text match
    /// </summary>
    /// <param name="searchTerm">Partial word to search for</param>
    /// <param name="maxResults">Maximum number of results to return (default: 50, max: 100)</param>
    /// <returns>List of matching words</returns>
    /// <response code="200">Search completed successfully</response>
    /// <response code="400">Invalid search parameters</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<WordDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse<object>), 400)]
    public async Task<ActionResult<ApiResponse<List<WordDto>>>> SearchWords(
        [FromQuery] string searchTerm,
        [FromQuery] int maxResults = 50)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(ApiResponse<List<WordDto>>.ErrorResult("Search term is required"));
            }

            if (searchTerm.Length < 2)
            {
                return BadRequest(ApiResponse<List<WordDto>>.ErrorResult("Search term must be at least 2 characters"));
            }

            if (maxResults < 1 || maxResults > 100)
            {
                return BadRequest(ApiResponse<List<WordDto>>.ErrorResult("Max results must be between 1 and 100"));
            }

            _logger.LogInformation("Searching words with term: {SearchTerm}, maxResults: {MaxResults}", searchTerm, maxResults);
            var results = await _wordService.SearchWordsAsync(searchTerm, maxResults);

            return Ok(ApiResponse<List<WordDto>>.SuccessResult(results));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching words with term: {SearchTerm}", searchTerm);
            return StatusCode(500, ApiResponse<List<WordDto>>.ErrorResult("An internal error occurred"));
        }
    }

    /// <summary>
    /// Gets word statistics for admin/analytics purposes
    /// </summary>
    /// <returns>Dictionary statistics including word counts and part-of-speech breakdown</returns>
    /// <response code="200">Statistics retrieved successfully</response>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<object>), 200)]
    public async Task<ActionResult<ApiResponse<object>>> GetStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving word statistics");
            var stats = await _wordService.GetWordStatisticsAsync();
            return Ok(ApiResponse<object>.SuccessResult(stats));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving word statistics");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An internal error occurred"));
        }
    }
}