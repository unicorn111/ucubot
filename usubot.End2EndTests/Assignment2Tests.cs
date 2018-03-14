using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using ucubot.Model;

namespace usubot.End2EndTests
{
    [TestFixture]
    [Category("Assignment2")]
    public class Assignment2Tests
    {
        private const string CONNECTION_STRING_NODB = "Server=db;Uid=root;Pwd=1qaz2wsx";
        private const string CONNECTION_STRING = "Server=db;Database=ucubot;Uid=root;Pwd=1qaz2wsx";

        private HttpClient _client;

        [SetUp]
        public void Init()
        {
            _client = new HttpClient {BaseAddress = new Uri("http://app:80")};
        }
        
        [Test, Order(-10)]
        public void Preparation()
        {
            // HACK: waits few seconds to give a time for mysql container to start
            Thread.Sleep(3000);
            Assert.That(true);
            
            // clean data
            using (var conn = new MySqlConnection(CONNECTION_STRING_NODB))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = "DROP DATABASE IF EXISTS ucubot;";
                command.ExecuteNonQuery();
                
                var users = ExecuteDataTable("SELECT User, Host FROM mysql.user;", conn);
                foreach (DataRow row in users.Rows)
                {
                    var name = (string) row["User"];
                    if (name == "root") continue;
                    var host = (string) row["Host"];

                    var cmd = $"DROP USER '{name}'@'{host}';";
                    var command2 = conn.CreateCommand();
                    command2.CommandText = cmd;
                    command2.ExecuteNonQuery();
                }
                
                var users2 = MapDataTableToStringCollection(ExecuteDataTable("SELECT User, Host FROM mysql.user;", conn)).ToArray();
                users2.Length.Should().Be(2);
            }
        }
        
        [Test, Order(1)]
        public void TestDatabaseWasCreated()
        {
            // create database test
            var dbScript = ReadMysqlScript("db");
            using (var conn = new MySqlConnection(CONNECTION_STRING_NODB))
            {
                conn.Open();
                
                var users1 = MapDataTableToStringCollection(ExecuteDataTable("SELECT User FROM mysql.user;", conn)).ToArray();
                users1.Length.Should().BeGreaterThan(1); // we don't know actual name of the user...
                
                var command = conn.CreateCommand();
                command.CommandText = dbScript;
                command.ExecuteNonQuery();

                var databases = MapDataTableToStringCollection(ExecuteDataTable("SHOW DATABASES;", conn)).ToArray();
                databases.Should().Contain("ucubot");

                var users = MapDataTableToStringCollection(ExecuteDataTable("SELECT User FROM mysql.user;", conn)).ToArray();
                users.Length.Should().Be(3); // we don't know actual name of the user, and there is only root exists after cleanup
            }
        }

