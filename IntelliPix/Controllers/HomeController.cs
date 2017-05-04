using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using IntelliPix.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using ImageSharp;
using ImageSharp.Formats;
using Microsoft.ProjectOxford.Vision;
using System.Linq;

namespace IntelliPix.Controllers
{
    public class HomeController : Controller
    {
        private readonly CloudStorageAccount _account;
        private readonly VisionServiceClient _vision;

        public HomeController(CloudStorageAccount account, VisionServiceClient vision)
        {
            _account = account;
            _vision = vision;
        }

        public async Task<IActionResult> Index()
        {
            var blobClient = _account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("photos");
            BlobContinuationToken token = null;
            var all = new List<BlobInfo>();
            do
            {
                var blobSegment = await container.ListBlobsSegmentedAsync(token);
                token = blobSegment.ContinuationToken;

                foreach (var item in blobSegment.Results)
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        var blob = (CloudBlockBlob)item;
                        await blob.FetchAttributesAsync();
                        var caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name;
                        var tags = blob.Metadata.Where(x => x.Key.StartsWith("tag"));

                        all.Add(new BlobInfo
                        {
                            ImageUri = blob.Uri.ToString(),
                            ThumbnailUri = blob.Uri.ToString().Replace("/photos/", "/thumbnails/"),
                            Caption = caption,
                            Tags = string.Join(", ", tags.Select(x => x.Value))
                        });
                    }
                }

            } while (token != null);

            return View(all);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file != null && file.Length > 0)
            {
                if (!file.ContentType.StartsWith("image"))
                {
                    TempData["Message"] = "Only image files may be uploaded.";
                }
                else
                {
                    var client = _account.CreateCloudBlobClient();
                    var container = client.GetContainerReference("photos");
                    var photo = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                    await photo.UploadFromStreamAsync(file.OpenReadStream());
                    file.OpenReadStream().Seek(0L, SeekOrigin.Begin);

                    using (var outputStream = new MemoryStream())
                    {
                        Configuration.Default.AddImageFormat(new PngFormat());
                        using (var image = Image.Load(file.OpenReadStream()))
                        {
                            image.Resize(new Size { Width = 252, Height = 252 }).SaveAsPng(outputStream);
                            container = client.GetContainerReference("thumbnails");
                            var thumbnail = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                            outputStream.Seek(0L, SeekOrigin.Begin);
                            await thumbnail.UploadFromStreamAsync(outputStream);
                        }
                    }

                    var features = new VisualFeature[] { VisualFeature.Description };
                    var result = await _vision.AnalyzeImageAsync(photo.Uri.ToString(), features);

                    photo.Metadata.Add("Caption", result.Description.Captions.First().Text);

                    for (int i = 0; i < result.Description.Tags.Length; i++)
                    {
                        var key = $"tag{i}";
                        photo.Metadata.Add(key, result.Description.Tags[i]);
                    }

                    await photo.SetMetadataAsync();
                }
            }
            else
            {
                TempData["Message"] = "Not uploaded! The file is empty :c";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
