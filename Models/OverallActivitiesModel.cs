using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SBIT3J_SuperSystem_Final.Models
{
    public class OverallActivitiesModel
    {
        public int Employee_ID { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }

        public string Role {  get; set; }
        public string Activity { get; set; }
        public Nullable<System.DateTime> Date { get; set; }

        // Additional properties for date filtering
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}