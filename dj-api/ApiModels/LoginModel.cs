using System.ComponentModel.DataAnnotations;

namespace dj_api.ApiModels
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string username { get; set; } = null!;
        [Required(ErrorMessage = "Password is required")]
        public string password { get; set; } = null!;
    }
}
