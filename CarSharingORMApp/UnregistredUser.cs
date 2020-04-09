using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSharingORMApp
{
    public class UnregistredUser : User
    {
        public RegistredUser Registrate(string role, string status, string id)
        {
            return new RegistredUser(int.Parse(id), int.Parse(role), int.Parse(status));
        }

        public RegistredUser Authorize(string role, string status, string id)
        {
            switch(int.Parse(role))
            {
                case 2: return new Admin(int.Parse(id), int.Parse(role), int.Parse(status));
                case 0: return new RegistredUser(int.Parse(id), int.Parse(role), int.Parse(status));
                case 1: return null;
            }
            return new RegistredUser(int.Parse(id), int.Parse(role), int.Parse(status) );
        }
    }
}
