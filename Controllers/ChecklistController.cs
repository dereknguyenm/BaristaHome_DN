﻿using BaristaHome.Data;
using BaristaHome.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;

namespace BaristaHome.Controllers
{
    [Authorize]
    public class ChecklistController : Controller
    {
        private readonly BaristaHomeContext _context;

        public ChecklistController(BaristaHomeContext context)
        {
            _context = context;
        }

        //Displays all the checklists in a store
        [HttpGet]
        /*SELINAvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvvv*/
        public IActionResult Checklist()
        {
            //List of a store's checklists
            var checklist = (from c in _context.Checklist
                             where c.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value)
                             select c).ToList();

            //checklistInfo = { {Checklist, {# of categorys, # of tasks}},..., }
            Dictionary<Checklist, List<int>> checklistInfo = new Dictionary<Checklist, List<int>>();

            //Calculates the total number of categories and tasks a checklist has
            //The calculated number gets added to a list where the Checklist object is the key
            foreach (var check in checklist)
            {
                var numOfCategory = 0;
                var numOfTask = 0;
                var checklistCategory = (from s in _context.Store
                                         join c in _context.Checklist on s.StoreId equals c.StoreId
                                         join cat in _context.Category on c.ChecklistId equals cat.ChecklistId
                                         where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && c.ChecklistId == check.ChecklistId
                                         select cat).ToList();

                if (checklistCategory != null)
                {
                    numOfCategory += checklistCategory.Count();

                    foreach (var cc in checklistCategory)
                    {
                        var checklistTasks = (from s in _context.Store
                                              join c in _context.Checklist on s.StoreId equals c.StoreId
                                              join cat in _context.Category on c.ChecklistId equals cat.ChecklistId
                                              join ct in _context.CategoryTask on cat.CategoryId equals ct.CategoryId
                                              join st in _context.StoreTask on ct.StoreTaskId equals st.StoreTaskId
                                              where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && ct.CategoryId == cc.CategoryId
                                              select st.TaskName).ToList();

                        if (checklistTasks != null)
                        {
                            numOfTask += checklistTasks.Count();
                        }
                    }
                }
                List<int> count = new List<int>();
                count.Add(numOfCategory);
                count.Add(numOfTask);
                checklistInfo[check] = count;
            }
            
            ViewBag.Checklist = checklistInfo;
            return View();
        }

        //Creates a new checklist
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checklist([Bind("ChecklistId,ChecklistTitle")] Checklist checklist)
        {
            checklist.StoreId = Convert.ToInt32(User.FindFirst("StoreId").Value);
            if (ModelState.IsValid)
            {
                //Checks if entered in checklist name already exists or not
                var existingChecklist = (from c in _context.Checklist
                                         where c.ChecklistTitle.Equals(checklist.ChecklistTitle) && c.StoreId.Equals(Convert.ToInt32(User.FindFirst("StoreId").Value))
                                         select c).FirstOrDefault();

                if (existingChecklist != null)
                {
                    ModelState.AddModelError(string.Empty, "Checklist name already exists! Please use a different one.");
                    return View(checklist);
                }

                _context.Add(checklist);
                await _context.SaveChangesAsync();
                return RedirectToAction("Checklist", "Checklist");
            }
            ModelState.AddModelError(string.Empty, "There was an issue creating a checklist.");
            return RedirectToAction(nameof(Checklist));
        }

        [HttpGet]
        // GET:  Gets a checklist's categories and tasks
        public async Task<IActionResult> ViewChecklist(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var checklist = await _context.Checklist.FirstOrDefaultAsync(m => m.ChecklistId == id);
            if (checklist == null)
            {
                return NotFound();
            }

            // Call helper function to get a ViewModel of a Checklist an a dictionary of their categories and respective tasks
            ChecklistViewModel checklistViewModel = GetCategoryTasks(checklist);
            return View(checklistViewModel);
        }

        // Helper function to get a checklist's respective categories and tasks
        public ChecklistViewModel GetCategoryTasks(Checklist checklist)
        {
            //List of a checklist's categories
            var checklistCategory = (from s in _context.Store
                                     join c in _context.Checklist on s.StoreId equals c.StoreId
                                     join cat in _context.Category on c.ChecklistId equals cat.ChecklistId
                                     where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && c.ChecklistId == checklist.ChecklistId
                                     select cat).ToList();

            //checklistInfo = { {categoryName, {list of tasks in category}}, ...}
            Dictionary<Category, List<StoreTask>> checklistInfo = new Dictionary<Category, List<StoreTask>>();

            //Finds tasks in a category and adds it to a list where the category is the key
            foreach (var cc in checklistCategory)
            {
                var checklistTasks = (from s in _context.Store
                                      join c in _context.Checklist on s.StoreId equals c.StoreId
                                      join cat in _context.Category on c.ChecklistId equals cat.ChecklistId
                                      join ct in _context.CategoryTask on cat.CategoryId equals ct.CategoryId
                                      join st in _context.StoreTask on ct.StoreTaskId equals st.StoreTaskId
                                      where s.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && ct.CategoryId == cc.CategoryId
                                      select st).ToList();

                checklistInfo[cc] = checklistTasks;
            }

            ChecklistViewModel checklistViewModel = new ChecklistViewModel
            {
                ChecklistId = checklist.ChecklistId,
                ChecklistTitle = checklist.ChecklistTitle,
                CategoryTasks = checklistInfo
            };

            // a dictionary of key-value pairs of the category name and their list of tasks
            return checklistViewModel;
        }

