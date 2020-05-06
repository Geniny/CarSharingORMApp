using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using MySqlConnector;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading;
using ConsoleTables;
using System.Globalization;

namespace CarSharingORMApp
{
    enum result_code
    {
        Failed,
        Complete
    }
    enum status
    {
        Active,
        Blocked
    }
    enum role
    {
        User,
        Operator,
        Admin
    }

    class Program
    {
        private static MySqlConnection currentConnection;
        private const string ConnectionString = "server = 192.168.190.130; port=3306;username=student;password=123;database=app_schema";
        private static User currentUser = new Admin(0, 0, 0) { RoleId = 2, Id = 0, StatusId = 0 }; //new UnregistredUser();
        private static bool isRunning = true;
        private static int authCount = 0;

        static void Main(string[] args)
        {
            try
            {
                Setup();
                OpenConnection();
                CheckConnection();
                while (isRunning)
                {
                    Console.Write("> ");
                    HandleCommand(Console.ReadLine());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
        }

        private static void Setup()
        {
            currentConnection = new MySqlConnection(ConnectionString);
        }

        private static void CheckConnection()
        {
            Console.WriteLine($"Connection state: {currentConnection.State}");
        }

        private static void OpenConnection()
        {
            try
            {
                currentConnection.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void CloseConnection()
        {
            currentConnection.Close();
        }

        private static void UserAuthorization()
        {
            Console.Write("  login: ");
            string login = Console.ReadLine();
            Console.Write("  password: ");
            string password = Console.ReadLine();

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "Authorize";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Password = new MySqlParameter
            {
                ParameterName = "Password",
                MySqlDbType = MySqlDbType.Int32,
                Value = int.Parse(password)
            };
            command.Parameters.Add(Password);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int16,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);
            Console.WriteLine("- Client status: {0}", command.Parameters["Result"].Value);
            if (table.Rows.Count != 0)
            {
                UnLogging();
                currentUser = ((UnregistredUser)currentUser).Authorize(table.Rows[0]["role_id"].ToString(), table.Rows[0]["status_id"].ToString(), table.Rows[0]["id"].ToString());
            }
            else
            {
                if (authCount > 3)
                {
                    Console.WriteLine("- Login limit exceeded, wait 1 min to another attemp");
                    authCount = 0;
                    Thread.Sleep(100);
                    Console.WriteLine("- 1 min left");
                }
                else
                {
                    authCount++;
                }
            }
        }

        private static void UserRegistration()
        {
            Console.Write("  email: ");
            string email = Console.ReadLine();
            Console.Write("  login: ");
            string login = Console.ReadLine();
            Console.Write("  password: ");
            string password = Console.ReadLine();
            Console.Write("  phone number: ");
            string phone = Console.ReadLine();
            Console.Write("  first name: ");
            string firstName = Console.ReadLine();
            Console.Write("  patrynomic: ");
            string patrynomic = Console.ReadLine();
            Console.Write("  second name: ");
            string secondName = Console.ReadLine();
            Console.Write("  passport series: ");
            string series = Console.ReadLine();
            Console.Write("  passport number: ");
            string number = Console.ReadLine();

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "Register";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Password = new MySqlParameter
            {
                ParameterName = "Password",
                MySqlDbType = MySqlDbType.Int32,
                Value = int.Parse(password)
            };
            command.Parameters.Add(Password);

            MySqlParameter Email = new MySqlParameter
            {
                ParameterName = "Email",
                Value = email
            };
            command.Parameters.Add(Email);

            MySqlParameter Phone = new MySqlParameter
            {
                ParameterName = "Phone",
                Value = phone
            };
            command.Parameters.Add(Phone);

            MySqlParameter FirstName = new MySqlParameter
            {
                ParameterName = "FirstName",
                Value = firstName
            };
            command.Parameters.Add(FirstName);

            MySqlParameter SecondName = new MySqlParameter
            {
                ParameterName = "SecondName",
                Value = secondName
            };
            command.Parameters.Add(SecondName);

            MySqlParameter Patrynomic = new MySqlParameter
            {
                ParameterName = "Patrynomic",
                Value = patrynomic
            };
            command.Parameters.Add(Patrynomic);

            MySqlParameter Series = new MySqlParameter
            {
                ParameterName = "Series",
                Value = series
            };
            command.Parameters.Add(Series);

            MySqlParameter Number = new MySqlParameter
            {
                ParameterName = "Number",
                Value = number
            };
            command.Parameters.Add(Number);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Register command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void AddTarif()
        {
            int id;
            string tarifName;
            string description;
            int perHourCost;
            int perMileCost;
            Console.Write("  id: ");
            id = int.Parse(Console.ReadLine());
            Console.Write("  tarif name: ");
            tarifName = Console.ReadLine();
            Console.Write("  description: ");
            description = Console.ReadLine();
            Console.Write("  cost per hourt: ");
            perHourCost = int.Parse(Console.ReadLine());
            Console.Write("  cost per mile: ");
            perMileCost = int.Parse(Console.ReadLine());
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddTarif";

            MySqlParameter Id = new MySqlParameter
            {
                ParameterName = "Id",
                Value = id,
                MySqlDbType = MySqlDbType.Int32
                
            };
            command.Parameters.Add(Id);

            MySqlParameter TarifName = new MySqlParameter
            {
                ParameterName = "TarifName",
                Value = tarifName
            };
            command.Parameters.Add(TarifName);

            MySqlParameter Description = new MySqlParameter
            {
                ParameterName = "Description",
                Value = description
            };
            command.Parameters.Add(Description);

            MySqlParameter PerHourCost = new MySqlParameter
            {
                ParameterName = "PerHourCost",
                Value = perHourCost
            };
            command.Parameters.Add(PerHourCost);

            MySqlParameter PerMileCost = new MySqlParameter
            {
                ParameterName = "PerMileCost",
                Value = perMileCost
            };
            command.Parameters.Add(PerMileCost);
            command.ExecuteNonQuery();
            Console.WriteLine("- Add tarif command status: Complete");
        }

        private static void UpdateUser(string login)
        {
            Console.Write("  email: ");
            string email = Console.ReadLine();
            Console.Write("  new login: ");
            string newLogin = Console.ReadLine();
            Console.Write("  password: ");
            string password = Console.ReadLine();
            Console.Write("  phone number: ");
            string phone = Console.ReadLine();
            Console.Write("  first name: ");
            string firstName = Console.ReadLine();
            Console.Write("  patrynomic: ");
            string patrynomic = Console.ReadLine();
            Console.Write("  second name: ");
            string secondName = Console.ReadLine();

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateUser";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter NewLogin = new MySqlParameter
            {
                ParameterName = "NewLogin",
                Value = newLogin
            };
            command.Parameters.Add(NewLogin);

            MySqlParameter Password = new MySqlParameter
            {
                ParameterName = "Password",
                MySqlDbType = MySqlDbType.Int32,
                Value = int.Parse(password)
            };
            command.Parameters.Add(Password);

            MySqlParameter Email = new MySqlParameter
            {
                ParameterName = "Email",
                Value = email
            };
            command.Parameters.Add(Email);

            MySqlParameter Phone = new MySqlParameter
            {
                ParameterName = "Phone",
                Value = phone
            };
            command.Parameters.Add(Phone);

            MySqlParameter FirstName = new MySqlParameter
            {
                ParameterName = "FirstName",
                Value = firstName
            };
            command.Parameters.Add(FirstName);

            MySqlParameter SecondName = new MySqlParameter
            {
                ParameterName = "SecondName",
                Value = secondName
            };
            command.Parameters.Add(SecondName);

            MySqlParameter Patrynomic = new MySqlParameter
            {
                ParameterName = "Patrynomic",
                Value = patrynomic
            };
            command.Parameters.Add(Patrynomic);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update user command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void UpdateTarif(string tarifName)
        {
            string newTarifName;
            string description;
            int perHourCost;
            int perMileCost;
            Console.Write("  new tarif name: ");
            newTarifName = Console.ReadLine();
            Console.Write("  description: ");
            description = Console.ReadLine();
            Console.Write("  cost per hourt: ");
            perHourCost = int.Parse(Console.ReadLine());
            Console.Write("  cost per mile: ");
            perMileCost = int.Parse(Console.ReadLine());
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateTarif";

            MySqlParameter TarifName = new MySqlParameter
            {
                ParameterName = "TarifName",
                Value = tarifName
            };
            command.Parameters.Add(TarifName);

            MySqlParameter NewTarifName = new MySqlParameter
            {
                ParameterName = "NewTarifName",
                Value = newTarifName
            };
            command.Parameters.Add(NewTarifName);

            MySqlParameter Description = new MySqlParameter
            {
                ParameterName = "Description",
                Value = description
            };
            command.Parameters.Add(Description);

            MySqlParameter PerHourCost = new MySqlParameter
            {
                ParameterName = "PerHourCost",
                Value = perHourCost
            };
            command.Parameters.Add(PerHourCost);

            MySqlParameter PerMileCost = new MySqlParameter
            {
                ParameterName = "PerMileCost",
                Value = perMileCost
            };
            command.Parameters.Add(PerMileCost);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update tarif command status: {0}", 
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void DeleteUser(string login)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "DeleteClient";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);
            command.ExecuteNonQuery();
            Console.WriteLine("- Delete command status: {0}", (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void UpdateCar(string number)
        {
            string model;
            string color;
            int capacity;

            Console.Write("  model: ");
            model = Console.ReadLine();
            Console.Write("  color: ");
            color = Console.ReadLine();
            Console.Write("  capacity: ");
            capacity = int.Parse(Console.ReadLine());

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateCar";

            MySqlParameter Capacity = new MySqlParameter
            {
                ParameterName = "Capacity",
                Value = capacity,
                MySqlDbType = MySqlDbType.Int32

            };
            command.Parameters.Add(Capacity);

            MySqlParameter Model = new MySqlParameter
            {
                ParameterName = "Model",
                Value = model
            };
            command.Parameters.Add(Model);

            MySqlParameter Color = new MySqlParameter
            {
                ParameterName = "Color",
                Value = color
            };
            command.Parameters.Add(Color);

            MySqlParameter Number = new MySqlParameter
            {
                ParameterName = "Number",
                Value = number
            };
            command.Parameters.Add(Number);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update car command status: {0}", 
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void ShowClients()
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "ShowClients";

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);


            Console.WriteLine("- Clients count: {0}", consoleTable.Rows.Count);
        }

        private static void AddProduct()
        {
            string carNumber;
            string companyName;
            string tarifName;
            string productName;
            Console.Write("  tarif name: ");
            tarifName = Console.ReadLine();
            Console.Write("  car number: ");
            carNumber = Console.ReadLine();
            Console.Write("  company name: ");
            companyName = Console.ReadLine();
            Console.Write("  product name: ");
            productName = Console.ReadLine();
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddProduct";

            MySqlParameter TarifName = new MySqlParameter
            {
                ParameterName = "TarifName",
                Value = tarifName
            };
            command.Parameters.Add(TarifName);

            MySqlParameter CompanyName = new MySqlParameter
            {
                ParameterName = "CompanyName",
                Value = companyName
            };
            command.Parameters.Add(CompanyName);

            MySqlParameter ProductName = new MySqlParameter
            {
                ParameterName = "ProductName",
                Value = productName
            };
            command.Parameters.Add(ProductName);

            MySqlParameter CarNumber = new MySqlParameter
            {
                ParameterName = "CarNumber",
                Value = carNumber
            };
            command.Parameters.Add(CarNumber);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Add product command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void AddBank()
        {
            string name;

            Console.Write("  name: ");
            name = Console.ReadLine();

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddBank";

            MySqlParameter BankName = new MySqlParameter
            {
                ParameterName = "BankName",
                Value = name,
            };
            command.Parameters.Add(BankName);

            command.ExecuteNonQuery();
            Console.WriteLine("- Add bank command status: Complete");
        }

        private static void UpdateBank(string name)
        {
            string newName;

            Console.Write("  new name: ");
            newName = Console.ReadLine();

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateBank";

            MySqlParameter BankName = new MySqlParameter
            {
                ParameterName = "BankName",
                Value = name,
            };
            command.Parameters.Add(BankName);

            MySqlParameter NewBankName = new MySqlParameter
            {
                ParameterName = "NewBankName",
                Value = newName,
            };
            command.Parameters.Add(NewBankName);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update bank command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void AddCompany()
        {
            bool isOk = false;
            string name;
            string bankAccountNumber;

            Console.Write("  name: ");
            name = Console.ReadLine();
            do
            {
                Console.Write("  bank account number: ");
                bankAccountNumber = Console.ReadLine();
                if (bankAccountNumber.Length != 8)
                {
                    isOk = true;
                    Console.WriteLine("- Reenter field 'Bank account number'");
                }
                else
                    isOk = false;
            }
            while (isOk);

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddCompany";

            MySqlParameter CompanyName = new MySqlParameter
            {
                ParameterName = "CompanyName",
                Value = name,
            };
            command.Parameters.Add(CompanyName);

            MySqlParameter BankAccountNumber = new MySqlParameter
            {
                ParameterName = "BankAccountNumber",
                Value = bankAccountNumber,
            };
            command.Parameters.Add(BankAccountNumber);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Add company command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void UpdateCompany(string name)
        {
            bool isOk = false;
            string newName;
            string bankAccountNumber;
            Console.Write("  new name: ");
            newName = Console.ReadLine();
            do
            {
                Console.Write("  bank account number: ");
                bankAccountNumber = Console.ReadLine();
                if (bankAccountNumber.Length != 8)
                {
                    isOk = true;
                    Console.WriteLine("- Reenter field 'Bank account number'");
                }
                else
                    isOk = false;
            }
            while (isOk);

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateCompany";

            MySqlParameter CompanyName = new MySqlParameter
            {
                ParameterName = "CompanyName",
                Value = name,
            };
            command.Parameters.Add(CompanyName);

            MySqlParameter NewCompanyName = new MySqlParameter
            {
                ParameterName = "NewCompanyName",
                Value = newName,
            };
            command.Parameters.Add(NewCompanyName);

            MySqlParameter NewBankAccountNumber = new MySqlParameter
            {
                ParameterName = "NewBankAccountNumber",
                Value = bankAccountNumber,
            };
            command.Parameters.Add(NewBankAccountNumber);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update company command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void AddBankAccount()
        {
            bool isOk = false;
            string name;
            string number;

            Console.Write("  bank name: ");
            name = Console.ReadLine();

            do
            {
                Console.Write("  bank account number: ");
                number = Console.ReadLine();
                if (number.Length != 8)
                {
                    isOk = true;
                    Console.WriteLine("- Reenter field 'Bank account number'");
                }
                else
                    isOk = false;
            }
            while (isOk);

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddBankAccount";

            MySqlParameter BankName = new MySqlParameter
            {
                ParameterName = "BankName",
                Value = name,
            };
            command.Parameters.Add(BankName);

            MySqlParameter Number = new MySqlParameter
            {
                ParameterName = "Number",
                Value = number,
            };
            command.Parameters.Add(Number);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Add bank account command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void ShowCarsModel()
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "OrderCarByModel";

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
            Console.WriteLine("- Cars count: {0}", consoleTable.Rows.Count); 
        }

        private static void ShowCarsState()
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "OrderCarByState";

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
            Console.WriteLine("- Cars count: {0}", consoleTable.Rows.Count);
        }

        private static void ShowCompanies()
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "ShowCompanies";

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
        }

        private static void ShowProducts(string type)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            switch(type)
            {
                case "name": command.CommandText = "OrderProductByName"; break;
                case "cost": command.CommandText = "OrderProductByCost"; break;
                case null: command.CommandText = "ShowProducts"; break;
                default: command.CommandText = "ShowProducts"; break;
            }

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
        }

        private static void ShowClient (string login)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "ShowClientStatus";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login,
            };
            command.Parameters.Add(Login);

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
        }

        private static void ShowCars()
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "ShowFreeCars";
            MySqlParameter Count = new MySqlParameter
            {
                ParameterName = "Count",
                MySqlDbType = MySqlDbType.Int32,
            };
            Count.Direction = ParameterDirection.Output;
            command.Parameters.Add(Count);

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();

            adapter.SelectCommand = command;
            adapter.Fill(table);

            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
            Console.WriteLine("- Cars count: {0}", (int)command.Parameters["Count"].Value);
        }
        
        private static void AddCar()
        {
            string model;
            string color;
            string number;
            int capacity;

            Console.Write("  model: ");
            model = Console.ReadLine();
            Console.Write("  color: ");
            color = Console.ReadLine();
            Console.Write("  number: ");
            number = Console.ReadLine();
            Console.Write("  capacity: ");
            capacity = int.Parse(Console.ReadLine());

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "AddCar";

            MySqlParameter Capacity = new MySqlParameter
            {
                ParameterName = "Capacity",
                Value = capacity,
                MySqlDbType = MySqlDbType.Int32

            };
            command.Parameters.Add(Capacity);

            MySqlParameter Model = new MySqlParameter
            {
                ParameterName = "Model",
                Value = model
            };
            command.Parameters.Add(Model);

            MySqlParameter Color = new MySqlParameter
            {
                ParameterName = "Color",
                Value = color
            };
            command.Parameters.Add(Color);

            MySqlParameter Number = new MySqlParameter
            {
                ParameterName = "Number",
                Value = number
            };
            command.Parameters.Add(Number);

            command.ExecuteNonQuery();
            Console.WriteLine("- Add car command status: Complete");
        }

        private static void BlockUser(string login)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "BlockUser";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);
            command.ExecuteNonQuery();
            Console.WriteLine("- Block command status: {0}", (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void FindUserBy(string parameters)
        {
            bool isOk = false;
            string phone ;
            string firstName ;
            string secondName ;
            string patrynomic ;
            string login ;
            string email ;
            Console.Write("  First name: ");
            firstName = Console.ReadLine();
            Console.Write("  Second name: ");
            secondName = Console.ReadLine();
            Console.Write("  Patrynomic: ");
            patrynomic = Console.ReadLine();
            Console.Write("  Login: ");
            login = Console.ReadLine();
            do
            {
                Console.Write("  PhoneNumber:");
                phone = Console.ReadLine();
                if (phone.Length > 20)
                {
                    isOk = true;
                    Console.WriteLine("- Reenter field 'PhoneNumber'");
                }
                else
                    isOk = false;
            }
            while (isOk);
            do
            {
                Console.Write("  Email:");
                email = Console.ReadLine();
                if (email.Length > 20)
                {
                    isOk = true;
                    Console.WriteLine("- Reenter field 'Email'");
                }
                else
                    isOk = false;
            }
            while (isOk);
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "FindUser";
            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);
            MySqlParameter FirstName = new MySqlParameter
            {
                ParameterName = "FirstName",
                Value = firstName
            };
            command.Parameters.Add(FirstName);
            MySqlParameter SecondName = new MySqlParameter
            {
                ParameterName = "SecondName",
                Value = secondName
            };
            command.Parameters.Add(SecondName);
            MySqlParameter Patrynomic = new MySqlParameter
            {
                ParameterName = "Patrynomic",
                Value = patrynomic
            };
            command.Parameters.Add(Patrynomic);
            MySqlParameter PhoneNumber = new MySqlParameter
            {
                ParameterName = "Phone",
                Value = phone
            };
            command.Parameters.Add(PhoneNumber);
            MySqlParameter Email = new MySqlParameter
            {
                ParameterName = "Email",
                Value = email
            };
            command.Parameters.Add(Email);
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            adapter.SelectCommand = command;
            adapter.Fill(table);
            if (table.Rows.Count != 0)
            {
                DataRow row = table.Rows[0];
                Console.WriteLine("- User info: ");
                Console.WriteLine("  Id: {0}", row["id"]);
                Console.WriteLine("  First name: {0}", row["firstname"]);
                Console.WriteLine("  Second name: {0}", row["secondname"]);
                Console.WriteLine("  Patrynomic: {0}", row["patrynomic"]);
                Console.WriteLine("  Phone: {0}", row["phone"]);
                Console.WriteLine("  Email: {0}", row["email"]);
                Console.WriteLine("  Role: {0}", row["role"]);
                Console.WriteLine("  Status: {0}", row["status"]);
                Console.WriteLine("  Register date: {0}", row["register"]);
            }
            else
            {
                Console.WriteLine("- User not found");
            }

        }

        private static void TimeBlockUser(string login, int time)
        {
            try
            {
                MySqlCommand command = new MySqlCommand("LOCK TABLES client WRITE", currentConnection);
                command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                command.Parameters.Add("@uS", MySqlDbType.Int16).Value = (int)status.Blocked;
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE client SET `status_id` = @uS WHERE `login` = @uL";
                int isChanged = command.ExecuteNonQuery();
                if (isChanged != 0)
                    Console.WriteLine($"User's status was changed to `{status.Blocked}`");
                else
                {
                    Console.WriteLine($"User with login `{login}` doesn't exist");
                }
                command.CommandText = "UNLOCK TABLES";
                command.ExecuteNonQuery();

                DataTable table = new DataTable();
                MySqlDataAdapter adapter = new MySqlDataAdapter();
                command.CommandText = "SELECT * FROM `client` WHERE `login` = @uL";
                adapter.SelectCommand = command;
                adapter.Fill(table);

                command.CommandText = $"CREATE EVENT IF NOT EXISTS {login + "_event"} " +
                                                        "ON  SCHEDULE AT  CURRENT_TIMESTAMP + INTERVAL @time MINUTE " +
                                                        "DO  UPDATE client SET `status_id` = 0 WHERE `login` = @uL";
                command.Parameters.Add("@time", MySqlDbType.Int32).Value = time;
                command.ExecuteNonQuery();
                command.CommandText = $"INSERT INTO block_log (client_id, start_time, end_time) VALUES ({table.Rows[0]["id"]}, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP + INTERVAL @time MINUTE)";
                command.ExecuteNonQuery();
                Console.WriteLine($"User was blocked for {time} minutes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void Restore(string login)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "RestoreBackup";

            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Restore command status: {0}", (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void UpdateRole(string login, string role)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UpdateRole";
            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Role = new MySqlParameter
            {
                ParameterName = "Role",
                Value = role
            };
            command.Parameters.Add(Role);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Update role command status: {0}", 
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void UnBlockUser(string login)
        {
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "UnblockUser";
            MySqlParameter Login = new MySqlParameter
            {
                ParameterName = "Login",
                Value = login
            };
            command.Parameters.Add(Login);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Unblock command status: {0}", (result_code)(int)command.Parameters["Result"].Value);

        }

        private static void AvailableCommands()
        {
            Console.WriteLine("Available functionality");
            Console.WriteLine("- Help          [help       | h]");
            if (currentUser is UnregistredUser)
            {
                Console.WriteLine("- Registration  [registrare | r]");
                Console.WriteLine("- Authorization [log        | a]");
            }
            if (currentUser is Admin && (((Admin)currentUser).StatusId != (int)status.Blocked))
            {
                Console.WriteLine("- Delete user   [delete     |  ]");
                Console.WriteLine("- Block user    [block      |  ]");
                Console.WriteLine("- Update user   [update     |  ]");
                Console.WriteLine("- UnBlock user  [unblock    |  ]");
                Console.WriteLine("- Update role   [updaterole |  ]");
            }
            Console.WriteLine("- Unlog         [unlog      |  ]");
            Console.WriteLine("- Exit          [exit       | e] ");
        }

        private static void UnLogging()
        {
            currentUser = new UnregistredUser();
        }

        private static void ShowSalesStat ()
        {
            Console.Write("  year: ");
            int year = int.Parse(Console.ReadLine());
            Console.Write("  month: ");
            int month = int.Parse(Console.ReadLine());
            Console.Write("  start day: ");
            int startDay = int.Parse(Console.ReadLine());
            Console.Write("  end day: ");
            int endDay = int.Parse(Console.ReadLine());
            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "ShowSalesStat";
            MySqlParameter Year = new MySqlParameter
            {
                ParameterName = "StatYear",
                Value = year,
                MySqlDbType = MySqlDbType.Int32
            };
            command.Parameters.Add(Year);
            MySqlParameter StatMonth = new MySqlParameter
            {
                ParameterName = "StatMonth",
                Value = month,
                MySqlDbType = MySqlDbType.Int32
            };
            command.Parameters.Add(StatMonth);
            MySqlParameter StartDay = new MySqlParameter
            {
                ParameterName = "StartDay",
                Value = startDay,
                MySqlDbType = MySqlDbType.Int32
            };
            command.Parameters.Add(StartDay);
            MySqlParameter EndDay = new MySqlParameter
            {
                ParameterName = "EndDay",
                Value = endDay,
                MySqlDbType = MySqlDbType.Int32
            };
            command.Parameters.Add(EndDay);
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            adapter.SelectCommand = command;
            adapter.Fill(table);
            List<string> columns = new List<string>();
            foreach (DataColumn column in table.Columns)
            {
                columns.Add(column.ColumnName);
            }
            var consoleTable = new ConsoleTable(columns.ToArray());
            foreach (DataRow row in table.Rows)
            {
                consoleTable.AddRow(row.ItemArray);
            }
            Console.WriteLine();
            consoleTable.Write(Format.Alternative);
            Console.WriteLine("- Sales count: {0}", consoleTable.Rows.Count);
        }

        private static void BookCar()
        {
            Console.Write("  product name: ");
            string productName = Console.ReadLine();
            Console.Write("  start date: ");
            DateTime startDate = DateTime.Parse(Console.ReadLine());
            Console.Write("  end date: ");
            DateTime endDate = DateTime.Parse(Console.ReadLine());

            var command = currentConnection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = "BookCar";

            MySqlParameter ProductName = new MySqlParameter
            {
                ParameterName = "ProductName",
                Value = productName
            };
            command.Parameters.Add(ProductName);

            MySqlParameter StartDate = new MySqlParameter
            {
                ParameterName = "StartDate",
                MySqlDbType = MySqlDbType.DateTime,
                Value = startDate
            };
            command.Parameters.Add(StartDate);

            MySqlParameter EndDate = new MySqlParameter
            {
                ParameterName = "EndDate",
                MySqlDbType = MySqlDbType.DateTime,
                Value = endDate
            };
            command.Parameters.Add(EndDate);

            MySqlParameter ClientId = new MySqlParameter
            {
                ParameterName = "ClientId",
                MySqlDbType = MySqlDbType.Int32,
                Value = ((Admin)currentUser).Id
            };
            command.Parameters.Add(ClientId);

            MySqlParameter Result = new MySqlParameter
            {
                ParameterName = "Result",
                MySqlDbType = MySqlDbType.Int32,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Book car command status: {0}",
                (result_code)(int)command.Parameters["Result"].Value);
        }

        private static void HandleCommand(string command)
        {
            try
            {
                string[] splittedCommand = command.Split(' ');
                if (string.Compare(command, "log", true) == 0 || string.Compare(command, "a", true) == 0 && currentUser is UnregistredUser)
                {
                    UserAuthorization();
                }
                if (string.Compare(command, "help", true) == 0 || string.Compare(command, "h", true) == 0)
                {
                    AvailableCommands();
                }
                if (string.Compare(command, "register", true) == 0 || string.Compare(command, "r", true) == 0 && currentUser is UnregistredUser)
                {
                    UserRegistration();
                }
                if (string.Compare(command, "exit", true) == 0 || string.Compare(command, "e", true) == 0)
                {
                    CloseConnection();
                    CheckConnection();
                    isRunning = false;
                }
                if (string.Compare(command, "unlog", true) == 0 || string.Compare(command, "r", true) == 0 && currentUser is UnregistredUser)
                {
                    UnLogging();
                }
                if (string.Compare(splittedCommand[0], "block", true) == 0 && currentUser is Admin)
                {
                    if (string.Compare(splittedCommand[1], "time", true) == 0)
                        TimeBlockUser(splittedCommand[2], int.Parse(splittedCommand[3]));
                    else
                        BlockUser(splittedCommand[1]);
                }
                if (string.Compare(splittedCommand[0], "unblock", true) == 0 && currentUser is Admin)
                {
                    UnBlockUser(splittedCommand[1]);
                }
                if (string.Compare(splittedCommand[0], "delete", true) == 0 && currentUser is Admin)
                {
                    DeleteUser(splittedCommand[1]);
                }
                if (string.Compare(splittedCommand[0], "updaterole", true) == 0 && currentUser is Admin)
                {
                    UpdateRole(splittedCommand[1], splittedCommand[2]);
                }
                if (string.Compare(splittedCommand[0], "find", true) == 0 && currentUser is Admin)
                {
                    if (splittedCommand.Length < 2)
                        FindUserBy(null);
                    else
                        FindUserBy(splittedCommand[1]);

                }
                if (string.Compare(splittedCommand[0], "restore", true) == 0)
                {
                    Restore(splittedCommand[1]);

                }
                if (string.Compare(splittedCommand[0], "bookcar", true) == 0)
                {
                    BookCar();

                }
                if (string.Compare(splittedCommand[0], "updatetarif", true) == 0 && currentUser is Admin)
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 1; i < splittedCommand.Length; i++)
                    {
                        builder.Append(" ");
                        builder.Append(splittedCommand[i]);
                    }
                    UpdateTarif(builder.ToString().Trim());
                }

                if (string.Compare(splittedCommand[0], "add", true) == 0 && currentUser is Admin)
                {
                    if (string.Compare(splittedCommand[1], "car", true) == 0)
                    {
                        AddCar();
                    }
                    if (string.Compare(splittedCommand[1], "bank", true) == 0)
                    {
                        AddBank();
                    }
                    if (string.Compare(splittedCommand[1], "company", true) == 0)
                    {
                        AddCompany();
                    }
                    if (string.Compare(splittedCommand[1], "bankaccount", true) == 0)
                    {
                        AddBankAccount();
                    }
                    if (string.Compare(splittedCommand[1], "product", true) == 0)
                    {
                        AddProduct();
                    }
                    if (string.Compare(splittedCommand[1], "tarif", true) == 0)
                    {
                        AddTarif();
                    }
                }

                if (string.Compare(splittedCommand[0], "update", true) == 0 && currentUser is Admin)
                {
                    if (string.Compare(splittedCommand[1], "car", true) == 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 2; i < splittedCommand.Length; i++)
                        {
                            builder.Append(" ");
                            builder.Append(splittedCommand[i]);
                        }
                        UpdateCar(builder.ToString().Trim());
                    }
                    if (string.Compare(splittedCommand[1], "bank", true) == 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 2; i < splittedCommand.Length; i++)
                        {
                            builder.Append(" ");
                            builder.Append(splittedCommand[i]);
                        }
                        UpdateBank(builder.ToString().Trim());
                    }
                    if (string.Compare(splittedCommand[1], "company", true) == 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 2; i < splittedCommand.Length; i++)
                        {
                            builder.Append(" ");
                            builder.Append(splittedCommand[i]);
                        }
                        UpdateCompany(builder.ToString().Trim());
                    }

                    if (string.Compare(splittedCommand[1], "user", true) == 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 2; i < splittedCommand.Length; i++)
                        {
                            builder.Append(" ");
                            builder.Append(splittedCommand[i]);
                        }
                        UpdateUser(builder.ToString().Trim());
                    }
                    if (string.Compare(splittedCommand[1], "role", true) == 0)
                    {
                        UpdateRole(splittedCommand[2], splittedCommand[3]);
                    }
                    if (string.Compare(splittedCommand[1], "tarif", true) == 0)
                    {
                        StringBuilder builder = new StringBuilder();
                        for (int i = 2; i < splittedCommand.Length; i++)
                        {
                            builder.Append(" ");
                            builder.Append(splittedCommand[i]);
                        }
                        UpdateTarif(builder.ToString().Trim());
                    }


                }

                if (string.Compare(splittedCommand[0], "show", true) == 0 && currentUser is Admin)
                {
                    if (string.Compare(splittedCommand[1], "cars", true) == 0)
                    {
                        if (string.Compare(splittedCommand[2], string.Empty, true) == 0)
                        {
                            ShowCars();
                        }
                        if (string.Compare(splittedCommand[2], "model", true) == 0)
                        {
                            ShowCarsModel();
                        }
                        if (string.Compare(splittedCommand[2], "state", true) == 0)
                        {
                            ShowCarsState();
                        }
                        
                    }
                    if (string.Compare(splittedCommand[1], "products", true) == 0)
                    {
                        string result = null;
                        if (splittedCommand.Length == 3)
                            result = splittedCommand[2];
                        ShowProducts(result);
                    }
                    if (string.Compare(splittedCommand[1], "companies", true) == 0)
                    {
                        ShowCompanies();
                    }
                    if (string.Compare(splittedCommand[1], "clientstatus", true) == 0)
                    {
                        ShowClient(splittedCommand[2]);
                    }
                    if (string.Compare(splittedCommand[1], "clients", true) == 0)
                    {
                        ShowClients();
                    }
                    if (string.Compare(splittedCommand[1], "sales", true) == 0)
                    {
                        ShowSalesStat();
                    }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("- " + ex.Message);
            }

        }
    }
}

