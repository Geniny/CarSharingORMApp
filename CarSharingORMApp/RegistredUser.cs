using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSharingORMApp
{
    public class RegistredUser : User
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int StatusId { get; set; }

        public RegistredUser(int id, int role, int status)
        {
            this.Id = id;
            this.RoleId = role;
            this.StatusId = status;
        }
    }
}