        [Test, Order(2)]
        public void Test_Student_TableWasCreated()
        {
            // create database test
            var dbScript = ReadMysqlScript("student");
            using (var conn = new MySqlConnection(CONNECTION_STRING))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = dbScript;
                command.ExecuteNonQuery();
                
                var tables = MapDataTableToStringCollection(ExecuteDataTable("SHOW TABLES;", conn)).ToArray();
                tables.Should().Contain("student");
            }
        }
        
        [Test, Order(3)]
        public void Test_LessonSignal_TableWasCreated()
        {
            // create database test
            var dbScript = ReadMysqlScript("lesson-signal");
            var dbScript2 = ReadMysqlScript("lesson-signal2");
            using (var conn = new MySqlConnection(CONNECTION_STRING))
            {
                conn.Open();
                var command = conn.CreateCommand();
                command.CommandText = dbScript;
                command.ExecuteNonQuery();
                
                var tables = MapDataTableToStringCollection(ExecuteDataTable("SHOW TABLES;", conn)).ToArray();
                tables.Should().Contain("lesson_signal");
                
                var command2 = conn.CreateCommand();
                command2.CommandText = dbScript2;
                command2.ExecuteNonQuery();
                
                var constranints = MapDataTableToStringCollection(ExecuteDataTable(@"SELECT REFERENCED_TABLE_NAME 
FROM information_schema.REFERENTIAL_CONSTRAINTS WHERE CONSTRAINT_SCHEMA = 'ucubot'
 AND TABLE_NAME = 'lesson_signal'", conn)).ToArray();
                constranints.Should().Contain("student");
            }
        }

        [Test, Order(4)]
        public async Task Test_Student_GetCreateGetUpdateGetDeleteGet()
        {
            // check is empty
            var getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            var values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(0);
            
            // create
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", "U111"),
                new KeyValuePair<string, string>("FirstName", "vasya"),
                new KeyValuePair<string, string>("LastName", "popov")
            });
            var createResponse = await _client.PostAsync("/api/StudentEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(1);
            values[0].UserId.Should().Be("U111");
            values[0].FirstName.Should().Be("vasya");
            values[0].LastName.Should().Be("popov");
            
            // update
            content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Id", values[0].Id.ToString()),
                new KeyValuePair<string, string>("UserId", "U111"),
                new KeyValuePair<string, string>("FirstName", "vasya"),
                new KeyValuePair<string, string>("LastName", "petrov")
            });
            var updateResponse = await _client.PutAsync("/api/StudentEndpoint", content);
            updateResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(1);
            values[0].UserId.Should().Be("U111");
            values[0].FirstName.Should().Be("vasya");
            values[0].LastName.Should().Be("petrov");
            
            // check by id
            getResponse = await _client.GetStringAsync($"/api/StudentEndpoint/{values[0].Id}");
            var value = ParseJson<Student>(getResponse);
            value.UserId.Should().Be("U111");
            value.FirstName.Should().Be("vasya");
            value.LastName.Should().Be("petrov");
            
            // delete
            var deleteResponse = await _client.DeleteAsync($"/api/StudentEndpoint/{values[0].Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(0);
        }
        
        [Test, Order(5)]
        public async Task Test_Student_SqlInjectionFail()
        {
            // check is empty
            var getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            var values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(0);
            
            // create another with attack
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", "U111', 0); DELETE FROM lesson_signal; #"),
                new KeyValuePair<string, string>("FirstName", "1"),
                new KeyValuePair<string, string>("LastName", "2")
            });
            var createResponse = await _client.PostAsync("/api/StudentEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(1);
        }
        
        [Test, Order(6)]
        public async Task Test_Student_NonExistRecordReturns404()
        {
            // get previous values
            var getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            var values = ParseJson<Student[]>(getResponse);
            var newId = values.Select(v => v.Id).Max() + 1;
            
            // check
            var response = await _client.GetAsync($"/api/StudentEndpoint/{newId}");
            Assert.IsTrue(new[]{HttpStatusCode.NotFound, HttpStatusCode.OK, HttpStatusCode.NoContent }.Contains(response.StatusCode),
                $"Non exists record response should not be {response.StatusCode}");
        }
        
        [Test, Order(7)]
        public async Task Test_Student_UserIdUnique()
        {
            // check is empty
            var getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            var values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(1);
            
            // create new
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", "U111"),
                new KeyValuePair<string, string>("FirstName", "1"),
                new KeyValuePair<string, string>("LastName", "2")
            });
            var createResponse = await _client.PostAsync("/api/StudentEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // create second with the same user_id
            content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", "U111"),
                new KeyValuePair<string, string>("FirstName", "1"),
                new KeyValuePair<string, string>("LastName", "2")
            });
            createResponse = await _client.PostAsync("/api/StudentEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
            
            // check
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            values = ParseJson<Student[]>(getResponse);
            values.Length.Should().Be(2);
        }
        
        [Test, Order(8)]
        public async Task Test_LessonSignal_GetCreateGetDeleteGet()
        {
            // check is empty
            var getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            var values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(0);
            
            // create with user_id already exists
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user_id", "U111"),
                new KeyValuePair<string, string>("text", "simple")
            });
            var createResponse = await _client.PostAsync("/api/LessonSignalEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(1);
            values[0].UserId.Should().Be("U111");
            values[0].Type.Should().Be(LessonSignalType.BoringSimple);
            
            // delete
            var deleteResponse = await _client.DeleteAsync($"/api/LessonSignalEndpoint/{values[0].Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check
            getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(0);
        }
        
        [Test, Order(9)]
        public async Task Test_LessonSignal_NonExistRecordReturns404()
        {
            // get previous values
            var getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            var values = ParseJson<LessonSignalDto[]>(getResponse);
            var newId = values.Length > 0 ? values.Select(v => v.Id).Max() + 1 : 1;
            
            // check
            var response = await _client.GetAsync($"/api/LessonSignalEndpoint/{newId}");
            Assert.IsTrue(new[]{HttpStatusCode.NotFound, HttpStatusCode.OK, HttpStatusCode.NoContent }.Contains(response.StatusCode),
                $"Non exists record response should not be {response.StatusCode}");
        }
        
        [Test, Order(10)]
        public async Task Test_LessonSignal_CanNotCreateForNonExistsStudent()
        {
            // create with user_id non exists
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user_id", "U112"),
                new KeyValuePair<string, string>("text", "simple")
            });
            var createResponse = await _client.PostAsync("/api/LessonSignalEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            
            // check
            var getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            var values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(0);
        }
        
        [Test, Order(11)]
        public async Task Test_Student_LessonSignal_CanNotDeleteWithChildRecords()
        {
            // check students non empty
            var getResponseStudents = await _client.GetStringAsync("/api/StudentEndpoint");
            var valuesStudents = ParseJson<Student[]>(getResponseStudents);
            valuesStudents.Length.Should().Be(2);
            
            // check lesson signal is empty
            var getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            var values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(0);
            
            // create lesson signal with user_id already exists
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("user_id", valuesStudents[0].UserId),
                new KeyValuePair<string, string>("text", "simple")
            });
            var createResponse = await _client.PostAsync("/api/LessonSignalEndpoint", content);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // check lesson signal is non empty
            getResponse = await _client.GetStringAsync("/api/LessonSignalEndpoint");
            values = ParseJson<LessonSignalDto[]>(getResponse);
            values.Length.Should().Be(1);
            
            // try delete student
            var deleteResponse1 = await _client.DeleteAsync($"/api/StudentEndpoint/{valuesStudents[0].Id}");
            deleteResponse1.StatusCode.Should().Be(HttpStatusCode.Conflict);
            
            // check ls
            getResponse = await _client.GetStringAsync("/api/StudentEndpoint");
            valuesStudents = ParseJson<Student[]>(getResponse);
            valuesStudents.Length.Should().Be(2);
            
            // delete lesson signal
            var deleteResponse = await _client.DeleteAsync($"/api/LessonSignalEndpoint/{values[0].Id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
            
            // delete student
            var deleteResponse2 = await _client.DeleteAsync($"/api/StudentEndpoint/{valuesStudents[0].Id}");
            deleteResponse2.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        [TearDown]
        public void Done()
        {
            _client.Dispose();
        }

        private string ReadMysqlScript(string scriptName)
        {
            using (var reader = new StreamReader(File.OpenRead($"/app/ucubot/Scripts/{scriptName}.sql")))
            {
                return reader.ReadToEnd();
            }
        }

        private DataTable ExecuteDataTable(string sqlCommand, MySqlConnection conn)
        {
            var adapter = new MySqlDataAdapter(sqlCommand, conn);

            var dataset = new DataSet();

            adapter.Fill(dataset);

            return dataset.Tables[0];
        }

        private IEnumerable<string> MapDataTableToStringCollection(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                yield return row[0].ToString();
            }
        }

        private T ParseJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new SnakeCaseNamingStrategy()
                }
            });
        }
    }
}
