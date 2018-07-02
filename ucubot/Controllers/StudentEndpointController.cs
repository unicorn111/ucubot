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
                var comm = "SELECT student.Id as Id, student.firstname as FirstName, " +
                        "student.lastname as LastName, student.user_id as UserId FROM student;";
                var lst = _msqlConnection.Query<LessonSignalDto>(comm).ToList();
                _msqlConnection.Close();
                return lst;
            }
            catch (Exception e)
            {
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
                var comm = "SELECT student.Id as Id, student.firstname as FirstName, " +
                        "student.lastname as LastName, student.user_id as UserId FROM student WHERE" +
                        " student.Id = @id;";
                var std = _msqlConnection.Query<LessonSignalDto>(comm).ToList();
                _msqlConnection.Close();
                return std.First();
            }
            catch (Exception e)
            {
                _msqlConnection.Close();
                return null;
            }

        }

        [HttpPost]
        public async Task<HttpStatusCode> CreateRecord(Student student)
        {
            _msqlConnection.Open();
            var uId = student.UserId;
            var fName = student.FirstName;
            var lName = student.LastName;
            var comm = "INSERT INTO student(first_name, last_name, user_id) VALUES(@first_name, @last_name, @user_id);";
            try
            {
                _msqlConnection.Execute(comm, new {first_name = fName, last_name = lName, user_id = uId});
                _msqlConnection.Close();
            }
            catch
            {
                _msqlConnection.Close();
                return HttpStatusCode.Conflict;
            }
            return HttpStatusCode.OK;
       }
       
       [HttpPut]
       public async Task<HttpStatusCode> UpdateStudent(Student student)
       {
            _msqlConnection.Open();
            var uId = student.UserId;
            var fName = student.FirstName;
            var lName = student.LastName;
            var comm = "UPDATE student set first_name =@first, last_name = @second, user_id = @uid  where id = @uuid;";
            try
            {
                _msqlConnection.Execute(comm, new {first_name = fName, last_name = lName, user_id = uId});
                _msqlConnection.Close();
            }
            catch (Exception e)
            {
                _msqlConnection.Close();
                return HttpStatusCode.Conflict;
            }
            return HttpStatusCode.OK;
       }
       
       [HttpDelete("{id}")]
       public async  Task<HttpStatusCode> RemoveStudent(long id)
       {
          _msqlConnection.Open();           
           var comm = "DELETE FROM student WHERE id = @id;";
           try
           {
               _msqlConnection.Execute(comm, new {Id = id});
               _msqlConnection.Close();
           }
           catch (Exception e)
           {
               _msqlConnection.Close();
               return HttpStatusCode.Conflict;
           }
           return HttpStatusCode.OK;
       }
    }
}
