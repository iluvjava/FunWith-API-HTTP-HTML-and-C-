﻿using MyLibrary.MyLittleXML;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyLibrary.SQLClient
{
    /// <summary>
    /// This class contains stuff needed for making a connections,
    /// keeping the connection, and running the query.
    /// - Singleton, only run one connection from this class.
    ///     just to keep things simple.
    /// </summary>
    public class MyLittleSqlClient
    {
        /// <summary>
        /// Bool if you want to see connection log and stuff like that.
        /// </summary>
        public static bool Log = false;

        /// <summary>
        /// Singleton
        /// </summary>
        public static MyLittleSqlClient TheInstance;

        public MySqlConnection DBConn;

        /// <summary>
        /// Constructor internal to keep singleton.
        /// </summary>
        protected MyLittleSqlClient()
        {
        }

        /// <summary>
        /// Get the default config for the testing codes.
        ///     - If the config files DNE, it will create a new XML file with 
        ///     default settings in it. 
        ///     - If the config files exists, it will load it; which is used 
        ///     by the dataconnection. 
        /// </summary>
        /// <returns></returns>
        public static SQLConnConfig GetDefultConnectionConfig()
        {
            var defultconfig = SQLConnConfig.GetConfig();
            var config = 
                new ObjectXMLCache<SQLConnConfig>
                (defultconfig, "","dbconn.config");
            if (config.Deserialize())
            {
                return config.ObjectToStore; 
            }
            Print("\n Config DNE, creating new config. \n");
            config.Serialize();
            return defultconfig; 
        }

        /// <summary>
        /// Return an instance of the connection.
        /// This method won't renew the data object that is 
        /// in the static field. 
        /// </summary>
        /// <returns>
        /// Null is returned if there is something wrong. 
        /// </returns>
        public static MyLittleSqlClient GetInstance()
        {
            try
            {
                if (TheInstance != null) return MyLittleSqlClient.TheInstance;
                var config = GetDefultConnectionConfig();
                var builder = new MySqlConnectionStringBuilder();
                builder.Server = config.Server;
                builder.UserID = config.UserID;
                builder.Database = config.DataBase;
                builder.Password = config.PassWord;
                var connectionstring = builder.ConnectionString;
                var dbconn = new MySqlConnection(connectionstring);
                dbconn.Open();
                var res = new MyLittleSqlClient();
                res.DBConn = dbconn;
                return res;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        /// <summary>
        /// Get an instance of the db connection.
        /// </summary>
        /// <returns></returns>
        public async static Task<MyLittleSqlClient> GetInstanceAsync()
        {
            var res = await Task<MyLittleSqlClient>.Run
                (
                    () =>
                    {
                        return GetInstance();
                    }
                );
            return res;
        }

        /// <summary>
        /// Give a string that only update the data.
        /// Method returns the data read from the db.
        /// </summary>
        /// <param name="qry"></param>
        /// <returns>
        /// Null will be returned if there is anykind of error.
        /// </returns>
        public MySqlDataReader QueryExtractData
            (string qry, IDictionary<string, string> parameters = null)
        {
            try
            {

                var command = new MySqlCommand(qry, this.DBConn);
                if (parameters != null)
                {
                    foreach (var stuff in parameters)
                    {
                        command.Parameters.AddWithValue(stuff.Key, stuff.Value);
                    }
                }
                return command.ExecuteReader();
            }
            catch (Exception e)
            {
                Print(e);
                return null;
            }
        }

        public async Task<MySqlDataReader> QueryExtractDataAsync
            (string qry, IDictionary<string, string> parameters)
        {
            var t = await Task<MySqlDataReader>.Run
                (
                    () =>
                    {
                        return QueryExtractData(qry, parameters);
                    }
                );
            return t;
        }

        /// <summary>
        /// The method provided for this class to log stuff.
        /// </summary>
        /// <param name="stuff"></param>
        internal static void Print(object stuff)
        {
            if (!Log) return;
            Console.WriteLine(stuff == null ? "null" : stuff.ToString());
        }

        internal static void Print()
        {
            if (!Log) return; 
            Console.WriteLine();
        }
    }

    /// <summary>
    /// This class contains method that build the connection to the databse.
    ///
    /// </summary>
    public class SQLConnectionDemo
    {
        /// <summary>
        /// This is an example of connecting to a mysql database.
        /// </summary>
        public static void Demo()
        {
            try
            {
                var Server = "localhost";
                var Database = "myschema";
                var Uid = "root";
                var Password = "password";

                // Initialize that databse, using connection string builder.

                var builder = new MySqlConnectionStringBuilder();
                builder.Server = Server;
                builder.UserID = Uid;
                builder.Password = Password;
                builder.Database = Database;
                Console.WriteLine("This is the connection string: " +
                    builder.ToString());

                var dbconn = new MySqlConnection(builder.ToString());
                Console.WriteLine(dbconn);
                Console.WriteLine
                    ("Connect and select everything from the database.");

                var query = "SELECT * FROM mytable";
                var mysqlcommand = new MySqlCommand(query, dbconn);
                Console.WriteLine("Opening connection to the database. ");
                dbconn.Open(); //Do this stage as late as possible.
                MySqlDataReader reader = mysqlcommand.ExecuteReader();

                while (reader.Read())
                {
                    int id = (int)reader["id"];
                    string col1 = reader["col1"].ToString();
                    string col2 = reader["col2"].ToString();
                    string col3 = reader["col3"].ToString();
                    Console.WriteLine($"{id}, {col1}, {col2}, {col3}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }


    [Serializable]
    public struct SQLConnConfig
    {
        public string Server;
        public string DataBase;
        public string UserID;
        public string PassWord;

        /// <summary>
        /// Get the default config for connecting to database. 
        /// </summary>
        /// <returns></returns>
        public static SQLConnConfig GetConfig()
        {
            var res = new SQLConnConfig();
            res.Server = "localhost";
            res.UserID = "root";
            res.DataBase = "mychema";
            res.PassWord = "password";
            return res; 
        }
        override
        public string ToString()
        {
            var nl = Environment.NewLine;
            var res = "";
            res += "=====SQL Config=====" + nl; 
            res += this.Server + nl;
            res += this.UserID + nl;
            res += this.DataBase + nl;
            res += this.PassWord + nl;
            return res; 
        }

    }



    /// <summary>
    /// This is a class that make use the MyLittleSQLClient 
    /// to provide a model for the database connection. 
    /// This class models a table. 
    /// <remarks>
    /// 
    /// </remarks>
    /// </summary>
    public class MyLittleEntityExample
    {
        public MyLittleSqlClient DBConn = MyLittleSqlClient.GetInstance(); 





    }

}