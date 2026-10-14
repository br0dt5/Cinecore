using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cinecore.Data;
using Cinecore.Models;

namespace Cinecore.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public MoviesController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class SearchOptions
        {
            public string MovieTitle { get; set; } = string.Empty;
            public string MovieGenre { get; set; } = string.Empty;
            public int ReleaseYear { get; set; } = 0;
            public float MinScore { get; set; } = 0;
            public string SortOption { get; set; } = string.Empty;
        }

        [BindProperty(SupportsGet = true)]
        public SearchOptions Search { get; set; } = new SearchOptions();

        // GET: Movies
        [HttpGet]
        public async Task<IActionResult> Index(int pageIndex = 1)
        {
            Search.MovieTitle ??= string.Empty;
            Search.MovieGenre ??= string.Empty;
            Search.SortOption ??= string.Empty;

            IQueryable<Movie> query;
            query = _context.Movies
                .Where(m => m.Name.ToLower().Contains(Search.MovieTitle.ToLower()))
                .Where(m => m.Genre!.ToLower().Contains(Search.MovieGenre.ToLower()))
                .Where(m => m.ReleaseDate!.Value.Year >= Search.ReleaseYear)
                .Where(m => m.Score >= Search.MinScore);

            if (Search.SortOption.Equals("ascending"))
            {
                query = query.OrderBy(m => m.Name);
            }
            else if (Search.SortOption.Equals("descending"))
            {
                query = query.OrderByDescending(m => m.Name);
            }

            int pageSize = 10;
            var page = await PaginatedList<Movie>.CreateAsync(query.AsNoTracking(), pageIndex, pageSize);

            ViewBag.Search = Search;

            return View(page);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            return View(await _context.Movies
                                        .Where(m => m.Id == id)
                                        .FirstOrDefaultAsync());
        }

        [HttpPost]
        public async Task<IActionResult> SaveToFavorites(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var movie = await _context.Movies
                                        .Where(m => m.Id == id)
                                        .FirstOrDefaultAsync();

            if (user == null || movie == null)
            {
                return RedirectToAction("Index");
            }

            user.FavMovies.Add(movie);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> SaveToWatchlist(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var movie = await _context.Movies
                                        .Where(m => m.Id == id)
                                        .FirstOrDefaultAsync();

            if (user == null || movie == null)
            {
                return RedirectToAction("Index");
            }

            user.Watchlist.Add(movie);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddRating(int id, int score)
        {
            var user = await _userManager.GetUserAsync(User);
            var movie = await _context.Movies
                                        .Where(m => m.Id == id)
                                        .Include(m => m.Ratings)
                                        .FirstOrDefaultAsync();

            if (user == null || movie == null)
            {
                return RedirectToAction("Index");
            }

            var rating = new Rating { Score = score };

            user.Ratings.Add(rating);
            movie.Ratings.Add(rating);

            movie.Score = movie.Ratings.Average(r => r.Score);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
