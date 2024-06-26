﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Auctions.Models;
using Auctions.Data.Services;
using System.Security.Claims;

namespace Auctions.Controllers;

public class ListingsController : Controller
{
    private readonly IListingsService _listingsService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IBidsService _bidsService;
    private readonly ICommentsService _commentsService;

    public ListingsController(IListingsService listingsService, IBidsService bidsService,
        ICommentsService commentsService, IWebHostEnvironment webHostEnvironment)
    {
        _listingsService = listingsService;
        _bidsService = bidsService;
        _webHostEnvironment = webHostEnvironment;
        _commentsService = commentsService;
    }

    // GET Listings
    public async Task<IActionResult> Index(int? pageNumber, string searchString)
    {
        var applicationDbContext = _listingsService.GetAll();
        int pageSize = 3;
        if(!string.IsNullOrEmpty(searchString))
        {
            applicationDbContext = applicationDbContext.Where(a => a.Title.Contains(searchString));
            return View(await PaginatedList<Listing>.CreateAsync(
                applicationDbContext.Where(l => l.IsSold == false).AsNoTracking(), pageNumber ?? 1, pageSize));

        }

        return View(await PaginatedList<Listing>.CreateAsync(
            applicationDbContext.Where(l => l.IsSold == false).AsNoTracking(), pageNumber ?? 1, pageSize));
    }

    public IActionResult Create()
    {
        return View();
    }

    public async Task<IActionResult> MyListings(int? pageNumber)
    {
        var applicationDbContext = _listingsService.GetAll();
        int pageSize = 3;          

        return View("Index", await PaginatedList<Listing>.CreateAsync(applicationDbContext.Where(l =>l.IdentityUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).AsNoTracking(), pageNumber ?? 1, pageSize));
    }
    public async Task<IActionResult> MyBids(int? pageNumber)
    {
        var applicationDbContext = _bidsService.GetAll();
        int pageSize = 3;

        return View(await PaginatedList<Bid>.CreateAsync(applicationDbContext.Where(l => l.IdentityUserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).AsNoTracking(), pageNumber ?? 1, pageSize));
    }
    
    [HttpPost]
    public async Task<ActionResult> AddBid([Bind("Id, Price, ListingId, IdentityUserId")] Bid bid)
    {
        if(ModelState.IsValid) 
        {
            await _bidsService.Add(bid);
        }
        var listing = await _listingsService.GetById(bid.ListingId);
        listing.Price = bid.Price;
        await _listingsService.SaveChanges();

        return View("Details", listing);
    }
    
    public async Task<ActionResult> CloseBidding(int id)
    {
        var listing = await _listingsService.GetById(id);
        listing.IsSold = true;
        await _listingsService.SaveChanges();
        return View("Details", listing);
    }
    
    [HttpPost]
    public async Task<ActionResult> AddComment([Bind("Id, Content, ListingId, IdentityUserId")] Comment comment)
    {
        if(ModelState.IsValid) 
        {
            await _commentsService.Add(comment);
        }
        var listing = await _listingsService.GetById(comment.ListingId);
        return View("Details", listing);
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ListingViewModel listing)
    {
        if(listing.Image != null) 
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "Images");
            string fileName = listing.Image.FileName;
            string filePath = Path.Combine(uploadDir, fileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                listing.Image.CopyTo(fileStream);
            }

            var listObj = new Listing
            {
                Title = listing.Title,
                Description = listing.Description,
                Price = listing.Price,                   
                IdentityUserId = listing.IdentityUserId,
                ImagePath = fileName,
            };
            
            await _listingsService.Add(listObj);
            return RedirectToAction("Index");
        }
        return View(listing);
    }
    
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var listing = await _listingsService.GetById(id);

        if (listing == null)
        {
            return NotFound();
        }

        return View(listing);
    }
}