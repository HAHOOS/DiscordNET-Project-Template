using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectTemplate.Objects
{
    public class Preset
    {
        public string name { get; set; }
        public string token { get; set; }
        public List<Setting> settings { get; set; }

        public void ChangeOption(string option, object to)
        {
            bool changed = false;
            if (option == "name")
            {
                name = (string)to;
                changed = true;
            }
            else if (option == "token")
            {
                token = (string)to;
                changed = true;
            }
            else
            {
                foreach (Setting setting in settings)
                {
                    if (setting.name == option)
                    {
                        setting.value = to;
                        changed = true; break;
                    }
                }
            }
            if (!changed)
            {
                settings.Add(new Setting()
                {
                    name = option,
                    value = to
                });
            }
        }
        public object GetValueOfOption(string opt)
        {
            if (opt == "name") { return name; }
            else if (opt == "token") { return token; }
            else
            {
                foreach (Setting setting in settings) { if (setting.name == opt) { return setting.value; } }
                return null;
            }
        }
    }
}
