
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace UploadPhotoFunctionApp
{
    public class PhotoInfo : TableEntity
    {
        public string OriginFileName { get; set; }
        public string BlobName { get; set; }

        public string UserId { get; set; }
        public string Comment { get; set; }
    }

    public static class PhotoUpload
    {
        [FunctionName("upload")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            [Table("UploadedPhotoInfo")] CloudTable outputUploadInfos,
            Binder binder,
            ILogger log)
        {
            log.LogInformation("File Upload HTTP trigger function processed a request.");

            if (!req.HasFormContentType)
            {
                return new BadRequestObjectResult("content type is not multipart/form-data");
            }
            try
            {
                var comment = string.Empty;
                var userId = string.Empty;

                foreach (var kv in req.Form)
                {
                    if (kv.Key == nameof(PhotoInfo.Comment).ToLower())
                    {
                        comment = kv.Value;
                        continue;
                    }

                    if (kv.Key == nameof(PhotoInfo.UserId).ToLower())
                    {
                        userId = kv.Value;
                    }
                }

                if (req.Form.Files == null || req.Form.Files.Count <= 0)
                {
                    log.LogInformation("no file uploaded!");
                    return new BadRequestResult();
                }

                foreach (var formFile in req.Form.Files)
                {
                    var originFileName = formFile.FileName;

                    var blobContainer = Environment.GetEnvironmentVariable("PhotoUploadBlobStorage");

                    var blobName = $"{Guid.NewGuid()}_{originFileName}";
                    var path = $"{blobContainer}/{blobName}";

                    // save the stream to output blob, which will save it to Azure storage blob
                    var outputBlob = await binder.BindAsync<CloudBlockBlob>(new BlobAttribute(path, FileAccess.Write));
                    outputBlob.Properties.ContentType = formFile.ContentType;
                    using (var stream = await outputBlob.OpenWriteAsync())
                    {
                        await formFile.CopyToAsync(stream);
                    }

                    log.LogInformation($"save {originFileName} to {path}");

                    var operation = TableOperation.Insert(new PhotoInfo
                    {
                        PartitionKey = Guid.NewGuid().ToString(),
                        RowKey = Guid.NewGuid().ToString(),
                        UserId = userId,
                        Comment = comment,
                        BlobName = blobName,
                        OriginFileName = originFileName,
                        
                        ETag = "*"
                    });

                    await outputUploadInfos.ExecuteAsync(operation);
                }

                return new OkObjectResult("uploaded");
            }
            catch (Exception ex)
            {
                log.LogError(ex, "processing upload error");
                return new BadRequestObjectResult("critical error occurred");
            }
        }
    }
}
