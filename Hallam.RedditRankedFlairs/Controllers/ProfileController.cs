﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Hallam.RedditRankedFlairs.Models;
using Hallam.RedditRankedFlairs.Riot;
using Hallam.RedditRankedFlairs.Services;

namespace Hallam.RedditRankedFlairs.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        public IRiotService Riot { get; set; }
        public ISummonerService Summoners { get; set; }
        public IUserService Users { get; set; }
        public ProfileViewModel ViewModel { get; set; }

        public ProfileController(IUserService userService, IRiotService riotService, ISummonerService summonerService)
        {
            Users = userService;
            Riot = riotService;
            Summoners = summonerService;
        }

        public async Task<ActionResult> Index()
        {
            ViewModel = await CreateViewModelAsync();
            return View(ViewModel);
        }

        [HttpPost]
        public async Task<ActionResult> Register(SummonerModel model)
        {
            ViewModel = await CreateViewModelAsync();
            if (!ModelState.IsValid)
            {
                return Error(ModelState);
            }

            // Rule: Summoner must not be registered to a User.
            if (await Summoners.IsSummonerRegistered(model.Region, model.SummonerName))
            {
                return Error("Summoner is already registered.");
            }

            // Rule: Summoner must exist.
            var cacheKey = string.Concat(model.Region, ":", model.SummonerName).ToLowerInvariant();
            var summoner = await CacheUtil.GetItemAsync(cacheKey,
                () => Riot.FindSummonerAsync(model.Region, model.SummonerName));

            if (summoner == null)
            {
                return Error("Summoner not found.");
            }

            return Success();
        }

        private async Task<ProfileViewModel> CreateViewModelAsync()
        {
            var viewModel = new ProfileViewModel
            {
                User = await Users.FindAsync(User.Identity.Name),
            };
            return viewModel;
        }


        private ActionResult Error(ModelStateDictionary modelState)
        {
            var errorStates = modelState.SelectMany(kvp => kvp.Value.Errors);
            return Error(errorStates.First().ErrorMessage);
        }

        private ActionResult Error(string errorMessage)
        {
            return RedirectToAction("Index", new {error = errorMessage});
        }

        private ActionResult Success()
        {
            return RedirectToAction("Index");
        }
    }
}