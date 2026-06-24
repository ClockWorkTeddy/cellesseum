using System.ComponentModel.DataAnnotations.Schema;

namespace Celleseum.Data
{
    public class Result
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public DateTime DateTime { get; set; }

        public int Score { get; set; }

        public int PlantsCreated { get; set; }

        public int GrazersCreated { get; set; }
    }
}
