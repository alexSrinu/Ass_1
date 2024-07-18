using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.IO;
using Ass_1.Models;

namespace Ass_1.Controllers
{
    public class AccountController : Controller
    {
        private string connectionString = ConfigurationManager.ConnectionStrings["cs"].ConnectionString;

        // GET: Account/Register
        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(Candidate candidate)
        {
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "INSERT INTO Candidates (Name, Mobile, Email, Password) VALUES (@Name, @Mobile, @Email, @Password)";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Name", candidate.Name);
                cmd.Parameters.AddWithValue("@Mobile", candidate.Mobile);
                cmd.Parameters.AddWithValue("@Email", candidate.Email);
                cmd.Parameters.AddWithValue("@Password", candidate.Password);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Login");

            //   return View(candidate);
        }

        // GET: Account/Login
        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(Login login)
        {
            if (ModelState.IsValid)
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT * FROM Candidates WHERE Email = @Email AND Password = @Password";
                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", login.Email);
                    cmd.Parameters.AddWithValue("@Password", login.Password);
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        Session["CandidateId"] = reader["CandidateId"];
                        return RedirectToAction("Profile");
                    }
                }
            }
            return View(login);
        }

        // GET: Account/Profile
        public ActionResult Profile()
        {
            int candidateId = Convert.ToInt32(Session["CandidateId"]);
            Candidate candidate = new Candidate();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT * FROM Candidates WHERE CandidateId = @CandidateId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@CandidateId", candidateId);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    candidate.CandidateId = Convert.ToInt32(reader["CandidateId"]);
                    candidate.Name = reader["Name"].ToString();
                    candidate.Mobile = reader["Mobile"].ToString();
                    candidate.Email = reader["Email"].ToString();
                    candidate.Address = reader["Address"].ToString();
                    candidate.StateId = reader["StateId"] != DBNull.Value ? Convert.ToInt32(reader["StateId"]) : 0;
                    candidate.CityId = reader["CityId"] != DBNull.Value ? Convert.ToInt32(reader["CityId"]) : 0;
                }
            }

            ViewBag.States = GetStates();
            ViewBag.Cities = candidate.StateId > 0 ? GetCities(candidate.StateId) : new List<City>();
            // return View(candidate);
            // return PartialView("Profile");
            return Json(JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult Profile(Candidate candidate, HttpPostedFileBase resume, HttpPostedFileBase photo)
        {

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "UPDATE Candidates SET Address = @Address, StateId = @StateId, CityId = @CityId WHERE CandidateId = @CandidateId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Address", candidate.Address);
                cmd.Parameters.AddWithValue("@StateId", candidate.StateId);
                cmd.Parameters.AddWithValue("@CityId", candidate.CityId);
                cmd.Parameters.AddWithValue("@CandidateId", candidate.CandidateId);

                if (resume != null && (resume.ContentType == "application/pdf" || resume.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                {
                    candidate.ResumeFileType = resume.ContentType;
                    using (var binaryReader = new System.IO.BinaryReader(resume.InputStream))
                    {
                        candidate.Resume = binaryReader.ReadBytes(resume.ContentLength);
                    }
                    query += ", Resume = @Resume, ResumeFileType = @ResumeFileType";
                    cmd.Parameters.AddWithValue("@Resume", candidate.Resume);
                    cmd.Parameters.AddWithValue("@ResumeFileType", candidate.ResumeFileType);
                }

                if (photo != null && (photo.ContentType == "image/jpeg" || photo.ContentType == "image/png"))
                {
                    candidate.PhotoFileType = photo.ContentType;
                    using (var binaryReader = new System.IO.BinaryReader(photo.InputStream))
                    {
                        candidate.Photo = binaryReader.ReadBytes(photo.ContentLength);
                    }
                    query += ", Photo = @Photo, PhotoFileType = @PhotoFileType";
                    cmd.Parameters.AddWithValue("@Photo", candidate.Photo);
                    cmd.Parameters.AddWithValue("@PhotoFileType", candidate.PhotoFileType);
                }

                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("Profile");

            ViewBag.States = GetStates();
            ViewBag.Cities = GetCities(candidate.StateId);
            // return View(candidate);
        }

        private List<State> GetStates()
        {
            List<State> states = new List<State>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT * FROM States";
                SqlCommand cmd = new SqlCommand(query, con);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    states.Add(new State
                    {
                        StateId = Convert.ToInt32(reader["StateId"]),
                        StateName = reader["StateName"].ToString()
                    });
                }
            }
            return states;
        }

        private List<City> GetCities(int stateId)
        {
            List<City> cities = new List<City>();
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string query = "SELECT * FROM Cities WHERE StateId = @StateId";
                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@StateId", stateId);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    cities.Add(new City
                    {
                        CityId = Convert.ToInt32(reader["CityId"]),
                        CityName = reader["CityName"].ToString(),
                        StateId = Convert.ToInt32(reader["StateId"])
                    });
                }
            }
            return cities;
        }

        [HttpGet]
        public JsonResult GetCitiesByStateId(int stateId)
        {
            var cities = GetCities(stateId);
            return Json(cities, JsonRequestBehavior.AllowGet);
        }
    }
}

