using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskApi.Models;
using TaskListApplication.Http;
using TaskListApplication.Models;
using TaskListApplication.Services;

namespace TaskListApplication.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly ITaskListService _taskListService;
        private readonly ITaskService _taskService;
        private readonly IUserService _userService;

        public HomeController(IAuthenticationService authService,
            ITaskListService taskListService,
            ITaskService taskService,
            IUserService userService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _taskListService = taskListService ?? throw new ArgumentNullException(nameof(taskListService));
            _taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        }

        public async Task<IActionResult> Index()
        {
            var model = new IndexViewModel();
            var jwtToken = HttpContext.Session.GetString("token");
            if (string.IsNullOrEmpty(jwtToken))
                return View(model);

            var userInfo = await _userService.GetSignedInUser();

            var taskLists = await _taskListService.GetUserTaskLists(userInfo.UserId);
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
            var result = await _authService.SignInAsync(email, password);
            if (result == null || result.Result == false)
                return RedirectToAction("Error", "Home");
            
            _authService.SetSessionParameters(result.Token, result.RefreshToken, email);

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Register(string email, string password)
        {
            var result = await _authService.RegisterAsync(email, password);
            if (result == null || result.Result == false)
                return RedirectToAction("Error", "Home");
            
            _authService.SetSessionParameters(result.Token, result.RefreshToken, email);

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
            await _taskListService.CreateTaskListAsync(taskListName, HttpContext.Session.GetString("userId"));
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> CreateTask(int taskListId, string title, string note)
        {
            await _taskService.CreateTaskAsync(taskListId, title, note);
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> EditTask(int taskId, string title, string note, int taskListId)
        {
            await _taskService.UpdateTaskAsync(taskId, title, note, taskListId);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            await _taskService.DeleteTaskAsync(taskId);
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTaskList(int taskListId)
        {
            await _taskListService.DeleteTaskListAsync(taskListId);
            return RedirectToAction("Index", "Home");
        }
        
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}