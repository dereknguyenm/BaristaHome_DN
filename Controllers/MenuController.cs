﻿using BaristaHome.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore.SqlServer;
using BaristaHome.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BaristaHome.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private readonly BaristaHomeContext _context;

        public MenuController(BaristaHomeContext context)
        {
            _context = context;
        }
        /*        public IActionResult Menu()
                {
                    return View();
                }*/

        /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
        //Displays view for add drink
        [HttpGet]
        public IActionResult Additem()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
        /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/


        //POST add a drink, drink tag, and tag to database
        [HttpPost]
        public async Task<IActionResult> AddItem([Bind("DrinkName,Instructions,Description,DrinkImageData,DrinkVideo,StoreId,Image,DrinkTags,Price")] Drink drink, 
            List<string> tagList, List<string> ingredientList, List<string> amountList, List<string> unitList)
        {
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            //Store Id
            var storeId = Convert.ToInt32(User.FindFirst("StoreId").Value);

            var existingDrink = (from d in _context.Drink
                                 where d.DrinkName.Equals(drink.DrinkName) && d.StoreId.Equals(storeId)
                                 select d).FirstOrDefault();

            if (existingDrink != null)
            {
                ModelState.AddModelError(string.Empty, "Drink name in use");
                return View(drink);
            }

            if (drink.Image != null)
            {
                using (var ms = new MemoryStream())
                {
                    drink.Image.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    drink.DrinkImageData = fileBytes;
                }
            }

            //Fix youtube link
            if ((drink.DrinkVideo != null) && drink.DrinkVideo.Contains("youtube.com"))
            {
                var temp = drink.DrinkVideo.Split("watch?v=");
                drink.DrinkVideo = temp[0] + "embed/" + temp[1];
            }
            else
            {
                drink.DrinkVideo = null;
            }

            if (ModelState.IsValid)
            {
                _context.Add(drink);
                await _context.SaveChangesAsync();

                for(int j = 0; j < ingredientList.Count; j++) 
                {
                    //Check for null values
                    if ((ingredientList.ElementAt(j) == null || amountList.ElementAt(j) == null) || unitList.ElementAt(j) == null)
                    {
                        return View(Additem());
                    }
                    var ing = ingredientList.ElementAt(j);
                    //Check for existing ingredients
                    var existingIng = (from i in _context.Ingredient
                                       where i.IngredientName == ing
                                       select i).FirstOrDefault();
                    //Add new ingredient
                    if (existingIng == null)
                    {
                        //Add to db
                        Ingredient newIngredient = new Ingredient { IngredientName = ing };
                        _context.Add(newIngredient);
                        await _context.SaveChangesAsync();

                        //Get Ingredient Id
                        var id = (from i in _context.Ingredient
                                  where i == newIngredient
                                  select i.IngredientId).FirstOrDefault();
                        DrinkIngredient drinkIngredient = new DrinkIngredient
                        {
                            DrinkId = drink.DrinkId,
                            IngredientId = id
                        };
                        drinkIngredient.unit = unitList[j];
                        _context.Add(drinkIngredient);
                        await _context.SaveChangesAsync();
                    }
                    //If ingredient exists in db
                    else
                    {
                        DrinkIngredient drinkIngredient = new DrinkIngredient
                        {
                            DrinkId = drink.DrinkId,
                            IngredientId = existingIng.IngredientId
                        };
                        drinkIngredient.Quantity = Convert.ToDecimal(amountList[j]);
                        drinkIngredient.unit = unitList[j];
                        _context.Add(drinkIngredient);
                        await _context.SaveChangesAsync();
                    }
                }
                /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

                /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                if (tagList != null)
                {
                    foreach (var tag in tagList)
                    {
                        //Checks Tag database to see if tag from list exists or not
                        var existingTag = (from t in _context.Tag
                                           where t.TagName == tag
                                           select t).FirstOrDefault();
                        //If tag does not exist yet, add it into the Tags database
                        if (existingTag == null)
                        {
                            Tag newTag = new Tag { TagName = tag };
                            _context.Add(newTag);
                            await _context.SaveChangesAsync();

                            var addedTag = (from t in _context.Tag
                                            where t == newTag
                                            select t.TagId).FirstOrDefault();

                            DrinkTag drinkTag = new DrinkTag
                            {
                                DrinkId = drink.DrinkId,
                                TagId = addedTag
                            };
                            _context.Add(drinkTag);
                            await _context.SaveChangesAsync();
                        }
                        //If tag already exists then add to DrinkTag database with the new drink and associated tag ids
                        else
                        {
                            //Gets the DrinkTag with the drink id and tag id
                            var existingDrinkTag = (from d in _context.Drink
                                                    join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                                    join t in _context.Tag on dt.TagId equals t.TagId
                                                    where drink.DrinkId == dt.DrinkId && existingTag.TagId == dt.TagId
                                                    select dt).FirstOrDefault();

                            //Checks if there is an existing DrinkTag
                            //This check is for when users enter in the same tag twice
                            //Duplicated tag is not added to the DrinkTag db
                            if (existingDrinkTag == null)
                            {
                                //If DrinkTag doesn't exist yet, add it into the DrinkTag db
                                DrinkTag drinkTag = new DrinkTag
                                {
                                    DrinkId = drink.DrinkId,
                                    TagId = existingTag.TagId
                                };
                                _context.Add(drinkTag);
                                await _context.SaveChangesAsync();
                            }

                        }
                        /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                    }
                }

                return RedirectToAction("Menu", "Menu");
            }
            return View(drink);

        }

        //Displays view for menu
        [HttpGet]
        public async Task<IActionResult> Menu()
        {
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            // Used to get drink list
            // Use type casting to return a IEnumerable<Model> with a LINQ query instead of doing await _context.Model.ToListAsync()
            var storeId = Convert.ToInt32(User.FindFirst("StoreId").Value);
            var drinkList = (IEnumerable<Drink>)from d in _context.Drink
                                                where d.StoreId == storeId
                                                orderby d.DrinkId descending
                                                select d;
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

            /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            // To get tags that belong to a store from database
            var tags = (IEnumerable<Tag>)(from s in _context.Store
                                          join d in _context.Drink on s.StoreId equals d.StoreId
                                          join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                          join t in _context.Tag on dt.TagId equals t.TagId
                                          where s.StoreId == storeId // forgot to filter by the user's store 
                                          select t);
            ViewData["Tags"] = new SelectList(tags.Distinct(), "TagId", "TagName");
            /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

            return View(drinkList);
        }

        /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
        //Displays the filtered menu
        [HttpPost]
        public async Task<IActionResult> Menu(string tagLine)
        {
            // Recreating viewbag to display store's filters/tags again
            var tags = (IEnumerable<Tag>)(from s in _context.Store
                                          join d in _context.Drink on s.StoreId equals d.StoreId
                                          join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                          join t in _context.Tag on dt.TagId equals t.TagId
                                          where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value)
                                          select t);
            ViewData["Tags"] = new SelectList(tags.Distinct(), "TagId", "TagName");

            if (tagLine != null)
            {
                // Converting the x,y,z,... string to an int list
                List<int> tagList = tagLine.Split(',').Select(int.Parse).ToList();

                var filteredDrinks = (from dt in _context.DrinkTag
                                     .Where(dt => tagList.Contains(dt.TagId))                 // get the drinktags that contain any of the ids in tagList
                                      join d in _context.Drink on dt.DrinkId equals d.DrinkId  // then joining with drink to return the drink obj
                                      select d).Distinct(); // ensure distinct drinks to prevent multiple same objs

                return View(filteredDrinks);
            }

            // If we reached here, that means we selected nothing to filter, so we must requery the drinks of the store again to redisplay onto the view
            var storeId = Convert.ToInt32(User.FindFirst("StoreId").Value);
            var drinkList = (IEnumerable<Drink>)from d in _context.Drink
                                                where d.StoreId == storeId
                                                orderby d.DrinkId descending
                                                select d;

            return View(drinkList);
        }
        /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

        [HttpGet]
        // GET: Display drink information of drink's page
        public async Task<IActionResult> Drink(int? id)
        {
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            if (id == null)
            {
                return NotFound();
            }

            var drink = await _context.Drink
                .FirstOrDefaultAsync(m => m.DrinkId == id);
            if (drink == null)
            {
                return NotFound();
            }
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

            else
            {
                /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                //Gets a list of tags that a drink has
                List<Tag> drinkTagQuery = (from d in _context.Drink
                                           join drinkTag in _context.DrinkTag on d.DrinkId equals drinkTag.DrinkId
                                           join tag in _context.Tag on drinkTag.TagId equals tag.TagId
                                           where d.DrinkId == drink.DrinkId
                                           select tag).ToList();
                ViewBag.DrinkTagList = drinkTagQuery;
                /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

                /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                //Gets ingredients of drink

                List<DrinkIngredient> drinkIngredientQuery = (from d in _context.Drink
                                                         join drinkIngredient in _context.DrinkIngredient on d.DrinkId equals drinkIngredient.DrinkId
                                                         where d.DrinkId == drink.DrinkId
                                                         select drinkIngredient).ToList();

                List<String> viewBag = new List<String>();
                foreach(var x in drinkIngredientQuery)
                {
                    Ingredient ingredient = (from i in _context.Ingredient
                                             where i.IngredientId == x.IngredientId
                                             select i).FirstOrDefault();
                    string item = x.Quantity + " " + x.unit + " " + ingredient.IngredientName;
                    viewBag.Add(item);
                }

                /*ViewBag.IngredientList = ingredientQuery;*/

                ViewBag.IngredientList = viewBag;
                //Get drink ingredient information
                List<DrinkIngredientViewModel> ingredients = (from d in _context.Drink
                                                              join drinkIngredient in _context.DrinkIngredient on d.DrinkId equals drinkIngredient.DrinkId
                                                              join ingredient in _context.Ingredient on drinkIngredient.IngredientId equals ingredient.IngredientId
                                                              where d.DrinkId == drink.DrinkId
                                                              select new DrinkIngredientViewModel
                                                              {
                                                                  Name = ingredient.IngredientName,
                                                                  Quantity = drinkIngredient.Quantity,
                                                                  IngredientId = ingredient.IngredientId,
                                                                  Unit = drinkIngredient.unit
                                                              }).ToList();

                //Get inventory information
                List<ItemViewModel> itemQuery = (from store in _context.Store
                                                 join inventory in _context.InventoryItem on store.StoreId equals inventory.StoreId // link store and inventoryitem by storeid
                                                 join item in _context.Item on inventory.ItemId equals item.ItemId                  // link inventoryitem and item by itemid
                                                 join unit in _context.Unit on item.UnitId equals unit.UnitId                       // link item and unit by unitid
                                                 where store.StoreId.Equals(Convert.ToInt16(User.FindFirst("StoreId").Value))       // filter items by user's store
                                                 select new ItemViewModel
                                                 {
                                                     Name = item.ItemName,                  // now we can send a 
                                                     Quantity = inventory.Quantity,         // ItemViewModel object
                                                     PricePerUnit = inventory.PricePerUnit, // to the view
                                                     UnitName = unit.UnitName,
                                                     ItemId = inventory.ItemId
                                                 }).ToList();
                List<String> missing = new List<String>();
                foreach (var di in ingredients)
                {
                    bool found = false;
                    //Break up drinkIngredient string
                    di.Name = di.Name.ToLower();
                    foreach (var i in itemQuery)
                    {
                        if (di.Name == i.Name.ToLower())
                        {
                            found = true;
                            continue;
                        }
                    }
                    //Keep track of missing items
                    if (found == false)
                    {
                        missing.Add(di.Name);
                    }
                }
                ViewBag.missingIngredients = missing;
                /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            }
            return View(drink);
        }

        [HttpGet]
        // GET: Drink Details
        public async Task<IActionResult> EditItem(int? id)
        {
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            if (id == null)
            {
                return NotFound();
            }

            var drink = await _context.Drink
                .FirstOrDefaultAsync(m => m.DrinkId == id);
            if (drink == null)
            {
                return NotFound();
            }

            //Gets ingredients of drink
/*            List<Ingredient> ingredientQuery = (from d in _context.Drink
                                                join drinkIngredient in _context.DrinkIngredient on d.DrinkId equals drinkIngredient.DrinkId
                                                join ingredient in _context.Ingredient on drinkIngredient.IngredientId equals ingredient.IngredientId
                                                where d.DrinkId == drink.DrinkId
                                                select ingredient).ToList();
            ViewBag.IngredientList = ingredientQuery;*/


            List<DrinkIngredient> drinkIngredientQuery = (from d in _context.Drink
                                                          join drinkIngredient in _context.DrinkIngredient on d.DrinkId equals drinkIngredient.DrinkId
                                                          where d.DrinkId == drink.DrinkId
                                                          select drinkIngredient).ToList();

            List<Ingredient> ingredientQuery = new List<Ingredient>();
            foreach (var x in drinkIngredientQuery)
            {
                Ingredient ingredient = (from i in _context.Ingredient
                                         where i.IngredientId == x.IngredientId
                                         select i).FirstOrDefault();
                ingredientQuery.Add(ingredient);
            }
            ViewBag.DrinkIngredientList = drinkIngredientQuery;
            ViewBag.IngredientList = ingredientQuery;

            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

            /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            //Gets a list of tags that a drink has
            List<Tag> tagQuery = (from d in _context.Drink
                                  join drinkTag in _context.DrinkTag on d.DrinkId equals drinkTag.DrinkId
                                  join tag in _context.Tag on drinkTag.TagId equals tag.TagId
                                  where d.DrinkId == drink.DrinkId
                                  select tag).ToList();
            ViewBag.TagList = tagQuery;
            /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            return View(drink);
        }


        //POST Edit Drink details
        [HttpPost]
        public async Task<IActionResult> EditItem([Bind("DrinkId,DrinkName,Description,Instructions,DrinkImageData,DrinkVideo,StoreId,Image,Price")] Drink drink, 
            List<string> tagList, List<string> ingredientList, List<string> amountList, List<string> unitList)
        {
            /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
            List<DrinkIngredient> drinkIngredients = (from d in _context.Drink
                                                      join di in _context.DrinkIngredient on d.DrinkId equals di.DrinkId
                                                      where di.DrinkId == drink.DrinkId
                                                      select di).ToList();


            List<Ingredient> ingredients = (from i in _context.Ingredient
                                            join di in _context.DrinkIngredient on i.IngredientId equals di.IngredientId
                                            join d in _context.Drink on di.DrinkId equals d.DrinkId
                                            where d.DrinkId == drink.DrinkId
                                            select i).ToList();

            //Removing old ingredients from drink
            foreach (var existingIngredient in ingredients)
            {
                var found = false;
                foreach (var ing in ingredientList)
                {
                    if (ing == existingIngredient.IngredientName)
                    {
                        found = true;
                        //Update junction values
                        Ingredient y = (from i in _context.Ingredient
                                        where i.IngredientName == ing
                                        select i).FirstOrDefault();
                        DrinkIngredient junction = (from i in _context.DrinkIngredient
                                                    where i.IngredientId == y.IngredientId
                                                    select i).FirstOrDefault();
                        for(int i = 0; i < ingredientList.Count; i++)
                        {
                            if(ingredientList[i] == ing)
                            {
                                junction.Quantity = Convert.ToDecimal(amountList[i]);
                                junction.unit = unitList[i];
                            }
                        }
                    }
                }
                //Remove ingredient if not found in ingredient is not found in new list
                if (!found)
                {
                    //Find Ingredient
                    var foundIngredient = (from i in _context.Ingredient
                                           where i.IngredientId == existingIngredient.IngredientId
                                           select i).FirstOrDefault();
                    //Remove junction
                    var DI = (from di in _context.DrinkIngredient
                              where di.IngredientId == foundIngredient.IngredientId
                              select di).First();
                    _context.DrinkIngredient.Remove(DI);
                    await _context.SaveChangesAsync();
                    //Delete ingredient from db if no other drinks use ingredient
                    var count = (from di in _context.DrinkIngredient
                                 where di.IngredientId == foundIngredient.IngredientId
                                 select di).Count();
                    if (count <= 0)
                    {
                        _context.Ingredient.Remove(foundIngredient);
                    }

                }
            }

            //Adding new ingredients to drink
            var newList = ingredientList;
            var allIngredients = (from i in _context.Ingredient
                                  select i).ToList();
            for(int j = 0; j < newList.Count; j++)
            {
                //Check for null
                if (amountList[j] == null || unitList[j] == null || newList[j] == null) 
                {
                    return View(Menu());
                }
                var ingredient = newList[j];
                var checkIngredient = (from i in _context.Ingredient
                                       where i.IngredientName == ingredient
                                       select i).FirstOrDefault() as Ingredient;
                //If ingredient exists
                if (allIngredients.Contains(checkIngredient))
                {
                    //if drink does not have ingredient
                    if (!ingredients.Contains(checkIngredient))
                    {
                        DrinkIngredient drinkIngredient = new DrinkIngredient
                        {
                            DrinkId = drink.DrinkId,
                            IngredientId = checkIngredient.IngredientId
                        };
                        drinkIngredient.Quantity = Convert.ToDecimal(amountList[j]);
                        drinkIngredient.unit = unitList[j];
                        _context.Add(drinkIngredient);
                        await _context.SaveChangesAsync();
                    }
                }
                //If ingredient does not exist
                else
                {
                    //Add ingredient to db
                    Ingredient newIngredient = new Ingredient { IngredientName = ingredient };
                    _context.Add(newIngredient);
                    await _context.SaveChangesAsync();

                    //Get Ingredient Id
                    var id = (from i in _context.Ingredient
                              where i == newIngredient
                              select i.IngredientId).FirstOrDefault();

                    //Create junction
                    DrinkIngredient drinkIngredient = new DrinkIngredient
                    {
                        DrinkId = drink.DrinkId,
                        IngredientId = id
                    };
                    drinkIngredient.Quantity = Convert.ToDecimal(amountList[j]);
                    drinkIngredient.unit = unitList[j];
                    _context.Add(drinkIngredient);
                    await _context.SaveChangesAsync();
                }
            }


            var existingDrink = (from d in _context.Drink
                                 where d.DrinkName.Equals(drink.DrinkName) && !d.DrinkId.Equals(drink.DrinkId) && d.StoreId.Equals(drink.StoreId)
                                 select d).FirstOrDefault();

            if (existingDrink != null)
            {
/*                ModelState.AddModelError(string.Empty, "Drink name in use");*/
                return View(drink);
            }

            if (drink.Image != null)
            {
                using (var ms = new MemoryStream())
                {
                    drink.Image.CopyTo(ms);
                    var fileBytes = ms.ToArray();
                    drink.DrinkImageData = fileBytes;
                }
            }

            //Format embed link
            if ((drink.DrinkVideo != null) && drink.DrinkVideo.Contains("youtube.com"))
            {
                if (!drink.DrinkVideo.Contains("embed/"))
                {
                    var temp = drink.DrinkVideo.Split("watch?v=");
                    drink.DrinkVideo = temp[0] + "embed/" + temp[1];
                }
            }
            else
            {
                drink.DrinkVideo = null;
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(drink);
                    await _context.SaveChangesAsync();
                    /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

                    /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                    /* 
                     * delete drink tags with old tags from DrinkTag db
                     * if drink tag doesnt exist: check if tag exists in tag db, 
                     *      - if tag exists, make drinktag
                     *      - if tag doesnt exist: make tag, check if drinktag alrdy exists, make drink tag
                     * check if there are any tags not associated with a drink
                     *      - if a tag is used in no drinks, delete from the tags database
                     * 
                     */
                    if (tagList != null)
                    {
                        //Gets all the tag names of the old tags of the Drink
                        var oldTags = (from d in _context.Drink
                                       join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                       join t in _context.Tag on dt.TagId equals t.TagId
                                       where drink.DrinkId == dt.DrinkId
                                       select t.TagName).ToList();

                        //If drink has existing old tags
                        if (oldTags != null)
                        {
                            //Returns a list of tags that are no longer used by the drink
                            var deleteOldTags = oldTags.Except(tagList);

                            if (deleteOldTags != null)
                            {
                                foreach (var ot in deleteOldTags)
                                {
                                    //Gets the tag id of the tag that is going to be deleted
                                    var deleteTag = (from t in _context.Tag
                                                     where t.TagName == ot
                                                     select t.TagId).FirstOrDefault();

                                    //Gets DrinkTag with the tag that is no longer being used
                                    var deleteDrinkTag = (from dt in _context.DrinkTag
                                                          join d in _context.Drink on dt.DrinkId equals d.DrinkId
                                                          join t in _context.Tag on dt.TagId equals t.TagId
                                                          where drink.DrinkId == dt.DrinkId && dt.TagId == deleteTag
                                                          select dt).FirstOrDefault();

                                    //Deletes Drink Tags with old tag from DrinkTag db
                                    _context.DrinkTag.Remove(deleteDrinkTag);
                                    await _context.SaveChangesAsync();

                                }
                            }
                        }

                        foreach (var tag in tagList)
                        {
                            //Checks Tag database to see if tag from list exists or not
                            var existingTag = (from t in _context.Tag
                                               where t.TagName == tag
                                               select t).FirstOrDefault();
                            //If tag does not exist yet, add it into the Tags database
                            if (existingTag == null)
                            {
                                Tag newTag = new Tag { TagName = tag };
                                _context.Add(newTag);
                                await _context.SaveChangesAsync();

                                var addedTag = (from t in _context.Tag
                                                where t == newTag
                                                select t.TagId).FirstOrDefault();

                                DrinkTag drinkTag = new DrinkTag
                                {
                                    DrinkId = drink.DrinkId,
                                    TagId = addedTag
                                };
                                _context.Add(drinkTag);
                                await _context.SaveChangesAsync();
                            }
                            //If tag already exists then add to DrinkTag database with the drink and associated tag ids
                            else
                            {
                                //Gets the DrinkTag with the drink id and tag id
                                var existingDrinkTag = (from d in _context.Drink
                                                        join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                                        join t in _context.Tag on dt.TagId equals t.TagId
                                                        where drink.DrinkId == dt.DrinkId && existingTag.TagId == dt.TagId
                                                        select dt).FirstOrDefault();

                                //Checks if there is an existing DrinkTag
                                if (existingDrinkTag == null)
                                {
                                    //If DrinkTag doesn't exist yet, add it into the DrinkTag db
                                    //This check is for when users enter in the same tag twice
                                    //Duplicated tag is not added to the DrinkTag db
                                    DrinkTag drinkTag = new DrinkTag
                                    {
                                        DrinkId = drink.DrinkId,
                                        TagId = existingTag.TagId
                                    };
                                    _context.Add(drinkTag);
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                    }

                    //Gets all the tag IDs from the tag db
                    var allTags = (from t in _context.Tag
                                   select t.TagId).ToList();

                    //Gets all the tag IDs from the drinktag db
                    var allUsedTag = (from d in _context.Drink
                                      join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                                      join t in _context.Tag on dt.TagId equals t.TagId
                                      where dt.TagId == t.TagId
                                      select dt.TagId).Distinct();

                    //Gets all the tags that are not associated with any drinks
                    var deleteUnusedTag = allTags.Except(allUsedTag);

                    //If a tag is used in no drinks, delete from the Tags db
                    if (deleteUnusedTag != null)
                    {
                        foreach (var t in deleteUnusedTag)
                        {
                            var tag = await _context.Tag.FindAsync(t);
                            _context.Tag.Remove(tag);
                            await _context.SaveChangesAsync();
                        }
                    }



                    /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
                }
                catch (DbUpdateConcurrencyException)
                {
                    throw;
                }
                return RedirectToAction("Menu", "Menu");
            }
            return View(drink);

        }

        /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
        //[HttpGet]
        /*public async Task<IActionResult> Delete(int? id)
        {
            var a = id;
            if (id == null)
            {
                return NotFound();
            }

            var drink = await _context.Drink.FirstOrDefaultAsync(m => m.DrinkId == id);

            if (drink == null)
            {
                return NotFound();
            }

            List<Tag> drinkTagQuery = (from d in _context.Drink
                                       join drinkTag in _context.DrinkTag on d.DrinkId equals drinkTag.DrinkId
                                       join tag in _context.Tag on drinkTag.TagId equals tag.TagId
                                       where d.DrinkId == drink.DrinkId
                                       select tag).ToList();
            ViewBag.DrinkTagList = drinkTagQuery;

            return View(drink);
        }*/

        // POST: Menu/Delete/DrinkId

        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? drinkId, int? tagId)
        {
            var existingDrinkTag = (from dt in _context.DrinkTag
                                    join d in _context.Drink on dt.DrinkId equals d.DrinkId
                                    join t in _context.Tag on dt.TagId equals t.TagId
                                    where d.DrinkId == drinkId && t.TagId == tagId
                                    select dt).FirstOrDefault();

            //var z = existingDrinkTag;
            //var b = existingTag;
            /* foreach (var tag in existingTag)
             {
                 var dTag = await (from dt in _context.DrinkTag
                                   join d in _context.Drink on dt.DrinkId equals d.DrinkId
                                   join t in _context.Tag on dt.TagId equals t.TagId
                                   where d.DrinkId == id && t.TagName == tag
                                   select dt).FirstOrDefaultAsync();
                 _context.DrinkTag.Remove(dTag);
                 await _context.SaveChangesAsync();
             }*/
            //var dTag = await _context.DrinkTag.FindAsync(drinkId, tagId);
            _context.DrinkTag.Remove(existingDrinkTag);
            await _context.SaveChangesAsync();

            var drink = await _context.Drink
                .FirstOrDefaultAsync(m => m.DrinkId == drinkId);
            if (drink == null)
            {
                return NotFound();
            }

            return View(drink);


            /*var filteredDrinks = (from dt in _context.DrinkTag
                             .Where(dt => tagList.Contains(dt.TagId))                 // get the drinktags that contain any of the ids in tagList
                             join d in _context.Drink on dt.DrinkId equals d.DrinkId  // then joining with drink to return the drink obj
                             select d).Distinct();                                    // ensure distinct drinks to prevent multiple same objs
            */
            /*
                var tags = await (from s in _context.Store
                        join d in _context.Drink on s.StoreId equals d.StoreId
                        join dt in _context.DrinkTag on d.DrinkId equals dt.DrinkId
                        join t in _context.Tag on dt.TagId equals t.TagId
                        select t).ToListAsync();
                */

        }
        /*SELINA ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

        /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
        //Method for rendering images
        public async Task<ActionResult> RenderImage(int id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var drink = await _context.Drink
                .FirstOrDefaultAsync(m => m.DrinkId == id);
            if (drink == null)
            {
                return NotFound();
            }
            var image = (from d in _context.Drink
                         where d.DrinkId == drink.DrinkId
                         select drink.DrinkImageData).First();


            return File(image, "image/png");
        }
        /*ALEX ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/


        /*CINDIE ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/
        //Method for returning and displaying all the store's Drink items that contain the search phrase in its name
        //If there are any filters/tags selected, it will return only drinks with matching filters/tags

        public async Task<IActionResult> ShowSearchResults(string SearchPhrase, string tagLine)
        {

            //Get current store ID
            var storeId = Convert.ToInt32(User.FindFirst("StoreId").Value);

            //Return a list of drinks that contain the search phrase in its name
            var drinkList = (List<Drink>)(from d in _context.Drink
                                          where (d.StoreId == storeId && d.DrinkName.Contains(SearchPhrase))
                                          orderby d.DrinkId descending
                                          select d).ToList();

            if (tagLine == null)
            {
                return View(drinkList);
            }


            List<int> tagList = tagLine.Split(',').Select(int.Parse).ToList();
            int numOfTags = tagList.Count;
            int numOfMatchingTags = 0;
            List<Drink> resultDrinks = new List<Drink>();

            if ((tagLine != null) && (SearchPhrase == null))
            {
                Menu();
            }

            //If there are tags selected
            if (tagList.Any())
            {
                //If there are drinks that match the search phrase
                if (drinkList.Any())
                {
                    // For each drink that contains the search phrase
                    foreach (Drink d in drinkList)
                    {
                        numOfMatchingTags = 0;

                        // Get all tags belonging to the current drink
                        var drinkTagList = (IEnumerable<Tag>)(from dt in _context.DrinkTag
                                                              join t in _context.Tag on dt.TagId equals t.TagId
                                                              where d.DrinkId == dt.DrinkId
                                                              select t).ToList();
                        // Go thru each tag that the current drink has
                        foreach (Tag t in drinkTagList)
                        {
                            // Go thru each tag that the user selected 
                            foreach (int drinkTag in tagList)
                            {
                                // Check if they are the same tag
                                if (drinkTag.Equals(t.TagId))
                                {
                                    numOfMatchingTags += 1;
                                }

                                // If the drink contains all of the tags that the user selected, add the drink to the result list
                                if (numOfMatchingTags == numOfTags)
                                {
                                    resultDrinks.Add(d);
                                    numOfMatchingTags = 0;

                                }
                            }

                        }


                    }
                }
            }
            else
            {
                return View(drinkList);
            }


            //Return list of drinks to the .cshtml to be displayed
            return View(resultDrinks);
        }
        /*CINDIE ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

        /* PETER ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼ */
        public async Task<IActionResult> Sales()
        {
            // Displaying list of store's drinks to filter sales
            IEnumerable<Drink> drinks = await (from d in _context.Drink
                                               where d.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value)
                                               orderby d.DrinkName
                                               select d).ToListAsync();
            ViewData["DrinkNames"] = new SelectList(drinks, "DrinkName", "DrinkName");

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSales()
        {
            // Add all the distinct drinks by id then sum up their count sold & profit
            var salesQuery = await (from s in _context.Sale
                                    join d in _context.Drink on s.DrinkId equals d.DrinkId
                                    where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value)
                                    // Group rows by the same drink name
                                    group s by new { d.DrinkName } into g
                                    // Then use an aggregate function to sum up the profit and units sold
                                    select new
                                    {
                                        DrinkName = g.Key.DrinkName,
                                        UnitsSold = g.Sum(s => s.UnitsSold),
                                        Profit = g.Sum(s => s.Profit)
                                    }).ToListAsync();

            // Serialize data to Json to then be able to read and use the data in js
            return Json(salesQuery);
        }

        [HttpPost]
        public async Task<IActionResult> SalesFilter(string drinkName)
        {
            IEnumerable<Drink> drinks = await (from d in _context.Drink
                                               where d.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value)
                                               orderby d.DrinkName
                                               select d).ToListAsync();
            ViewData["DrinkNames"] = new SelectList(drinks, "DrinkName", "DrinkName");
            if (drinkName != null)
            {
                var filteredDrink = await (from d in _context.Drink
                                           where d.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && d.DrinkName == drinkName
                                           select d).FirstOrDefaultAsync();
                return View(filteredDrink);
            }
            // Display sales for all drinks when filter is cleared
            return RedirectToAction(nameof(Sales));
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesFilter(int drinkId)
        {
            // displaying daily sales of filtered drink
            IEnumerable<SaleViewModel> filterQuery = await (from s in _context.Sale
                                                            join d in _context.Drink on s.DrinkId equals d.DrinkId
                                                            where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && d.DrinkId == drinkId
                                                            group s by new { d.DrinkName, Date = new DateTime(s.TimeSold.Year, s.TimeSold.Month, s.TimeSold.Day) } into g
                                                            select new SaleViewModel
                                                            {
                                                                DrinkName = g.Key.DrinkName,
                                                                TimeSold = g.Key.Date.ToString("MM/dd/yyyy"),
                                                                UnitsSold = g.Sum(x => x.UnitsSold),
                                                                Profit = g.Sum(x => x.Profit)
                                                            }).OrderBy(x => x.DrinkName).ToListAsync();
            return Json(filterQuery);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellDrink([Bind("DrinkId,DrinkName,Price,Instructions,Description,StoreId")] Drink drink)
        {
            if (ModelState.IsValid)
            {
                TempData["drinkSold"] = "success";
                // create a sale to append a record to the db
                Sale sale = new Sale { UnitsSold = 1,
                                       Profit = drink.Price,
                                       TimeSold = DateTime.Now,
                                       DrinkId = drink.DrinkId,
                                       StoreId = drink.StoreId,};
                _context.Add(sale);
                await _context.SaveChangesAsync();

                /*Alex vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv*/
                var theDrink = await _context.Drink.FirstOrDefaultAsync(m => m.DrinkId == drink.DrinkId);
                //Get drink ingredient information
                List<DrinkIngredientViewModel> ingredients = (from d in _context.Drink
                                                              join drinkIngredient in _context.DrinkIngredient on d.DrinkId equals drinkIngredient.DrinkId
                                                              join ingredient in _context.Ingredient on drinkIngredient.IngredientId equals ingredient.IngredientId
                                                              where d.DrinkId == drink.DrinkId
                                                              select new DrinkIngredientViewModel
                                                              {
                                                                  Name = ingredient.IngredientName,
                                                                  Quantity = drinkIngredient.Quantity,
                                                                  IngredientId = ingredient.IngredientId,
                                                                  Unit = drinkIngredient.unit
                                                              }).ToList();

                //Get inventory information
                List<ItemViewModel> itemQuery = (from store in _context.Store
                                                 join inventory in _context.InventoryItem on store.StoreId equals inventory.StoreId // link store and inventoryitem by storeid
                                                 join item in _context.Item on inventory.ItemId equals item.ItemId                  // link inventoryitem and item by itemid
                                                 join unit in _context.Unit on item.UnitId equals unit.UnitId                       // link item and unit by unitid
                                                 where store.StoreId.Equals(Convert.ToInt16(User.FindFirst("StoreId").Value))       // filter items by user's store
                                                 select new ItemViewModel
                                                 {
                                                     Name = item.ItemName,                  // now we can send a 
                                                     Quantity = inventory.Quantity,         // ItemViewModel object
                                                     PricePerUnit = inventory.PricePerUnit, // to the view
                                                     UnitName = unit.UnitName,
                                                     ItemId = inventory.ItemId
                                                 }).ToList();
                List<DrinkIngredientViewModel> foundItems = new List<DrinkIngredientViewModel>();
                foreach (var di in ingredients)
                {
                    bool found = false;
                    //Break up drinkIngredient string
                    di.Name = di.Name.ToLower();
                    foreach (var i in itemQuery)
                    {
                        if (di.Name == i.Name.ToLower())
                        {
                            found = true;
                            foundItems.Add(di);
                            continue;
                        }
                    }
                }
                //Update inventory based on available items
                foreach (var item in foundItems)
                {
                    foreach (var inventoryItem in itemQuery)
                    {
                        if (item.Name.ToLower() == inventoryItem.Name.ToLower())
                        {
                            InventoryItem updateItem = (from ii in _context.InventoryItem
                                                        join i in _context.Item on ii.ItemId equals i.ItemId
                                                        where i.ItemName.ToLower().Equals(item.Name.ToLower()) && ii.StoreId.Equals(Convert.ToInt16(User.FindFirst("StoreId").Value))
                                                        select ii).FirstOrDefault();

                            if (updateItem != null)
                            {
                                if (updateItem.Quantity < item.Quantity)
                                {
                                    updateItem.Quantity = 0;
                                }
                                else
                                {
                                    updateItem.Quantity = updateItem.Quantity - item.Quantity;
                                }
                                _context.Update(updateItem);
                                await _context.SaveChangesAsync();
                            }

                        }
                    }
                }
                /*Alex^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/


                return RedirectToAction(nameof(Drink), new { id = drink.DrinkId });
            }
            TempData["drinkSold"] = "failed";

            return RedirectToAction(nameof(Drink), new { id = drink.DrinkId });
        }
        /* PETER ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲ */

        /*Alex vvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv*/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDrink([Bind("DrinkId,DrinkName,Instructions,Description,StoreId")] Drink drink)
        {
            var d = await _context.Drink.FindAsync(drink.DrinkId);
            //Delete/Clean up tags
            //Get
            var tags = await (from dr in _context.Drink
                              join dt in _context.DrinkTag on dr.DrinkId equals dt.DrinkId
                              join t in _context.Tag on dt.TagId equals t.TagId
                              where dr.DrinkId == drink.DrinkId
                              select t).ToListAsync();
            var salesOfDrink = await (from s in _context.Sale
                                      where s.DrinkId == d.DrinkId && s.StoreId == d.StoreId
                                      select s).ToListAsync();
            foreach(var t in tags)
            {
                //Check if more than 1 drink is using tag
                var count = (from dt in _context.DrinkTag
                             where dt.TagId == t.TagId
                             select dt).Count();
                //if there is only 1 drink with the tag, delete the tag. DB will automatically delete junction if just drink is deleted
                if (count == 1)
                {
                    _context.Tag.Remove(t);
                    await _context.SaveChangesAsync();
                }

            }

            //Delete Sales
            foreach (var sale in salesOfDrink)
            {
                _context.Sale.Remove(sale);
                await _context.SaveChangesAsync();
            }
            //Delete Drink
            _context.Drink.Remove(d);
            await _context.SaveChangesAsync();

            return RedirectToAction("Menu", "Menu");
        }
        /*Alex^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

    }
}
