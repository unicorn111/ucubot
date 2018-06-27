using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using ucubot.Model;
using Dapper;


namespace ucubot.Controllers
{
    [Route("api/[controller]")]
    public class LessonSignalEndpointController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlConnection _msqlConnection;
        private readonly string _connectionString;
        

        public LessonSignalEndpointController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("BotDatabase");
            _msqlConnection = new MySqlConnection(_connectionString);
        }

        [HttpGet]
        public IEnumerable<LessonSignalDto> ShowSignals()
        {
            try
            {
                _msqlConnection.Open();
                var j = "SELECT lesson_signal.Id as Id, lesson_signal.Timestemp as Timestamp, " +
                        "lesson_signal.signal_type as Type, student.user_id as UserId FROM lesson_signal" +
                        " JOIN student ON lesson_signal.student_id = student.id;";
                var lst = _msqlConnection.Query<LessonSignalDto>(j).ToList();
                _msqlConnection.Close();
                return lst;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _msqlConnection.Close();
                return null;
            }
        }
        
        [HttpGet("{id}")]
        public LessonSignalDto ShowSignal(long id)
        {
            try
            {
                _msqlConnection.Open();
                var j = "SELECT lesson_signal.Id as Id, lesson_signal.Timestemp as Timestamp, " +
                        "lesson_signal.signal_type as Type, student.user_id as UserId FROM lesson_signal" +
                        " JOIN student ON lesson_signal.student_id = student.id WHERE lesson_signal.Id = @id;";
                var signalDto = _msqlConnection.Query<LessonSignalDto>(j);
                return signalDto.First();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _msqlConnection.Close();
                return null;
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateSignal(SlackMessage message)
        {
            try
            {
                var userId = message.user_id;
                var signalType = message.text.ConvertSlackMessageToSignalType();
                var checkUser = new MySqlCommand("SELECT * FROM student WHERE id = @userId", _msqlConnection);
                MySqlDataAdapter adapter = new MySqlDataAdapter(checkUser);
                DataSet dataset = new DataSet();
                adapter.Fill(dataset, "lesson_signal");
                if (dataset.Tables[0].Rows.Count < 1)
                {
                    _msqlConnection.Close();
                    return BadRequest();
                }

                checkUser.Parameters.AddWithValue("id", userId);

                _msqlConnection.Open();
                var command = _msqlConnection.CreateCommand();
                command.CommandText =
                    "INSERT INTO lesson_signal (user_id, signal_type) VALUES (@userId, @signalType);";
                command.Parameters.AddRange(new[]
                {
                    new MySqlParameter("userId", userId),
                    new MySqlParameter("signalType", signalType)
                });
                await command.ExecuteNonQueryAsync();
                _msqlConnection.Close();
                return Accepted();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _msqlConnection.Close();
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveSignal(long id)
        {
            _msqlConnection.Open();
                var command = _msqlConnection.CreateCommand();
                command.CommandText =
                    "DELETE FROM lesson_signal WHERE ID = @id;";
            	command.Parameters.Add(new MySqlParameter("id", id));
                await command.ExecuteNonQueryAsync();
            _msqlConnection.Close();
            return Accepted();
        }
    }
}
