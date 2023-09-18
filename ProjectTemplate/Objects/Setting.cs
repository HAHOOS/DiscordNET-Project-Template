using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTemplate.Objects
{
    public class Setting
    {
        /// <summary>
        /// Name used in the JSON file and used to get in code
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Value of the setting
        /// </summary>
        public object value { get; set; }
    }
}
