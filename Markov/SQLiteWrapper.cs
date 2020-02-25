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
                    Headline                TEXT NOT NULL,
                    `Sanitised Headline`  TEXT NOT NULL,
                    GUID                    TEXT PRIMARY KEY,
                    Date                    TEXT NOT NULL
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
                
                comm.CommandText = @"CREATE TABLE IF NOT EXISTS Markov2Probabilities
                (
                    Word1           NOT NULL,
                    Word2           NOT NULL,
                    Subsequent      NOT NULL,
                    Probability     FLOAT NOT NULL,
                    PRIMARY KEY (Word1, Word2, Subsequent)
                )";
                comm.ExecuteNonQuery();
            }
        }

        public static void InsertRecord(SQLiteConnection conn, string table, Dictionary<string, string> columnVals)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            var comm = conn.CreateCommand();
            string varString = string.Join(", ", columnVals.Select(x => $"${x.Key.Replace(" ", "")}"));
            string colString = string.Join(", ", columnVals.Select(x => $"`{x.Key}`"));
            comm.CommandText = $"INSERT OR REPLACE INTO {table} ({colString}) VALUES ({varString})";
            foreach (var pair in columnVals)
            {
                comm.Parameters.AddWithValue($"${pair.Key.Replace(" ", "")}", pair.Value);
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

        public static void UpdateRecord(SQLiteConnection conn, string table, Dictionary<string, string> keys, Dictionary<string, string> updateFields)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();

            var comm = conn.CreateCommand();
            string updateStr = string.Join(", ", updateFields.Select(x => $"`{x.Key}` = ${x.Key.Replace(" ", "")}"));
            string predStr = string.Join(" AND ", keys.Select(x => $"`{x.Key}` = ${x.Key.Replace(" ", "")}"));

            comm.CommandText = $"UPDATE {table} SET {updateStr} WHERE {predStr}";
            foreach(var k in keys)
                comm.Parameters.AddWithValue($"${k.Key.Replace(" ", "")}", k.Value);

            foreach(var u in updateFields)
                comm.Parameters.AddWithValue($"${u.Key.Replace(" ", "")}", u.Value);

            comm.ExecuteNonQuery();
        }

    }

}
