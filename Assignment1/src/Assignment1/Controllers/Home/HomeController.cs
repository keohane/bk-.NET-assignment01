using System;
using System.Linq;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860
using Assignment1.Models;
using Microsoft.EntityFrameworkCore;

namespace Assignment1.Controllers.Home
{
    public class HomeController : Controller
    {

        Assignment1DataContext dataContext;

        public HomeController(Assignment1DataContext context)
        {
            dataContext = context;
            
        }

        // GET: /<controller>/
        public IActionResult Index()
        {
            // View model to pass to view
            var model = new IndexViewModel();

            // If not first time and logged in
            if (HttpContext.Session.GetInt32("userid") != null)
            {
                
                var user = (from u in dataContext.Users
                            where u.UserId == HttpContext.Session.GetInt32("userid")
                            select u).FirstOrDefault();
                model.User = user;
            }

            // Get list of blog posts currently in database
            model.BlogPosts = dataContext.BlogPosts.ToList();

            return View(model);
        }

        public IActionResult Register()
        {
            BaseViewModel model = new BaseViewModel();
            // If not first time and logged in
            if (HttpContext.Session.GetInt32("userid") != null)
            {
                var user = (from u in dataContext.Users
                            where u.UserId == HttpContext.Session.GetInt32("userid")
                            select u).FirstOrDefault();
                model.User = user;
            }

            return View(model);
        }

        public IActionResult RegisterUser(User user)
        {
            if (Request.Form["Name"] == "Admin")
            {
                user.RoleId = 2;
            }
            else
            {
                user.RoleId = 1;
            }

            // Add new user
            dataContext.Users.Add(user);
            // Save database changes
            dataContext.SaveChanges();
            // Redirect to the login page
            return RedirectToAction("Login");
        }

        public IActionResult Login()
        {
            BaseViewModel model = new BaseViewModel();
            // If not first time and logged in
            if (HttpContext.Session.GetInt32("userid") != null)
            {
                var user = (from u in dataContext.Users
                            where u.UserId == HttpContext.Session.GetInt32("userid")
                            select u).FirstOrDefault();
                model.User = user;
            }

            if (TempData["Error"] != null && (Int32)TempData["Error"] == 1) ViewBag.Error = 1;
            return View(model);
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }

        public IActionResult LoginUser()
        {
            var user = (from u in dataContext.Users where u.EmailAddress.Equals(Request.Form["Email"])
                && u.Password.Equals(Request.Form["Password"]) select u).FirstOrDefault();

            if (user != null)
            {
                ViewBag.User = user;
                HttpContext.Session.SetInt32("userid", user.UserId);
                return RedirectToAction("Index");
            }
            else
            {
                TempData["Error"] = 1;
                return RedirectToAction("Login");
            }
        }

        public IActionResult AddBlogPost()
        {
            BaseViewModel model = new BaseViewModel();
            var user = (from u in dataContext.Users
                        where u.UserId == HttpContext.Session.GetInt32("userid")
                        select u).FirstOrDefault();
            
            // Must be logged in and admin
            if (user != null && user.RoleId == 2)
            {
                model.User = user;
                return View(model);
            }
            
            // Either not logged in or not admin
            return RedirectToAction("Index");
        }

        public IActionResult SubmitBlogPost(BlogPost post)
        {
            post.Posted = DateTime.Now;
            post.UserId = HttpContext.Session.GetInt32("userid").Value;
            dataContext.BlogPosts.Add(post);
            dataContext.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult EditBlogPost(int id)
        {
            EditBlogPostViewModel model = new EditBlogPostViewModel();
            // If not first time and logged in
            if (HttpContext.Session.GetInt32("userid") != null)
            {
                var user = (from u in dataContext.Users
                            where u.UserId == HttpContext.Session.GetInt32("userid")
                            select u).FirstOrDefault();
                var blog = (from bp in dataContext.BlogPosts where bp.BlogPostId == id select bp).First();
                model.User = user;
                model.BlogPost = blog;
            }

            if (model.User != null && model.User.RoleId == 2)
            {
                return View(model);
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        public IActionResult UpdateBlogPost(BlogPost post)
        {
            dataContext.BlogPosts.Update(post);
            dataContext.SaveChanges();
            return RedirectToAction("Index");
        }

        public IActionResult DisplayFullBlogPost(int id)
        {
            DisplayFullBlogPostViewModel model = new DisplayFullBlogPostViewModel();
            // If not first time and logged in
            if (HttpContext.Session.GetInt32("userid") != null)
            {
                var u2 = (from u in dataContext.Users where u.UserId == HttpContext.Session.GetInt32("userid") select u).FirstOrDefault();
                model.User = u2;
            }

            // No lazy loading, causes errors in the long run
            dataContext.BlogPosts.Include(u => u.Users).ToList();
            dataContext.Comments.Include(c => c.Users).ToList();

            // Retrieve blog info
            model.BlogPost = (from bp in dataContext.BlogPosts where bp.BlogPostId == id select bp).First();
            // Get comments associated with blog post
            model.Comments = (from c in dataContext.Comments where c.BlogPostId == model.BlogPost.BlogPostId orderby c.CommentId select c).ToList();

            return View(model);
        }

        public IActionResult AddComment(Comment comment)
        {
            dataContext.Comments.Add(comment);
            dataContext.SaveChanges();
            return RedirectToAction("DisplayFullBlogPost", new { id = comment.BlogPostId });
        }
    }
}
