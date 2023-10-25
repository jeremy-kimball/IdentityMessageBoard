using IdentityMessageBoard.DataAccess;
using IdentityMessageBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace IdentityMessageBoard.Controllers
{
    public class MessagesController : Controller
    {
        private readonly MessageBoardContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(MessageBoardContext context, UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        public IActionResult Index()
        {
            var messages = _context.Messages
                .Include(m => m.Author)
                .OrderBy(m => m.ExpirationDate)
                .ToList()
                .Where(m => m.IsActive()); // LINQ Where(), not EF Where()

            return View(messages);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult AllMessages()
        {
            var allMessages = new Dictionary<string, List<Message>>()
            {
                { "active" , new List<Message>() },
                { "expired", new List<Message>() }
            };

            foreach (var message in _context.Messages)
            {
                if (message.IsActive())
                {
                    allMessages["active"].Add(message);
                }
                else
                {
                    allMessages["expired"].Add(message);
                }
            }


            return View(allMessages);
        }

        [Authorize(Roles = "Admin, SuperUser")]
        public IActionResult MyMessages()
        {
            var myMessages = new Dictionary<string, List<Message>>()
            {
                { "active" , new List<Message>() },
                { "expired", new List<Message>() }
            };

            var userId = _userManager.GetUserId(User);
            var user = _context.Users.Where(u => u.Id == userId).Include(u => u.Messages).First();

            foreach (var message in user.Messages)
            {
                if (message.IsActive())
                {
                    myMessages["active"].Add(message);
                }
                else
                {
                    myMessages["expired"].Add(message);
                }
            }


            return View(myMessages);
        }

        [Authorize]
        public IActionResult New()
        {
            return View();
        }

        [Authorize(Roles = "Admin, SuperUser")]
        [Route("Messages/MyMessages/{id:int}")]
        public IActionResult Edit(int id)
        {
            var userId = _userManager.GetUserId(User);
            var user = _context.Users.Find(userId);
            var message = _context.Messages.Find(id);
            message.Author = user;

            return View(message);
        }

        [Authorize]
        [HttpPost]
        public IActionResult Create(string userId, string content, int expiresIn)
        {
            var user = _context.Users.Find(userId);
            _context.Messages.Add(
                new Message()
                {
                    Content = content,
                    ExpirationDate = DateTime.UtcNow.AddDays(expiresIn),
                    Author = user
                });

            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin, SuperUser")]
        [HttpPost]
        [Route("Messages/MyMessages/{messageId:int}")]
        public IActionResult Update(int messageId, Message message, int expiresIn)
        {
            var userId = _userManager.GetUserId(User);
            var user = _context.Users.Find(userId);
            message.ExpirationDate = DateTime.UtcNow.AddDays(expiresIn);
            message.Id = messageId;
            _context.Update(message);
            _context.SaveChanges();

            return RedirectToAction("MyMessages");
        }

        [Authorize(Roles = "Admin, SuperUser")]
        [HttpPost]
        [Route("Messages/MyMessages/{messageId:int}/Delete")]
        public IActionResult Delete(int messageId, Message message)
        {
            var userId = _userManager.GetUserId(User);
            var user = _context.Users.Find(userId);
            message.Id = messageId;
            _context.Remove(message);
            _context.SaveChanges();

            return RedirectToAction("MyMessages");
        }
    }
}
