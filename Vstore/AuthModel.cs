namespace Vstore
{
    public class AuthModel
    {
        public string Massage { get; set; }
        public bool IsAuthonticated { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public List<string> Roles { get; set; }
        public string Token { get; set; }
        public DateTime Expireon { get; set; }
       // public User User { get; set; }
    }
}
