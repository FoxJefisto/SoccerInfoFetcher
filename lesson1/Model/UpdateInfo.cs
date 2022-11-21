using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lesson1.Model
{
    class UpdateInfo
    {
        public int Id { get; set; }
        public string ActionName { get; set; }
        public DateTime DateTime { get; set; }

        public UpdateInfo(string actionName, DateTime dateTime)
        {
            ActionName = actionName;
            DateTime = dateTime;
        }
    }
}
