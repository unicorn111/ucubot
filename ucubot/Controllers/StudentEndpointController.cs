using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Net;
using ucubot.Model;
using Dapper;


namespace ucubot.Controllers
{
    [Route("api/[controller]")]
    public class StudentEndpointController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly MySqlConnection _msqlConnection;
        private readonly string _connectionString;
        
        public StudentEndpointController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("BotDatabase");
            _msqlConnection = new MySqlConnection(_connectionString);
        }
        
        [HttpGet]
        public IEnumerable<LessonSignalDto> ShowStudents()
        {
            try
            {
                _msqlConnection.Open();
                var j = "SELECT student.Id as Id, student.firstname as FirstName, " +
                        "student.lastname as LastName, student.user_id as UserId FROM student;";
                var lst = _msqlConnection.Query<LessonSignalDto>(j);
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
        public LessonSignalDto ShowStudent(long id)
        {
            try
            {
                _msqlConnection.Open();
                var j = "SELECT student.Id as Id, student.firstname as FirstName, " +
                        "student.lastname as LastName, student.user_id as UserId FROM student WHERE" +
                        " student.Id = @id;";
                var std = _msqlConnection.Query<LessonSignalDto>(j);
                _msqlConnection.Close();
                return std.First();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _msqlConnection.Close();
                return null;
            }

        }

        [HttpPost]
        public async Task<IActionResult> CreateRecord(Student std)
        {
            try
            {
                _msqlConnection.Open();
                var userId = std.UserId;
                var j = "SELECT student.Id as Id, student.firstname as FirstName, " +
                        "student.lastname as LastName, student.user_id as UserId FROM student WHERE" +
                        " student.Id = @userId;";
                var signalDto = _msqlConnection.Query<Student>(j);
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
    }
}
