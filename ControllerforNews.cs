using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class NewsContext : DbContext
{
    public DbSet<NewsController.Article> articles { get; set; }

    public NewsContext(DbContextOptions<NewsContext> options) : base(options) { }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
optionsBuilder.UseMySql(
    "Server=localhost;Port=3306;Database=news;User=api;Password=saadsaadsaad;Charset=utf8mb4",
    new MySqlServerVersion(new Version(8, 0, 25))
);    }
}

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly NewsContext _context;
    private readonly HttpClient _httpClient;

    public NewsController(NewsContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient();
    }

    public class Article
    {
        public int? Id { get; set; }
        public string? Author { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Url { get; set; }
        public string? Source { get; set; }
        public string? Image { get; set; }
        public string? Category { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public DateTime Published_At { get; set; }
    }

    public class Pagination
    {
        public int Limit { get; set; }
        public int Offset { get; set; }
        public int Count { get; set; }
        public int Total { get; set; }
    }

    public class ApiResponse
    {
        public Pagination Pagination { get; set; }
        public List<Article> Data { get; set; }
    }
[HttpGet]
public async Task<IActionResult> GetNews()
{
    string apiKey = "f5396e69922316f9c01b1fd15c1bcdd6"; 
    string apiUrl = $"https://api.mediastack.com/v1/news?access_key={apiKey}&countries=ma,fr&languages=fr&limit=100"; 

    try
    {
        var response = await _httpClient.GetStringAsync(apiUrl);
        var newsApiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

        if (newsApiResponse?.Data != null)
        {
            var articlesToAdd = new List<Article>();

            foreach (var article in newsApiResponse.Data)
            {
                if (!_context.articles.Any(a => a.Url == article.Url))
                {
                    if (!_context.articles.Any(a => a.Title == article.Title))
                    {
                        articlesToAdd.Add(article);
                    }
                }
            }

            if (articlesToAdd.Any())
            {
                await _context.articles.AddRangeAsync(articlesToAdd);
                await _context.SaveChangesAsync();
            }

            var allArticles = await _context.articles.ToListAsync();
            return Ok(allArticles);
        }

        return NotFound("No articles found.");
    }
    catch (DbUpdateException dbEx)
    {
        Console.WriteLine($"Database update error: {dbEx.Message}");
        
        return StatusCode(500, "An error occurred while updating the database.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred: {ex.Message}");
        
        return StatusCode(500, "An error occurred while processing your request.");
    }
}

}



