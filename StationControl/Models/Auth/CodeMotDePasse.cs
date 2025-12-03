using System;

namespace StationControl.Models.Auth
{
    public class CodeMotDePasse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Code { get; set; }
        public DateTime DateExpiration { get; set; }
        public bool IsUsed { get; set; }
    }
}
