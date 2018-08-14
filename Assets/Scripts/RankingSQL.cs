using System.Data;
using System.Text;
using Mono.Data.Sqlite;
using UnityEngine;

public class RankingSQL
{
    private const string TABLE = "ranking";
    private const string TABLE_IND = "ranking_ind";

    private const string RANKING_ORDER = "DESC"; // DESC or ASC

#if UNITY_EDITOR
    private const string DB_FILE_NAME = "/debugDatabase.db";
    private string FILE_PATH = Application.streamingAssetsPath;
#else
        private const string DB_FILE_NAME = "/rankingDatabase.db";
        private string FILE_PATH = Application.persistentDataPath;
#endif

    private string dbPath;

    private string currentInsertedID = "";

    public RankingSQL()
    {
        LoadTable();
    }

    public void LoadTable()
    {
        string actualDbPath = FILE_PATH + DB_FILE_NAME;
        dbPath = "URI=file:" + actualDbPath;

        if (!System.IO.File.Exists(actualDbPath))
        {
            // first time
            CreateSchema();
        }
        Debug.Log("Total records:" + GetTotalRows());
    }

    public void CreateSchema()
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS '" + TABLE + "' ( " +
                                  "  'id' INTEGER PRIMARY KEY, " +
                                  "  'name' TEXT NOT NULL, " +
                                  "  'score' INTEGER NOT NULL," +
                                  "  'created_at' TIMESTAMP DEFAULT (datetime(CURRENT_TIMESTAMP,'localtime'))" +
                                  ");";
                // you can check result like "var result = cmd.ExecuteNonQuery();" this way.
                cmd.ExecuteNonQuery();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "CREATE TABLE IF NOT EXISTS '" + TABLE_IND + "' ( " +
                                  "  'ranking_id' INTEGER NOT NULL" +
                                  ");";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    // return: Get your current score
    public string InsertScore(string name, int score)
    {
        string res = "";
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "INSERT INTO " + TABLE + " (name,score) " +
                                  "VALUES (@NAME,@SCORE);";

                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "NAME",
                    Value = name
                });

                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "SCORE",
                    Value = score
                });

                cmd.ExecuteNonQuery();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT last_insert_rowid()";
                var LastRowID = cmd.ExecuteScalar();

                currentInsertedID = LastRowID.ToString();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM " + TABLE_IND + "; INSERT INTO " + TABLE_IND + " (ranking_id) SELECT id FROM " + TABLE + " ORDER BY score "+ RANKING_ORDER + ";";
                cmd.ExecuteNonQuery();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT *, ROWID FROM " + TABLE_IND + " where ranking_id = @ID";

                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "ID",
                    Value = LastRowID
                });

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var rowid = reader.GetInt32(1);
                    res = rowid.ToString();
                }

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
                return res;
            }
        }
    }

    // This method is debug purpose. If you want more than 500 records, Process will take a time.
    public void DebugInsertScores(int n)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;

                string query = "";

                for (int i = 0; i < n; i++)
                {
                    int val = (int)Random.Range(0, 1000000f);
                    string name = "testname" + val.ToString();
                    query += "INSERT INTO " + TABLE + " (name,score) " + "VALUES ("+ name + "," + val.ToString() + ");";
                }

                cmd.CommandText = query;

                cmd.ExecuteNonQuery();

                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM " + TABLE_IND + "; INSERT INTO " + TABLE_IND + " (ranking_id) SELECT id FROM " + TABLE + " ORDER BY score " + RANKING_ORDER + ";";
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    public void GetHighScores(int limit)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * FROM " + TABLE + " ORDER BY score " + RANKING_ORDER + " LIMIT @COUNT;";

                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "COUNT",
                    Value = limit
                });

                var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var name = reader.GetString(1);
                    var score = reader.GetInt32(2);
                    var create_at = reader.GetDateTime(3);
                    var text = string.Format("[#{0}],{1},{2} {3}", id, name, score, create_at);
                    Debug.Log(text);
                }

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    public int GetTotalRows()
    {
        int count = 0;
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT COUNT(*) FROM " + TABLE + ";";
                var reader = cmd.ExecuteScalar();
                count = int.Parse(reader.ToString());

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }

        return count;
    }

    public void UpdateScore(int id, int score)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "update " + TABLE + " set score = @SCORE where id = @ID";
                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "ID",
                    Value = id
                });
                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "SCORE",
                    Value = score
                });
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    public void DeleteScore(int id)
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "delete from " + TABLE + " where id = @ID";
                cmd.Parameters.Add(new SqliteParameter
                {
                    ParameterName = "ID",
                    Value = id
                });
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    public void DeleteAllScores()
    {
        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "DELETE FROM " + TABLE + ";" + "DELETE FROM " + TABLE_IND + ";";
                cmd.Connection = conn;
                cmd.ExecuteNonQuery();

                cmd.Dispose();
                cmd.Connection = null;

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    // output sql table to csv
    public void OutCSV(string csvPath)
    {
        System.Text.Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
        System.IO.StreamWriter sr = new System.IO.StreamWriter(csvPath, false, enc);

        using (var conn = new SqliteConnection(dbPath))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = "SELECT * FROM " + TABLE + " ORDER BY score " + RANKING_ORDER + ";";

                var reader = cmd.ExecuteReader();
                var builder = new StringBuilder();
                while (reader.Read())
                {
                    var id = reader.GetInt32(0);
                    var name = reader.GetString(1);
                    var score = reader.GetInt32(2);
                    var create_at = reader.GetDateTime(3);

                    string idField = id.ToString();
                    string nameField = name.ToString();
                    string scoreField = score.ToString();
                    string create_atField = create_at.ToString();

                    idField = EncloseDoubleQuotesIfNeed(idField);
                    builder.Append(idField);
                    builder.Append(',');

                    nameField = EncloseDoubleQuotesIfNeed(nameField);
                    builder.Append(nameField);
                    builder.Append(',');

                    scoreField = EncloseDoubleQuotesIfNeed(scoreField);
                    builder.Append(scoreField);
                    builder.Append(',');

                    create_atField = EncloseDoubleQuotesIfNeed(create_atField);
                    builder.Append(create_atField);

                    builder.Append("\r\n");
                }
                sr.Write(builder.ToString());

                cmd.Dispose();
                cmd.Connection = null;

                sr.Close();

                conn.Dispose();
                conn.Close();

                ForceGC();
            }
        }
    }

    private void ForceGC()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();

        Mono.Data.Sqlite.SqliteConnection.ClearAllPools();
    }

    private string EncloseDoubleQuotesIfNeed(string field)
    {
        if (NeedEncloseDoubleQuotes(field))
        {
            return EncloseDoubleQuotes(field);
        }
        return field;
    }

    private string EncloseDoubleQuotes(string field)
    {
        if (field.IndexOf('"') > -1)
        {
            field = field.Replace("\"", "\"\"");
        }
        return "\"" + field + "\"";
    }

    private bool NeedEncloseDoubleQuotes(string field)
    {
        return field.IndexOf('"') > -1 ||
            field.IndexOf(',') > -1 ||
            field.IndexOf('\r') > -1 ||
            field.IndexOf('\n') > -1 ||
            field.StartsWith(" ") ||
            field.StartsWith("\t") ||
            field.EndsWith(" ") ||
            field.EndsWith("\t");
    }

    public string GetCurrentInsertedID()
    {
        return currentInsertedID;
    }

    public string GetFilePath()
    {
        return FILE_PATH;
    }
}