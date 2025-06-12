using FaceRecognition.Concrete.IServices;
using FaceRecognition.Models;
using Microsoft.AspNetCore.Mvc;

namespace FaceRecognition.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FaceRecognitionController : ControllerBase
    {
        private readonly IFaceRecognitionService _faceRecognitionService;

        public FaceRecognitionController(IFaceRecognitionService faceRecognitionService)
        {
            _faceRecognitionService = faceRecognitionService;
        }


        [HttpPost("upload")]
        public async Task<IActionResult> UploadFace([FromBody] FaceImageUploadModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                return await _faceRecognitionService.UploadFaceAsync(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error processing face: {ex.Message}");
            }
        }


        [HttpPost("train")]
        public async Task<IActionResult> TrainModel()
        {
            try
            {
                return await _faceRecognitionService.TrainModelAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Training error: {ex.Message}");
            }
        }


        [HttpPost("predict")]
        public async Task<IActionResult> PredictUser([FromBody] FaceImageUploadModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                return await _faceRecognitionService.PredictUserAsync(model);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error during prediction: {ex.Message}");
            }
        }
    }
}
