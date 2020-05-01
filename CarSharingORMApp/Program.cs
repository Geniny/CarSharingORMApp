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
    enum result_code
    {
        Complete,
        Failed
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
            catch(Exception ex)
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
            Console.Write("$ email: ");
            string email = Console.ReadLine();
            Console.Write("$ login: ");
            string login = Console.ReadLine();
            Console.Write("$ password: ");
            string password = Console.ReadLine();
            Console.Write("$ phone number: ");
            string phone = Console.ReadLine();
            Console.Write("$ first name: ");
            string firstName = Console.ReadLine();
            Console.Write("$ patrynomic: ");
            string patrynomic = Console.ReadLine();
            Console.Write("$ second name: ");
            string secondName = Console.ReadLine();
            Console.Write("$ passport series: ");
            string series = Console.ReadLine();
            Console.Write("$ passport number: ");
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
                MySqlDbType = MySqlDbType.Int16,
            };
            Result.Direction = ParameterDirection.Output;
            command.Parameters.Add(Result);

            command.ExecuteNonQuery();
            Console.WriteLine("- Registration {0}", (result_code)(int)command.Parameters["Result"].Value);
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
            bool isOk = false;
            string phone = "375447474200";
            string firstName = "Andrey";
            string secondName = "Stepanko";
            string patrynomic = "Grigorevich";
            string login = "bylka";
            string email = "email@mail.ru";

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
            try
            {
                var command = currentConnection.CreateCommand();
                command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;

                command.CommandText = "SELECT * FROM `backup` WHERE `login` = @login AND `backup_creation_time` = (SELECT MAX(`backup_creation_time`) FROM `backup`)";
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    reader.Read();
                    command.Parameters.Add("@bId", MySqlDbType.Int32).Value = reader["client_id"];
                    command.Parameters.Add("@bEmail", MySqlDbType.VarChar).Value = reader["email"];
                    command.Parameters.Add("@bLogin", MySqlDbType.Int64).Value = reader["login"];
                    command.Parameters.Add("@bPassword", MySqlDbType.VarChar).Value = reader["password"];
                    command.Parameters.Add("@bPhone", MySqlDbType.VarChar).Value = reader["phoneNumber"];
                    command.Parameters.Add("@bPassport", MySqlDbType.Int32).Value = reader["passport_id"];
                    command.Parameters.Add("@bAdress", MySqlDbType.Int32).Value = reader["adress_id"];
                    command.Parameters.Add("@bRole", MySqlDbType.Int32).Value = reader["role_id"];
                    command.Parameters.Add("@bStatus", MySqlDbType.Int32).Value = reader["status_id"];
                    command.Parameters.Add("@bRegDate", MySqlDbType.DateTime).Value = reader["register_date"];
                    object backupTime = reader["backup_creation_time"];
                    string operation = reader["operation"].ToString();
                    Console.WriteLine($"- Last backup info for {login}:\n  Operation: {operation}\n  Time: {backupTime}");
                    reader.Close();

                    command.CommandText = "LOCK TABLES client WRITE";
                    command.ExecuteNonQuery();
                    if (operation == "update")
                        command.CommandText =
                            "UPDATE " +
                            "client " +
                            "SET " +
                            "`id` = @bId, `email` = @bEmail, `login` = @bLogin, `password` = @bPassword, " +
                            "`phoneNumber` = @bPhone, `passport_id` = @bPassport, `adress_id` = @bAdress, `role_id` = @bRole, " +
                            "`status_id` = @bStatus, `register_date` = @bRegDate WHERE `login` = @login";
                    if (operation == "delete")
                        command.CommandText =
                            "INSERT " +
                            "client " +
                            "SET " +
                            "`id` = @bId, `email` = @bEmail, `login` = @bLogin, `password` = @bPassword, " +
                            "`phoneNumber` = @bPhone, `passport_id` = @bPassport, `adress_id` = @bAdress, `role_id` = @bRole, " +
                            "`status_id` = @bStatus, `register_date` = @bRegDate";
                    int isUpdated = command.ExecuteNonQuery();
                    if (isUpdated > 0)
                    {
                        Console.WriteLine($"- User {login} was restored. ");
                    }
                    command.CommandText = "UNLOCK TABLES";
                    command.ExecuteNonQuery();

                }
                else
                {
                    reader.Close();
                    Console.WriteLine($"- No backup for {login}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Restoring failed: {0}", ex.Message);
            }
        }

        private static void UpdateRole(string login, int role)
        {
            try
            {
                var command = currentConnection.CreateCommand();
                command.Parameters.Add("@role", MySqlDbType.Int32).Value = role;
                command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;

                command.CommandText = "LOCK TABLES client WRITE";
                command.ExecuteNonQuery();
                command.CommandText = "UPDATE client SET `role_id` = @role WHERE `login` = @login";
                int isUpdated = command.ExecuteNonQuery();

                if (isUpdated != 0)
                {
                    Console.WriteLine($"User's role was updated to {(role)role}");
                }

                command.CommandText = "UNLOCK TABLES";
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
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
                command.CommandText = "UPDATE client SET `status_id` = @uS WHERE `login` = @uL";
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
                    switch (splittedCommand[2])
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
                    if (splittedCommand.Length < 2)
                        FindUserBy(null);
                    else
                        FindUserBy(splittedCommand[1]);

                }
                if (string.Compare(splittedCommand[0], "restore", true) == 0)
                {
                    Restore(splittedCommand[1]);

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}

