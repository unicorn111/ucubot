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
                var comm = "SELECT lesson_signal.Id as Id, lesson_signal.Timestemp as Timestamp, " +
                        "lesson_signal.signal_type as Type, student.user_id as UserId FROM lesson_signal" +
                        " JOIN student ON lesson_signal.student_id = student.id;";
                var lst = _msqlConnection.Query<LessonSignalDto>(comm).ToList();
                _msqlConnection.Close();
                return lst;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                var comm = "SELECT lesson_signal.Id as Id, lesson_signal.Timestemp as Timestamp, " +
                        "lesson_signal.signal_type as Type, student.user_id as UserId FROM lesson_signal" +
                        " JOIN student ON lesson_signal.student_id = student.id WHERE lesson_signal.Id = @id;";
                var signalDto = _msqlConnection.Query<LessonSignalDto>(comm).ToList();
                _msqlConnection.Close();
                return signalDto.First();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _msqlConnection.Close();
                return null;
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSignal(SlackMessage message)
        {
            try
            {
                _msqlConnection.Open();
                var userId = message.user_id;
                var signalType = message.text.ConvertSlackMessageToSignalType();
                var comm = "SELECT id as Id, first_name as FirstName, last_name as LastName, user_id as UserId from student where user_id=@uId";
                _msqlConnection.Query<Student>(comm, new {uId = userId}).AsList();
                if (!comm.Any())
                {
                    _msqlConnection.Close();
                    return BadRequest();
                }
                var comm2 = "INSERT INTO lesson_signal (student_id, signal_type) VALUES (@std, @st)";
                _msqlConnection.Execute(comm2, new {std = comm.First(), st = signalType});
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
            try
            {
                var com = "DELETE FROM lesson_signal WHERE id=@id;";
                _msqlConnection.Execute(com, new {Id = id});
                _msqlConnection.Close();
                return Accepted();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _msqlConnection.Close();
                return NotFound();
            }
        }
    }
}
