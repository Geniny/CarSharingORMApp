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

namespace CarSharingORMApp
{
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
        private static User currentUser = new UnregistredUser();
        private static bool isRunning = true;

        static void Main(string[] args)
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
            currentConnection.Open();
        }

        private static void CloseConnection()
        {
            currentConnection.Close();
        }

        private static void UserAuthorization()
        {
            Console.Write("login: ");
            string userName = Console.ReadLine();
            Console.Write("password: ");
            string password = Console.ReadLine();

            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM `client` WHERE `login` = @uL AND `password` = @uP AND `status_id` = 0", currentConnection);
            command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = userName;
            command.Parameters.Add("@uP", MySqlDbType.Int64).Value = long.Parse(password);

            adapter.SelectCommand = command;
            adapter.Fill(table);
            if (table.Rows.Count != 0)
            {
                currentUser = ((UnregistredUser)currentUser).Authorize(table.Rows[0]["role_id"].ToString(), table.Rows[0]["status_id"].ToString(), table.Rows[0]["id"].ToString());
                Console.WriteLine($"- Authorization complete -\nCurrent role: {(role)int.Parse(table.Rows[0]["role_id"].ToString())}\nCurrent status: {(status)int.Parse(table.Rows[0]["status_id"].ToString())}");
            }
            else
            {
                Console.WriteLine("Authorization failed.");
            }
        }

        private static void UserRegistration()
        {
            Console.Write("email: ");
            string email = Console.ReadLine();
            Console.Write("login: ");
            string login = Console.ReadLine();
            Console.Write("password: ");
            string password = Console.ReadLine();
            Console.Write("phone number: ");
            string phone = Console.ReadLine();

            MySqlTransaction sqlTransaction = currentConnection.BeginTransaction();

            try
            {
                MySqlCommand command = new MySqlCommand("LOCK TABLES client WRITE", currentConnection);
                command.Transaction = sqlTransaction;
                command.ExecuteNonQuery();
                command.CommandText = "INSERT INTO client " +
                    "(email, login, password, phonenumber, passport_id, adress_id, role_id, status_id) " +
                    "VALUES " +
                    "(@uEmail, @uLogin, @uPassword, @uPhone, @uPassport, @uAdress, @uRole, @uStatus)";
                command.Parameters.Add("@uEmail", MySqlDbType.VarChar).Value = email;
                command.Parameters.Add("@uLogin", MySqlDbType.VarChar).Value = login;
                command.Parameters.Add("@uPassword", MySqlDbType.Int32).Value = int.Parse(password);
                command.Parameters.Add("@uPhone", MySqlDbType.VarChar).Value = phone;
                command.Parameters.Add("@uPassport", MySqlDbType.Int16).Value = 0;
                command.Parameters.Add("@uAdress", MySqlDbType.Int16).Value = 0;
                command.Parameters.Add("@uStatus", MySqlDbType.VarChar).Value = 0;
                command.Parameters.Add("@uRole", MySqlDbType.Int16).Value = 0;
                command.ExecuteNonQuery();

                command.CommandText = "UNLOCK TABLES";
                command.ExecuteNonQuery();

                sqlTransaction.Commit();
                Console.WriteLine("User registred.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sqlTransaction.Rollback();
                return;
            }
        }

        private static void DeleteUser(string login)
        {
            MySqlTransaction sqlTransaction = currentConnection.BeginTransaction();

            try
            {
                MySqlCommand command = currentConnection.CreateCommand();
                command.Transaction = sqlTransaction;
                command.CommandText = "LOCK TABLES client WRITE";
                command.ExecuteNonQuery();
                command.CommandText = "SELECT * FROM `client` WHERE `login` = @uL";
                command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                int isExists = command.ExecuteNonQuery();

                if (isExists != 0)
                {
                    command.CommandText = "DELETE FROM client WHERE `login` = @uL";
                    int isDeleted = command.ExecuteNonQuery();
                    command.CommandText = "UNLOCK TABLES";
                    command.ExecuteNonQuery();
                    sqlTransaction.Commit();
                    if (isDeleted != 0)
                        Console.WriteLine($"User {login} was deleted.");
                }
                else
                {
                    sqlTransaction.Rollback();
                    Console.WriteLine($"User with login `{login}` doesn't exist");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                sqlTransaction.Rollback();
                return;
            }
        }
        

        private static void BlockUser(string login)
        {
            try
            {
                MySqlCommand command = currentConnection.CreateCommand();
                command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                command.Parameters.Add("@uS", MySqlDbType.Int16).Value = (int)status.Blocked;
                command.CommandText = "UPDATE client SET `status_id` = @uS WHERE `login` = @uL";
                int isChanged = command.ExecuteNonQuery();
                if (isChanged != 0)
                    Console.WriteLine($"User's status was changed to `{status.Blocked}`");
                else
                {
                    Console.WriteLine($"User with login `{login}` doesn't exist");
                }
                   
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private static void FindUserBy(string parameters)
        {
            string[] splittedParameters = parameters.Split('=');
            string field = splittedParameters[0];
            string value = splittedParameters[1];

            try
            {
                var command = currentConnection.CreateCommand();
                field = "`"+field+"`";
                command.CommandText = $"SELECT * FROM client WHERE {field} = '{value}'";
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows) 
                {
                    Console.WriteLine("id\temail\t\tlogin\t\tphone\t\tstatus");

                    while (reader.Read())
                    {
                        object id = reader.GetValue(0);
                        object email = reader.GetValue(1);
                        object login = reader.GetValue(2);
                        object password = reader.GetValue(3);
                        object phone = reader.GetValue(4);
                        object passport = reader.GetValue(5);
                        object adress = reader.GetValue(6);
                        object role = reader.GetValue(7);
                        object status = reader.GetValue(8);
                        object date = reader.GetValue(9);

                        Console.WriteLine("{0}\t{1}\t{2}\t\t{3}\t{4}", id, email, login, phone ,status);
                    }
                }

                reader.Close();

            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
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
                command.CommandText = $"CREATE EVENT IF NOT EXISTS {login + "_event"} " +
                                                        "ON  SCHEDULE AT  CURRENT_TIMESTAMP + INTERVAL @time MINUTE " +
                                                        "DO  UPDATE client SET `status_id` = 0 WHERE `login` = @uL";
                command.Parameters.Add("@time", MySqlDbType.Int32).Value = time;
                command.ExecuteNonQuery();
                Console.WriteLine($"User was blocked for {time} minutes.");
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void UpdateRole(string login, int role)
        {
            try
            {
                var command = currentConnection.CreateCommand();
                command.Parameters.Add("@role",MySqlDbType.Int32).Value = role;
                command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;

                command.CommandText = "LOCK TABLES client WRITE";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE client SET `role_id` = @role WHERE `login` = @login";
                int isUpdated = command.ExecuteNonQuery();

                if(isUpdated != 0)
                {
                    Console.WriteLine($"User's role was updated to {(role)role}");
                }

                command.CommandText = "UNLOCK TABLES";
                command.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static void UnBlockUser(string login)
        {
            try
            {
                MySqlCommand command = new MySqlCommand("LOCK TABLES client WRITE", currentConnection);
                command.ExecuteNonQuery();
                command.CommandText ="UPDATE client SET `status_id` = @uS WHERE `login` = @uL";
                command.Parameters.Add("@uL", MySqlDbType.VarChar).Value = login;
                command.Parameters.Add("@uS", MySqlDbType.Int16).Value = (int)status.Active;
                command.ExecuteNonQuery();
                Console.WriteLine($"User's status was changed to `{status.Active}`");
                command.CommandText = "UNLOCK TABLES";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
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

            if (currentUser is Admin)
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
                    switch(splittedCommand[2])
                    {
                        case "operator":
                            UpdateRole(splittedCommand[1], (int)role.Operator);
                            break;
                        case "admin":
                            UpdateRole(splittedCommand[1], (int)role.Admin);
                            break;
                        case "user":
                            UpdateRole(splittedCommand[1], (int)role.User);
                            break;
                        default: break;
                    }
                }
                if (string.Compare(splittedCommand[0], "find", true) == 0 && currentUser is Admin)
                {
                    FindUserBy(splittedCommand[1]);
                }
            }
            catch(Exception ex)
            {
                return;
            }

        }
    }
}

