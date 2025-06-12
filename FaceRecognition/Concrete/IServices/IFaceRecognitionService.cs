using FaceRecognition.Models;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognition.Concrete.IServices
{
    public interface IFaceRecognitionService
    {
        Task<IActionResult> UploadFaceAsync(FaceImageUploadModel model);
        Task<IActionResult> TrainModelAsync();
        Task<IActionResult> PredictUserAsync(FaceImageUploadModel model);
    }
}
