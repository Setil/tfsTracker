using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimeTrackerTfs.Model
{
    public class WorkItemDTO
    {
        public WorkItemDTO() { }
        public WorkItemDTO(WorkItem wk)
        {
            ConvertWorkItem(wk);
        }
        public int Id { get; set; }
        public string AreaPath { get; set; }
        public string TeamProject { get; set; }
        public string IterationPath { get; set; }
        public string WorkItemType { get; set; }
        public string State { get; set; }
        public string Reason { get; set; }
        public string AssignedTo { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ChangedDate { get; set; }
        public string ChangedBy { get; set; }
        public string Title { get; set; }
        public double RemainingWork { get; set; }
        public string Activity { get; set; }
        public string Description { get; set; }
        public double CompletedWork { get; set; }
        public string Blocked { get; set; }
        public double OriginalEstimate { get; set; }
        public long TimeWorked { get; set; }
        public double TimeWorkedPercent
        {
            get
            {
                //60 segundos / 100 porcentagem = 600
                double div = Math.Round(((double)TimeWorked) / 600,2);
                return div;
            }
        }

        public string FormattedWorkTime
        {
            get
            {
                return formatDoubleTime((CompletedWork + TimeWorkedPercent));
            }
        }

        public string FormattedRemaining
        {
            get
            {
                return formatDoubleTime(RemainingCalc);
            }
        }

        private string formatDoubleTime(double time)
        {
            var span = TimeSpan.FromHours((time));
            string format = "{0}:{1}:{2}";
            return string.Format(format, (span.Hours + (span.Days*24)).ToString("00"), Math.Abs(span.Minutes).ToString("00"), span.Seconds.ToString("00"));
        }

        public double RemainingCalc
        {
            get {
                return Math.Round((OriginalEstimate == 0 ? RemainingWork : OriginalEstimate) - CompletedWork, 2);
            }
        }

        public string StartImage
        {
            get
            {
                if (Blocked == "Yes")
                    return "Images/starty.png";
                else
                    return "Images/start.png";
            }
        }

        private void ConvertWorkItem(WorkItem wk)
        {
            this.Id = wk.Id.Value;
            foreach(var item in wk.Fields)
            {
                var prop = this.GetType().GetProperty(item.Key.Split('.').Last());
                if(prop != null)
                    prop.SetValue(this, item.Value);
            }
        }

    }
}
