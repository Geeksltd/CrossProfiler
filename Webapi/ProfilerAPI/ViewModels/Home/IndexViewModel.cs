using System.Collections.Generic;
using Geeks.ProfilerAPI.Models;

namespace Geeks.ProfilerAPI.ViewModels.Home
{
    public class IndexViewModel
    {
        public string Command { get; set; }

        public ICollection<Report> Reports { get; set; }
    }
}