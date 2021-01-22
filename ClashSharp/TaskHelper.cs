using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32.TaskScheduler;

namespace ClashSharp
{
    public static class TaskHelper
    {
        private const string TaskPath = "ClashSharp Task";

        public static void InstallTask(string exePath, string arguments)
        {
            var taskService = TaskService.Instance;
            using var task = taskService.NewTask();

            task.Actions.Add(exePath, arguments, Directory.GetCurrentDirectory());
            task.Principal.RunLevel = TaskRunLevel.Highest;
            task.Settings.ExecutionTimeLimit = TimeSpan.Zero;
            task.Settings.DisallowStartIfOnBatteries = false;

            taskService.RootFolder.RegisterTaskDefinition(TaskPath, task);
        }

        public static Task? GetTask()
        {
            var taskService = TaskService.Instance;
            return taskService.GetTask(TaskPath);
        }
    }
}