        //Deletes a checklist from the db
        [HttpPost]
        public async Task<IActionResult> DeleteChecklist(int id)
        {
            var checklist = await _context.Checklist.FindAsync(id);
            _context.Checklist.Remove(checklist);
            await _context.SaveChangesAsync();
            return RedirectToAction("Checklist", "Checklist");
        }
        /*SELINA^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^*/

        /* PETER ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼ */
        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> EditChecklist(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var checklist = await _context.Checklist.FirstOrDefaultAsync(m => m.ChecklistId == id);
            if (checklist == null)
            {
                return NotFound();
            }

            ChecklistViewModel checklistViewModel = GetCategoryTasks(checklist);
            return View(checklistViewModel);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int checklistId, int categoryId, int taskId, string taskName)
        {
            // getting the categorytask to update
            var categoryTask = await (from ct in _context.CategoryTask
                                              join t in _context.StoreTask on ct.StoreTaskId equals t.StoreTaskId
                                              where ct.CategoryId == categoryId && t.StoreTaskId == taskId
                                              select ct).FirstOrDefaultAsync();
            // getting the checklist to return view
            var checklist = await (from c in _context.Checklist
                                   where c.ChecklistId == checklistId
                                   select c).FirstOrDefaultAsync();

            if (checklist == null || categoryTask == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingTask = await (from t in _context.StoreTask
                                              where t.TaskName == taskName
                                              select t).FirstOrDefaultAsync();

                    CategoryTask newCategoryTask = new CategoryTask { CategoryId = categoryId };

                    // adding new task to db if unique and does not exist anywhere
                    if (existingTask == null)
                    {
                        StoreTask newTask = new StoreTask { TaskName = taskName };
                        _context.Add(newTask);
                        await _context.SaveChangesAsync();

                        // updating the categoryTask's id with the new task's id
                        newCategoryTask.StoreTaskId = newTask.StoreTaskId;
                    } 
                    else
                    {
                        // updating the categoryTask's id with the existing task's id
                        newCategoryTask.StoreTaskId = existingTask.StoreTaskId;
                    }

                    // see if there's any existing tasks in that category with the same name (id)
                    var existingCategoryTask = await (from ct in _context.CategoryTask
                                                      join t in _context.StoreTask on ct.StoreTaskId equals t.StoreTaskId
                                                      where ct.CategoryId == newCategoryTask.CategoryId && t.StoreTaskId == newCategoryTask.StoreTaskId
                                                      select ct).FirstOrDefaultAsync();
                    if (existingCategoryTask == null)
                    {
                        // for some reason EF advises to not update and change the ID of an existing object, but instead create a new one
                        _context.CategoryTask.Remove(categoryTask);
                        await _context.SaveChangesAsync();

                        // now we can "update" the categoryTask by adding a new one with the updated task id
                        newCategoryTask.IsFinished = false;
                        _context.Add(newCategoryTask);
                        await _context.SaveChangesAsync();
                        return RedirectToAction("EditChecklist", new { id = checklist.ChecklistId });
                    }
                    ModelState.AddModelError(string.Empty, "This task already exists in this category! Please use a differnt one.");
                    return RedirectToAction("EditChecklist", new { id = checklist.ChecklistId });
                    
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryTaskExists(categoryId, taskId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }   
            }
            ModelState.AddModelError(string.Empty, "There was an error editing this task.");
            return RedirectToAction("EditChecklist", new { id = checklist.ChecklistId });
        }

        private bool CategoryTaskExists(int categoryId, int taskId)
        {
            return _context.CategoryTask.Any(e => e.CategoryId == categoryId && e.StoreTaskId == taskId);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AddCategory([Bind("CategoryName,ChecklistId")] Category category)
        {
            if (ModelState.IsValid)
            {
                // checking for existing cateogry only for those in the same store AND same checklist (dupes are therefore allowed outside these parameters)
                var existingCategory = await (from cat in _context.Category
                                              join c in _context.Checklist on cat.ChecklistId equals c.ChecklistId
                                              where c.StoreId == Convert.ToInt32(User.FindFirst("StoreId").Value) && cat.CategoryName == category.CategoryName && cat.ChecklistId == category.ChecklistId
                                              select cat).FirstOrDefaultAsync();
                if (existingCategory != null)
                {
                    ModelState.AddModelError(string.Empty, "Category name already exists! Please use a different one.");
                    return View(category);
                }

                // reopen the edit page with the new category for that checklist
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("EditChecklist", new { id = category.ChecklistId });
            }
            ModelState.AddModelError(string.Empty, "There was an issue creating a category.");
            return View(category);
        }
        /* PETER ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲ */


        [HttpGet]
        public IActionResult MarkChecklist()
        {
            return View();
        }
    }
}
