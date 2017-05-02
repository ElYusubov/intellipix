using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using IntelliPix.Models;
using Microsoft.Extensions.Configuration;

namespace IntelliPix.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            var storageName = _config["accountName"];
            var keyValue = _config["keyValue"];
            var account = new CloudStorageAccount(new StorageCredentials(storageName, keyValue), true);
            var blobClient = account.CreateCloudBlobClient();
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
                        all.Add(new BlobInfo
                        {
                            ImageUri = blob.Uri.ToString(),
                            ThumbnailUri = blob.Uri.ToString().Replace("/photos/", "/thumbnails/")
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
        public IActionResult Upload()
        {
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
