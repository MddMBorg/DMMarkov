using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace Markov
{
    public class SQLiteWrapper
    {
        public static void CheckDatabase()
        {
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();

                var comm = conn.CreateCommand();
                comm.CommandText = @"CREATE TABLE IF NOT EXISTS Headlines
                (
                    Headline    TEXT NOT NULL,
                    GUID        TEXT PRIMARY KEY,
                    Date        TEXT NOT NULL
                )";
                comm.ExecuteNonQuery();

                comm.CommandText = @"CREATE TABLE IF NOT EXISTS MarkovProbabilities
                (
                    Word            NOT NULL,
                    Subsequent      NOT NULL,
                    Probability     FLOAT NOT NULL,
                    PRIMARY KEY (Word, Subsequent)
                )";
                comm.ExecuteNonQuery();
            }
        }

        public static void InsertRecord(SQLiteConnection conn, string table, Dictionary<string, string> columnVals)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            var comm = conn.CreateCommand();
            string varString = string.Join(", ", columnVals.Select(x => $"${x.Key}"));
            string colString = string.Join(", ", columnVals.Select(x => $"{x.Key}"));
            comm.CommandText = $"INSERT INTO {table} ({colString}) VALUES ({varString})";
            foreach (var pair in columnVals)
            {
                comm.Parameters.AddWithValue($"${pair.Key}", pair.Value);
            }
            try
            {
                comm.ExecuteNonQuery();
            }
            catch { }
        }

        public static void InsertRecords(SQLiteConnection conn, string table, List<Dictionary<string, string>> columnVals)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            foreach (var item in columnVals)
                InsertRecord(conn, table, item);
        }

        public static void InsertRecords(string table, List<Dictionary<string, string>> columnVals)
        {
            using (var conn = new SQLiteConnection("Data Source=DMHeadlines.db"))
            {
                conn.Open();
                InsertRecords(conn, table, columnVals);
            }
        }

    }

}
