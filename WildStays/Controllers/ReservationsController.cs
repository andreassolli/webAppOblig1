﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WildStays.DAL;
using WildStays.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;


namespace WildStays.Controllers
{
    public class ReservationsController : Controller
    {
        private readonly IItemRepository _itemRepository;
        private readonly ILogger<ListingsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReservationsController(IItemRepository itemRepository,
            ILogger<ListingsController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _itemRepository = itemRepository;
            _logger = logger;
            _userManager = userManager;
        }



        // Index
        public async Task<IActionResult> Index()
        {
            var listings = await _itemRepository.GetAll();
            return View(listings);
        }

        // Detail action, same as in listingscontroller, but has reservations in it
        public async Task<IActionResult> Details(int id)
        {
            var listing = await _itemRepository.GetItemById(id);
            if (listing == null)
            {
                return NotFound();
            }
            return View(listing);
        }

        // Creates a reservation
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateReservation(int listingId, DateTime startDate, DateTime endDate)
        {
            try
            {
                var listing = await _itemRepository.GetItemById(listingId);
                if (listing == null)
                {
                    return NotFound();
                }

                // Check if the start date is after todays date
                if (!_itemRepository.DateCheck(startDate))
                {
                    TempData["ErrorMessage"] = "Start date must be after today's date.";
                    return RedirectToAction("Details", new { id = listingId });
                }

                // Check if the start date is before the end date
                if (!_itemRepository.StartEndCheck(startDate, endDate))
                {
                    TempData["ErrorMessage"] = "End date must be after the start date.";
                    return RedirectToAction("Details", new { id = listingId });
                }

                // Get the user id
                var userId = _userManager.GetUserId(User);

                // Create a new reservation
                var reservation = new Reservation
                {
                    ListingId = listingId,
                    StartDate = startDate,
                    EndDate = endDate,
                    UserId = userId
                };

                // Try to create the reservation
                bool isReservationSuccessful = await _itemRepository.CreateReservation(reservation);

                if (isReservationSuccessful)
                {
                    // Return the reservation confirmation view with the reservation
                    return View("ReservationConfirmation", reservation);
                }
                else
                {
                    // If the listing is not available
                    TempData["ErrorMessage"] = "This listing is not available for the selected dates.";
                    return RedirectToAction("Details", new { id = listingId });
                }
            }
            catch (Exception ex)
            {
                // Handles exceptions and logs them
                _logger.LogError("An error occurred while creating the reservation: {ex}", ex);
                TempData["ErrorMessage"] = "An error occurred while creating the reservation. Please try again later.";
                return RedirectToAction("Details", new { id = listingId });
            }
        }


    }
}
