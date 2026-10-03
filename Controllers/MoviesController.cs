using Microsoft.AspNetCore.Mvc;
using OnlineCinema.Models;
using OnlineCinema.Services;

namespace OnlineCinema.Controllers;

public class MoviesController : Controller
{
    private const long MaxUploadSize = 500_000_000;
    private readonly MovieStorage _storage;

    public MoviesController(MovieStorage storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _storage.GetMoviesAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [RequestSizeLimit(MaxUploadSize)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadSize)]
    public async Task<IActionResult> Create(Movie movie, IFormFile? poster, IFormFile? video)
    {
        if (string.IsNullOrWhiteSpace(movie.Title) || poster == null || video == null)
        {
            ViewBag.Error = "Вкажіть назву та оберіть файли постера й відео.";
            return View(movie);
        }

        await _storage.AddAsync(movie, poster, video);
        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Watch(string id)
    {
        var movies = await _storage.GetMoviesAsync();
        var movie = movies.FirstOrDefault(m => m.Id == id);

        if (movie == null)
        {
            return NotFound();
        }

        return View(movie);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        await _storage.DeleteAsync(id);
        return RedirectToAction("Index");
    }
}
