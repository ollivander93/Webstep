using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaskApi.Models;
using TaskListApplication.Http;
using TaskListApplication.Models;

namespace TaskListApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITaskApiHttpClientWrapper _httpClient;

        public IdentityViewModel IdentityViewModel { get; set; }
        
        public HomeController(ILogger<HomeController> logger, 
            ITaskApiHttpClientWrapper httpClient)
        {
            _logger = logger;
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<IActionResult> Index()
        {
            var model = new IndexViewModel();
            var jwtToken = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(jwtToken))
                return View(model);

            var userInfo = await _httpClient.GetSignedInUser();

            var taskLists = await _httpClient.GetUserTaskLists(userInfo.UserId);
            var taskListArray = taskLists as TaskList[] ?? taskLists.ToArray();
            if (taskListArray.Any())
                model.TaskLists = taskListArray.ToList();
            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _httpClient.SignInAsync(email, password);
            if (result == null || result.Result == false)
                return RedirectToAction("Error", "Home");
            
            _httpClient.SetSessionParameters(result.Token, result.RefreshToken, email);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            var result = await _httpClient.RegisterAsync(email, password);
            if (result == null || result.Result == false)
                return RedirectToAction("Error", "Home");
            
            _httpClient.SetSessionParameters(result.Token, result.RefreshToken, email);

            return RedirectToAction("Index", "Home");
        }
        
        public IActionResult SignOut()
        {
            HttpContext.Session.Remove("token");
            HttpContext.Session.Remove("refreshToken");
            HttpContext.Session.Remove("userName");
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTaskList(string taskListName)
        {
            await _httpClient.CreateTaskListAsync(taskListName, HttpContext.Session.GetString("userId"));
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(int taskListId, string title, string note)
        {
            await _httpClient.CreateTaskAsync(taskListId, title, note);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> EditTask(int taskId, string title, string note, int taskListId)
        {
            await _httpClient.UpdateTaskAsync(taskId, title, note, taskListId);
            return RedirectToAction("Index", "Home");
        }
        
        [HttpPost]
        public Task<IActionResult> ChangeTaskOwner(int taskId, int currentTaskListId)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            await _httpClient.DeleteTaskAsync(taskId);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTaskList(int taskListId)
        {
            await _httpClient.DeleteTaskListAsync(taskListId);
            return RedirectToAction("Index", "Home");
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}