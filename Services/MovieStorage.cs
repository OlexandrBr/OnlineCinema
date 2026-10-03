using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using OnlineCinema.Models;

namespace OnlineCinema.Services;

public class MovieStorage
{
    private const string ListBlob = "movies.json";
    private readonly BlobContainerClient _container;

    public MovieStorage(IConfiguration config)
    {
        _container = new BlobContainerClient(config.GetConnectionString("BlobStorage"), "movies");
        _container.CreateIfNotExists();
    }

    public async Task<List<Movie>> GetMoviesAsync()
    {
        var blob = _container.GetBlobClient(ListBlob);

        if (!await blob.ExistsAsync())
        {
            return new List<Movie>();
        }

        var content = await blob.DownloadContentAsync();
        return content.Value.Content.ToObjectFromJson<List<Movie>>() ?? new List<Movie>();
    }

    public async Task AddAsync(Movie movie, IFormFile poster, IFormFile video)
    {
        movie.Id = Guid.NewGuid().ToString();
        movie.PosterBlob = await UploadAsync(movie.Id, "poster", poster);
        movie.VideoBlob = await UploadAsync(movie.Id, "video", video);

        var movies = await GetMoviesAsync();
        movies.Add(movie);
        await SaveAsync(movies);
    }

    public async Task DeleteAsync(string id)
    {
        var movies = await GetMoviesAsync();
        var movie = movies.FirstOrDefault(m => m.Id == id);

        if (movie == null)
        {
            return;
        }

        await _container.GetBlobClient(movie.PosterBlob).DeleteIfExistsAsync();
        await _container.GetBlobClient(movie.VideoBlob).DeleteIfExistsAsync();

        movies.Remove(movie);
        await SaveAsync(movies);
    }

    public string GetSasUrl(string blobName)
    {
        var blob = _container.GetBlobClient(blobName);
        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.AddHours(1)).ToString();
    }

    private async Task<string> UploadAsync(string id, string kind, IFormFile file)
    {
        var name = $"{id}/{kind}{Path.GetExtension(file.FileName)}";
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
        };

        await using var stream = file.OpenReadStream();
        await _container.GetBlobClient(name).UploadAsync(stream, options);
        return name;
    }

    private async Task SaveAsync(List<Movie> movies)
    {
        await _container.GetBlobClient(ListBlob).UploadAsync(BinaryData.FromObjectAsJson(movies), overwrite: true);
    }
}
