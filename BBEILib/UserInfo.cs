using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BBEILib
{
    public class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RoleId { get; set; }
        public string Role { get; set; }
        public string SecondaryEmail { get; set; }
        public string PhoneNumber { get; set; }
        public string Name { get; set; }

        public UserInfo()
        {
            UserId = "";
            UserName = "";
            RoleId = "";
            Role = "";
            SecondaryEmail = "";
            PhoneNumber = "";
            Name = "";
        }
    }
}
