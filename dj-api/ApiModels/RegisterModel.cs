using System.ComponentModel.DataAnnotations;

namespace dj_api.ApiModels
{
    public class RegisterModel
    {
        [Required(ErrorMessage = "Name is required")]
        public string name { get; set; } = null!;

        [Required(ErrorMessage = "Family name is required")]
        public string familyName { get; set; } = null!;

        [Required(ErrorMessage = "Image URL is required")]
        public string imageUrl { get; set; } = null!;

        [Required(ErrorMessage = "Username is required")]
        public string username { get; set; } = null!;

        [Required(ErrorMessage = "Email is required")]
        public string email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string password { get; set; } = null!;
    }
}
