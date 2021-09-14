using System;
using System.Data.SqlClient;

namespace SqlConnectionProj
{
    class Program
    {
        static void Main(string[] args)
        {
            //var conString = "Data source=localhost; Initial catalog=AcademySummer; Integrated security=true";
            var conString = "" +
                "Data source=localhost; " +
                "Initial catalog=AcademySummer; " +
                "user id=sa; " +
                "password=1234";
            while (true)
            {
                Console.Write("1. Create client\n2. Create Account\n3. Client list\n4. Transfer from acc to acc\nChoice:");
                int.TryParse(Console.ReadLine(), out var choice);
                switch (choice)
                {
                    case 1:
                        {
                            CreateClient(conString);
                        }
                        break;
                    case 2:
                        break;
                    case 3:
                        {
                            ListClient(conString);
                        }
                        break;
                    case 4:
                        {
                            Console.Write("From acc");
                            var fromAcc = Console.ReadLine();

                            Console.Write("To acc");
                            var toAcc = Console.ReadLine();

                            Decimal.TryParse(Console.ReadLine(), out var amount);

                            TransferFromToAcc(fromAcc, toAcc, amount, conString);
                        }
                        break;
                    default:
                        Console.WriteLine("Wrong command.");
                        break;
                }
            }
        }

        private static void TransferFromToAcc(string fromAcc, string toAcc, decimal amount, string conString)
        {
            if (string.IsNullOrEmpty(fromAcc) || string.IsNullOrEmpty(toAcc) || amount == 0)
            {
                Console.WriteLine("Something went wrong.");
                return;
            }

            var conn = new SqlConnection(conString);
            conn.Open();

            SqlTransaction sqlTransaction = conn.BeginTransaction();

            var command = conn.CreateCommand();

            command.Transaction = sqlTransaction;

            try
            {
                command.CommandText = "select sum( case when t.Type = 'C' then t.Amount * -1 else t.Amount end) from Transactions t left join Accounts a on t.Account_Id = a.Id where a.Number = @fromAcc";
                command.Parameters.AddWithValue("@fromAcc", fromAcc);
                var reader = command.ExecuteReader();
                var fromAccBalance = 0m;

                while (reader.Read())
                {
                    fromAccBalance = !string.IsNullOrEmpty(reader.GetValue(0)?.ToString()) ? reader.GetDecimal(0) : 0;
                }
                
                reader.Close();
                command.Parameters.Clear();

                if (fromAccBalance <= 0 || (fromAccBalance - amount) < 0)
                {
                    throw new Exception("From account balance not enough amount");
                }

                var fromAccId = GetAccountId(fromAcc, conString);

                if(fromAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Type] ,[Created_At] ,[Account_Id]) VALUES (@amount , 'C' , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", fromAccId);

                var result1 = command.ExecuteNonQuery();

                var toAccId = GetAccountId(toAcc, conString);

                if (toAccId == 0)
                {
                    throw new Exception("Account not found");
                }

                command.Parameters.Clear();

                command.CommandText = "INSERT INTO [dbo].[Transactions]([Amount] ,[Type] ,[Created_At] ,[Account_Id]) VALUES (@amount , 'D' , @createdAt, @accountId)";
                command.Parameters.AddWithValue("@amount", amount);
                command.Parameters.AddWithValue("@createdAt", DateTime.Now);
                command.Parameters.AddWithValue("@accountId", toAccId);

                var result2 = command.ExecuteNonQuery();

                if (result1 == 0 || result2 == 0)
                {
                    throw new Exception("Something went wrong");
                }

                sqlTransaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                sqlTransaction.Rollback();
            }
            finally
            {
                conn.Close();
            }
        }

        private static int GetAccountId(string number, string conString)
        {
            var accNumber = 0;
            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] FROM [dbo].[Accounts] WHERE [Number] = @number";

            var command = connection.CreateCommand();
            command.Parameters.AddWithValue("@number", number);
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                accNumber = reader.GetInt32(0);
            }
            connection.Close();
            reader.Close();

            return accNumber;
        }

        private static void CreateClient(string conString)
        {
            var client = new Client { FirstName = "test1", LastName = "test1", MiddleName = "", CreatedAt = DateTime.Now };

            var connection = new SqlConnection(conString);
            var query = "INSERT INTO [dbo].[Clients]([FirstName] ,[LastName] ,[MiddleName] ,[Created_At]) VALUES (@firstName ,@lastName ,@middleName ,@createdAt)";

            var command = connection.CreateCommand();
            command.CommandText = query;
            command.Parameters.AddWithValue("@firstName", client.FirstName);
            command.Parameters.AddWithValue("@lastName", client.LastName);
            command.Parameters.AddWithValue("@middleName", client.MiddleName);
            command.Parameters.AddWithValue("@createdAt", client.CreatedAt);

            connection.Open();

            var result = command.ExecuteNonQuery();



            if (result > 0)
            {
                Console.WriteLine("Added successfully.");
            }

            connection.Close();
        }

        private static void ListClient(string conString)
        {
            Client[] clients = new Client[0];

            var connection = new SqlConnection(conString);
            var query = "SELECT [Id] ,[FirstName] ,[LastName] ,[MiddleName] ,[Created_At] ,[Updated_At] FROM [dbo].[Clients]";

            var command = connection.CreateCommand();
            command.CommandText = query;

            connection.Open();

            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var client = new Client { };

                client.Id = int.Parse(reader["Id"].ToString());
                client.LastName = reader["LastName"].ToString();
                client.FirstName = reader["FirstName"].ToString();
                client.MiddleName = reader["MiddleName"].ToString();
                var x = reader["Created_At"]?.ToString();
                client.CreatedAt = !string.IsNullOrEmpty(reader["Created_At"]?.ToString()) ?  DateTime.Parse(reader["Created_At"].ToString()) : null;
                client.UpdatedAt = !string.IsNullOrEmpty(reader["Updated_At"]?.ToString()) ? DateTime.Parse(reader["Updated_At"].ToString()) : null;
                AddClient(ref clients, client);
            }
            connection.Close();
            foreach (var client in clients)
            {
                Console.WriteLine($"ID:{client.Id}, LastName:{client.LastName}, FirstName:{client.FirstName}, MiddleName:{client.MiddleName}, CreatedAt:{client.CreatedAt}, UpdatedAt:{client.UpdatedAt}");
            }
        }

        private static void AddClient(ref Client[] clients, Client client)
        {
            if (clients == null)
            {
                return;
            }

            Array.Resize(ref clients, clients.Length + 1);

            clients[clients.Length - 1] = client;
        }
    }

    public class Client
    {
        public int Id { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class Account
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public int ClientId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string Type { get; set; }
        public DateTime CreatedAt { get; set; }
        public int AccountId { get; set; }
    }
}
