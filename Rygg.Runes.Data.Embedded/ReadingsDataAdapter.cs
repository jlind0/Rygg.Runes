using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Rygg.Runes.Data.Core;
using System.Data;

namespace Rygg.Runes.Data.Embedded
{
    
    public interface IReadingsDataAdapter
    {
        IAsyncEnumerable<Reading> GetAll(int pageSize = 5, int page = 1, string? searchCondition = null, [EnumeratorCancellation] CancellationToken token = default);
        Task<long> Count(string? searchCondition = null, CancellationToken token = default);
        Task Add(Reading reading, CancellationToken token = default);
        Task Delete(long id, CancellationToken token = default);
        Task CreateDatabase(CancellationToken token = default);
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
        public async Task CreateDatabase(CancellationToken token = default)
        {
            using (SqliteConnection conn = new SqliteConnection(ConnectionString))
            {
                await conn.OpenAsync(token);

                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "CREATE TABLE IF NOT EXISTS Readings(Id INTEGER PRIMARY KEY AUTOINCREMENT, SpreadTypeId INTEGER, Question TEXT, Answer TEXT, Runes TEXT, AnnotatedImage BLOB);";
                    await command.ExecuteNonQueryAsync(token);
                }
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "CREATE VIRTUAL TABLE IF NOT EXISTS ReadingsFTS USING fts5(Question, Answer, Runes, content='Readings', content_rowid='Id');";
                    await command.ExecuteNonQueryAsync(token);
                }
                using (var command = conn.CreateCommand())
                {
                    command.CommandText = "CREATE TRIGGER IF NOT EXISTS Readings_AI AFTER INSERT ON Readings BEGIN INSERT INTO ReadingsFTS(rowid,Question, Answer, Runes) VALUES(new.Id, new.Question, new.Answer, new.Runes); END;";
                    await command.ExecuteNonQueryAsync(token);
                }
                using(var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA table_info('Readings')";
                    DataTable table = new DataTable();
                    using (var dr = await cmd.ExecuteReaderAsync(token))
                    {
                        table.Load(dr);
                    }
                    var cols = table.Rows.Cast<DataRow>().Select(r => r.Field<string>("name")).ToArray();
                    if(!cols.Any(c => c == "SpreadTypeId"))
                        using(var command = conn.CreateCommand())
                        {
                            command.CommandText = "ALTER TABLE Readings ADD COLUMN SpreadTypeId INTEGER";
                            await command.ExecuteNonQueryAsync(token);
                        }
                }
                await conn.CloseAsync();
            }
        }
        protected async Task<SqliteConnection> GetConnection(CancellationToken token = default)
        {
            
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
                    string def = "", varib = "";
                    if (reading.AnnotatedImage != null)
                    {
                        def += "AnnotatedImage,"; 
                        varib += "@AnnotatedImage,";
                    }
                    if (reading.SpreadType != null)
                    {
                        def += "SpreadTypeId,"; 
                        varib += "@SpreadTypeId,";
                    }
                    cmd.CommandText = $"INSERT INTO Readings(Answer,{def} Question, Runes) VALUES(@Answer, {varib} @Question, @Runes); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@Question", reading.Question);
                    cmd.Parameters.AddWithValue("@Answer", reading.Answer);
                    cmd.Parameters.AddWithValue("@Runes", string.Join(',', reading.Runes.Select(r => r.ToString())));
                    if(reading.AnnotatedImage != null)
                        cmd.Parameters.AddWithValue("@AnnotatedImage", reading.AnnotatedImage);
                    if (reading.SpreadType != null)
                        cmd.Parameters.AddWithValue("@SpreadTypeId", (int)reading.SpreadType);
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

        public async IAsyncEnumerable<Reading> GetAll(int pageSize = 5, int page = 1, string? searchCondition = null, [EnumeratorCancellation] CancellationToken token = default)
        {
            using(var conn = await GetConnection(token))
            {
                
                using(var cmd = conn.CreateCommand())
                {
                    if (!string.IsNullOrWhiteSpace(searchCondition))
                    {
                        cmd.CommandText = "SELECT rowid, Question, Answer, Runes FROM ReadingsFTS WHERE ReadingsFTS MATCH @SearchCondition LIMIT @PageSize OFFSET @Offset;";
                        cmd.Parameters.AddWithValue("@SearchCondition", searchCondition);

                    }
                    else
                        cmd.CommandText = "SELECT rowid, Question, Answer, Runes from ReadingsFTS LIMIT @PageSize OFFSET @Offset;";
                    int offset = (page - 1) * pageSize;
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);
                    cmd.Parameters.AddWithValue("@Offset", offset);
                    using (var dr = await cmd.ExecuteReaderAsync(token))
                    {
                        while (await dr.ReadAsync(token))
                        {
                            using (var c = conn.CreateCommand())
                            {
                                var id = (long)dr["rowid"];
                                c.CommandText = "Select AnnotatedImage,SpreadTypeId FROM Readings WHERE Id = @Id";
                                c.Parameters.AddWithValue("@Id", id);
                                byte[]? data;
                                int? spreadTypeId;
                                using(var drr = await c.ExecuteReaderAsync(token))
                                {
                                    await drr.ReadAsync(token);
                                    data = drr["AnnotatedImage"] as byte[];
                                    spreadTypeId = drr["SpreadTypeId"] != DBNull.Value ? Convert.ToInt32(drr["SpreadTypeId"]) : null;
                                }
                                yield return new Reading()
                                {
                                    Runes = ((string)dr["Runes"]).Split(',').Select(r => new PlacedRune(r)).ToArray(),
                                    Answer = (string)dr["Answer"],
                                    Question = (string)dr["Question"],
                                    AnnotatedImage = data ,
                                    SpreadType = spreadTypeId != null ? (SpreadTypes)spreadTypeId.Value : null,
                                    Id = id
                                };
                            }
                                
                        }
                    }
                }
            }
        }

        public async Task<long> Count(string? searchCondition = null, CancellationToken token = default)
        {
            using(var conn = await GetConnection(token))
            {
                using(var cmd = conn.CreateCommand())
                {
                    if (!string.IsNullOrWhiteSpace(searchCondition))
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM ReadingsFTS WHERE ReadingsFTS MATCH @SearchCondition";
                        cmd.Parameters.AddWithValue("@SearchCondition", searchCondition);
                    }
                    else
                    {
                        cmd.CommandText = "SELECT COUNT(*) FROM ReadingsFTS";
                    }
                    return (long)(await cmd.ExecuteScalarAsync(token) ?? throw new InvalidDataException());
                }
            }
        }
    }
}
