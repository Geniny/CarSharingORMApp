using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSharingORMApp
{
    public class Admin : RegistredUser
    {
        public Admin(int role, int status, int id) : base(id, role, status)
        {

        }
    }
}
