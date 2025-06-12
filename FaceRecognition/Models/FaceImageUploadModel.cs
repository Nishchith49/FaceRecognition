using System.ComponentModel.DataAnnotations;

namespace FaceRecognition.Models
{
    public class FaceImageUploadModel
    {
        [Required]
        public long UserId { get; set; }

        [Required]
        //[RegularExpression(@"^data:image\/(png|jpeg);base64,", ErrorMessage = "Invalid image format.")]
        public string Base64Image { get; set; } = string.Empty;
    }
}
