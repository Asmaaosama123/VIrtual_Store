namespace Vstore.DTO
{
    public class ColorDTO
    {
      
            [Required(ErrorMessage = "Color name is required.")]
            [RegularExpression("^[a-zA-Z ]*$", ErrorMessage = "Only English letters are allowed.")]
            public string Color_Name { get; set; }
        
    }
}
