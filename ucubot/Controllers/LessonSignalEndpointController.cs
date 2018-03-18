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
        private readonly MySqlConnection msqlConnection;
        private string connectionString;
        

        public LessonSignalEndpointController(IConfiguration configuration)
        {
            _configuration = configuration;
            msqlConnection = new MySqlConnection(connectionString);
            connectionString = _configuration.GetConnectionString("BotDatabase");
        }

        [HttpGet]
        public IEnumerable<LessonSignalDto> ShowSignals()
        {
            msqlConnection.Open();
                MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT * FROM lesson_signal", msqlConnection);
                
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
            msqlConnection.Close();
            return lst;
        }
        
        [HttpGet("{id}")]
        public LessonSignalDto ShowSignal(long id)
        {
            msqlConnection.Open();
                var command = new MySqlCommand("SELECT * FROM lesson_signal WHERE id = @id", msqlConnection);
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
                msqlConnection.Close();
                return signalDto;
            
        }
        
        [HttpPost]
        public async Task<IActionResult> CreateSignal(SlackMessage message)
        {
            var userId = message.user_id;
            var signalType = message.text.ConvertSlackMessageToSignalType();

            msqlConnection.Open();
                var command = msqlConnection.CreateCommand();
                command.CommandText =
                    "INSERT INTO lesson_signal (user_id, signal_type) VALUES (@userId, @signalType);";
                command.Parameters.AddRange(new[]
                {
                	new MySqlParameter("userId", userId),
                    new MySqlParameter("signalType", signalType)
                });
                await command.ExecuteNonQueryAsync();
            msqlConnection.Close();
            return Accepted();
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveSignal(long id)
        {
            msqlConnection.Open();
                var command = msqlConnection.CreateCommand();
                command.CommandText =
                    "DELETE FROM lesson_signal WHERE ID = @id;";
            	command.Parameters.Add(new MySqlParameter("id", id));
                await command.ExecuteNonQueryAsync();
            msqlConnection.Close();
            return Accepted();
        }
    }
}
