using Emgu.CV.Ocl;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace FaceRecognition.Entities
{
    public class FaceRecognitionDbContext : DbContext
    {
        public FaceRecognitionDbContext(DbContextOptions options) : base(options) 
        {
        }

        public DbSet<FaceImage> FaceImages { get; set; }
    }
}
