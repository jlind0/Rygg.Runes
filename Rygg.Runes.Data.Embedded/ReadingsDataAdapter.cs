using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rygg.Runes.Data.Embedded
{
    public class Reading
    {
        public long Id { get; set; }
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
        public string[] Runes { get; set; } = null!;
        public byte[] AnnotatedImage { get; set; } = null!;
    }
    public interface IReadingsDataAdapter
    {
        IAsyncEnumerable<Reading> GetAll(string? searchCondition = null, [EnumeratorCancellation] CancellationToken token = default);
        Task Add(Reading reading, CancellationToken token = default);
        Task Delete(long id, CancellationToken token = default);
    }
    public class ReadingsDataAdapter : IReadingsDataAdapter
    {
        protected string DatabaseName { get; } = "readings.db";
        protected string ConnectionString { get; }
        protected string RootDirectory { get; }
        public ReadingsDataAdapter(string path)
        {
            ConnectionString = $"Data Source = {Path.Combine(path,DatabaseName)}";
            RootDirectory = path;
        }
        protected async Task<SqliteConnection> GetConnection(CancellationToken token = default)
        {
            //File.Delete(Path.Join(RootDirectory, DatabaseName));
            if(!File.Exists(Path.Combine(RootDirectory, DatabaseName))) 
            {
                using(SqliteConnection conn = new SqliteConnection(ConnectionString))
                {
                    await conn.OpenAsync(token);
                   
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "CREATE TABLE Readings(Id INTEGER PRIMARY KEY AUTOINCREMENT, Question TEXT, Answer TEXT, Runes TEXT, AnnotatedImage BLOB);";
                        await command.ExecuteNonQueryAsync(token);
                    }
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "CREATE VIRTUAL TABLE ReadingsFTS USING fts5(Question, Answer, Runes, content='Readings', content_rowid='Id');";
                        await command.ExecuteNonQueryAsync(token);
                    }
                    using (var command = conn.CreateCommand())
                    {
                        command.CommandText = "CREATE TRIGGER Readings_AI AFTER INSERT ON Readings BEGIN INSERT INTO ReadingsFTS(rowid,Question, Answer, Runes) VALUES(new.Id, new.Question, new.Answer, new.Runes); END;";
                        await command.ExecuteNonQueryAsync(token);
                    }
                }
            }
            var con = new SqliteConnection(ConnectionString);
            await con.OpenAsync(token);
            return con;
        }
        public async Task Add(Reading reading, CancellationToken token = default)
        {
            using(var conn = await GetConnection(token))
            {
                using(var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "INSERT INTO Readings(Answer, Question, Runes, AnnotatedImage) VALUES(@Answer, @Question, @Runes, @AnnotatedImage); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@Question", reading.Question);
                    cmd.Parameters.AddWithValue("@Answer", reading.Answer);
                    cmd.Parameters.AddWithValue("@Runes", string.Join(',', reading.Runes));
                    cmd.Parameters.AddWithValue("@AnnotatedImage", reading.AnnotatedImage);
                    reading.Id = (long)(await cmd.ExecuteScalarAsync(token) ?? throw new InvalidOperationException());
                }
            }
        }

        public async Task Delete(long id, CancellationToken token = default)
        {
            using (var conn = await GetConnection(token))
            {
                using(var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Readings WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);
                    await cmd.ExecuteNonQueryAsync(token);
                }
            }
        }

        public async IAsyncEnumerable<Reading> GetAll(string? searchCondition = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            using(var conn = await GetConnection(token))
            {
                
                using(var cmd = conn.CreateCommand())
                {
                    if (!string.IsNullOrEmpty(searchCondition))
                    {
                        cmd.CommandText = "SELECT rowid, Question, Answer, Runes FROM ReadingsFTS WHERE ReadingsFTS MATCH @SearchCondition";
                        cmd.Parameters.AddWithValue("@SearchCondition", searchCondition);

                    }
                    else
                        cmd.CommandText = "SELECT rowid, Question, Answer, Runes from ReadingsFTS;";
                    using (var dr = await cmd.ExecuteReaderAsync(token))
                    {
                        while (await dr.ReadAsync(token))
                        {
                            using (var c = conn.CreateCommand())
                            {
                                var id = (long)dr["rowid"];
                                c.CommandText = "Select AnnotatedImage FROM Readings WHERE Id = @Id";
                                c.Parameters.AddWithValue("@Id", id);
                                var data = (byte[])(await c.ExecuteScalarAsync(token) ?? throw new InvalidDataException());
                                yield return new Reading()
                                {
                                    Runes = ((string)dr["Runes"]).Split(','),
                                    Answer = (string)dr["Answer"],
                                    Question = (string)dr["Question"],
                                    AnnotatedImage = data,
                                    Id = id
                                };
                            }
                                
                        }
                    }
                }
            }
        }
    }
}
