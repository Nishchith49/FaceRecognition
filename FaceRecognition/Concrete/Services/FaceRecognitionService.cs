using Emgu.CV;
using Emgu.CV.Face;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using FaceRecognition.Concrete.IServices;
using FaceRecognition.Entities;
using FaceRecognition.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;

namespace FaceRecognition.Services
{
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly FaceRecognitionDbContext _context;
        private readonly string _modelPath = Path.Combine(Directory.GetCurrentDirectory(), "model.yml");
        private readonly string _cascadePath = Path.Combine(Directory.GetCurrentDirectory(), "haarcascade_frontalface_default.xml");

        public FaceRecognitionService(FaceRecognitionDbContext context)
        {
            _context = context;
        }


        public async Task<IActionResult> UploadFaceAsync(FaceImageUploadModel model)
        {
            var imageData = GetImageBytes(model.Base64Image);
            if (imageData == null)
                return new BadRequestObjectResult("Invalid base64 image data.");

            using var ms = new MemoryStream(imageData);
            using var bitmap = new Bitmap(ms);
            var image = ConvertBitmapToColorImage(bitmap);
            var gray = image.Convert<Gray, byte>();

            var cascade = new CascadeClassifier(_cascadePath);
            var faces = cascade.DetectMultiScale(gray, 1.1, 10, Size.Empty);

            if (faces.Length == 0)
                return new BadRequestObjectResult("No face detected.");

            foreach (var face in faces)
            {
                var faceImg = gray.Copy(face).Resize(200, 200, Emgu.CV.CvEnum.Inter.Linear);
                using var memStream = new MemoryStream();
                faceImg.ToBitmap().Save(memStream, ImageFormat.Jpeg);
                var faceBytes = memStream.ToArray();

                _context.FaceImages.Add(new FaceImage
                {
                    UserId = model.UserId,
                    ImageData = faceBytes
                });
            }

            await _context.SaveChangesAsync();
            return new OkObjectResult("Face saved successfully.");
        }


        public async Task<IActionResult> TrainModelAsync()
        {
            var recognizer = new LBPHFaceRecognizer();
            var images = new List<Image<Gray, byte>>();
            var labels = new List<int>();
            var labelToUserIdMap = new Dictionary<int, long>();

            var faceEntries = await _context.FaceImages.ToListAsync();
            int labelCounter = 0;

            foreach (var group in faceEntries.GroupBy(f => f.UserId))
            {
                foreach (var face in group)
                {
                    using var ms = new MemoryStream(face.ImageData);
                    using var bmp = new Bitmap(ms);
                    var grayImage = ConvertBitmapToGrayImage(bmp).Resize(200, 200, Emgu.CV.CvEnum.Inter.Linear);
                    images.Add(grayImage);
                    labels.Add(labelCounter);
                }

                labelToUserIdMap[labelCounter] = group.Key;
                labelCounter++;
            }

            if (!images.Any())
                return new BadRequestObjectResult("No training data found.");

            using var trainImages = new VectorOfMat(images.Select(i => i.Mat).ToArray());
            using var labelVec = new VectorOfInt(labels.ToArray());

            recognizer.Train(trainImages, labelVec);
            recognizer.Write(_modelPath);

            return new OkObjectResult("Model trained successfully.");
        }


        public async Task<IActionResult> PredictUserAsync(FaceImageUploadModel model)
        {
            if (!System.IO.File.Exists(_modelPath))
                return new BadRequestObjectResult("Model not found. Please train first.");

            var imageData = GetImageBytes(model.Base64Image);
            if (imageData == null)
                return new BadRequestObjectResult("Invalid base64 image data.");

            using var ms = new MemoryStream(imageData);
            using var bitmap = new Bitmap(ms);
            var image = ConvertBitmapToColorImage(bitmap);
            var gray = image.Convert<Gray, byte>();

            var cascade = new CascadeClassifier(_cascadePath);
            var faces = cascade.DetectMultiScale(gray, 1.1, 10, Size.Empty);

            if (faces.Length == 0)
                return new BadRequestObjectResult("No face detected.");

            var recognizer = new LBPHFaceRecognizer();
            recognizer.Read(_modelPath);

            var faceEntries = await _context.FaceImages.ToListAsync();
            var labelMap = faceEntries
                .GroupBy(f => f.UserId)
                .Select((g, index) => new { Label = index, UserId = g.Key })
                .ToDictionary(x => x.Label, x => x.UserId);

            foreach (var face in faces)
            {
                var testFace = gray.Copy(face).Resize(200, 200, Emgu.CV.CvEnum.Inter.Linear);
                var result = recognizer.Predict(testFace);

                if (result.Label == -1 || result.Distance > 100)
                    return new OkObjectResult("No match found.");

                var userId = labelMap.ContainsKey(result.Label) ? labelMap[result.Label] : -1;
                return new OkObjectResult($"Match found: UserId {userId}, Confidence: {result.Distance}");
            }

            return new BadRequestObjectResult("Face recognition failed.");
        }

        private byte[]? GetImageBytes(string base64)
        {
            try
            {
                return Convert.FromBase64String(base64);
            }
            catch
            {
                return null;
            }
        }

        private static Image<Bgr, byte> ConvertBitmapToColorImage(Bitmap bitmap)
        {
            var formattedBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using (var g = Graphics.FromImage(formattedBitmap))
            {
                g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            }

            var rect = new Rectangle(0, 0, formattedBitmap.Width, formattedBitmap.Height);
            var bitmapData = formattedBitmap.LockBits(rect, ImageLockMode.ReadOnly, formattedBitmap.PixelFormat);
            int bytes = Math.Abs(bitmapData.Stride) * bitmapData.Height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, rgbValues, 0, bytes);
            formattedBitmap.UnlockBits(bitmapData);

            var img = new Image<Bgr, byte>(formattedBitmap.Width, formattedBitmap.Height);
            img.Bytes = rgbValues;
            return img;
        }

        private static Image<Gray, byte> ConvertBitmapToGrayImage(Bitmap bitmap)
        {
            return ConvertBitmapToColorImage(bitmap).Convert<Gray, byte>();
        }
    }
}
