using Contract_Monthly_Claim_System.Models;

namespace Contract_Monthly_Claim_System.Services
{
    public class LecturerService
    {
        private static List<Lecturer> _lecturers = new List<Lecturer>
        {
            // Seed data
            new Lecturer { LecturerId = "IIE2024001", Name = "John Doe", Email = "j.doe@iie.ac.za", HourlyRate = 350.00m, Department = "Computer Science" },
            new Lecturer { LecturerId = "IIE2024002", Name = "Jane Smith", Email = "j.smith@iie.ac.za", HourlyRate = 400.00m, Department = "Engineering" }
        };

        public Lecturer GetLecturerById(string id)
        {
            return _lecturers.FirstOrDefault(l => l.LecturerId == id);
        }

        public List<Lecturer> GetAllLecturers()
        {
            return _lecturers;
        }

        public void UpdateLecturer(Lecturer updatedLecturer)
        {
            var existing = GetLecturerById(updatedLecturer.LecturerId);
            if (existing != null)
            {
                existing.Name = updatedLecturer.Name;
                existing.Email = updatedLecturer.Email;
                existing.HourlyRate = updatedLecturer.HourlyRate;
                existing.Department = updatedLecturer.Department;
            }
        }
    }
}