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
            _msqlConnection.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT * FROM lesson_signal", _msqlConnection);
                
                DataSet dataset = new DataSet();
                
                adapter.Fill(dataset, "lesson_signal");
            
                var lst = new List<LessonSignalDto>();

                foreach (DataRow row in dataset.Tables[0].Rows)
                {
                    var signalDto = new LessonSignalDto
                    {
                        Id = (int) row["id"],
                        Timestamp = (DateTime) row["timestamp_"],
                        Type = (LessonSignalType)Convert.ToInt32(row["signal_type"]),
                        UserId = (string) row["user_id"]
                    };
                    lst.Add(signalDto);
                }
            _msqlConnection.Close();
            return lst;
        }
        
        [HttpGet("{id}")]
        public LessonSignalDto ShowSignal(long id)
        {
            _msqlConnection.Open();
                var command = new MySqlCommand("SELECT * FROM lesson_signal WHERE id = @id", _msqlConnection);
                command.Parameters.AddWithValue("id", id);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                
                DataSet dataset = new DataSet();
                
                adapter.Fill(dataset, "lesson_signal");
                if (dataset.Tables[0].Rows.Count < 1)
                    return null;
                
                var row = dataset.Tables[0].Rows[0];
                var signalDto = new LessonSignalDto
                {
                    Id = (int) row["id"],
                	Timestamp = (DateTime) row["timestamp_"],
                    Type = (LessonSignalType)Convert.ToInt32(row["signal_type"]),
                    UserId = (string) row["user_id"]
                };
                _msqlConnection.Close();
                return signalDto;
            
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateSignal(SlackMessage message)
        {
            var userId = message.user_id;
            var signalType = message.text.ConvertSlackMessageToSignalType();

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
