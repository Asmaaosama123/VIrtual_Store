namespace Vstore.DTO
{
    public class ChangePasswordDTO
    {
       // public string UserId { get; set; }


        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }



        [DataType(DataType.Password)]
        public string NewPassword { get; set; }


        [DataType(DataType.Password)]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
