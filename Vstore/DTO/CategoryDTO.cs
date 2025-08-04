namespace Vstore.DTO
{
    public class CategoryDTO
    {
        
            [Required(ErrorMessage = "Color name is required.")]
            [RegularExpression("^[a-zA-Z ]*$", ErrorMessage = "Only English letters are allowed.")]
            public string Category_Name { get; set; }
        
    }
}
