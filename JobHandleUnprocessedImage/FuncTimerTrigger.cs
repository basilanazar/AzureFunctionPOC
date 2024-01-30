using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageResizerHandler;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Serilog;

namespace JobHandleUnprocessedImage
{
    public class FuncTimerTrigger
    {
        [FunctionName("FuncTimerTrigger")]
        public static async Task Run([TimerTrigger("0 */1 * * * *")]TimerInfo myTimer, 
            [Blob("unprocessedimage", Connection = "AzureWebJobsStorage")] BlobContainerClient containerClient
            )
        {
            try
            {
                //Log.Logger = new LoggerConfiguration()
                //.WriteTo.Console()
                //.CreateLogger();
                List<String> imageNamesInUnProcessedImageContainer = await getAllImagesInUnprocesedImageContainer(Log.Logger);
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string directoryTemporaryResizeImage = "C:\\Users\\TLTUSer\\Desktop\\Image";
                foreach (String name in imageNamesInUnProcessedImageContainer)
                {

                    ImageResizerHandler.ImageResizerHandler imageResizerHandler = new ImageResizerHandler.ImageResizerHandler();
                    imageResizerHandler.ResizeImageInBlobStorage(name, Log.Logger, connectionString, directoryTemporaryResizeImage);

                }
                Log.Information($"C# Timer trigger function executed at: {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"An error occurred: {ex.Message}");
            }


        }
        public static async Task<List<string>> getAllImagesInUnprocesedImageContainer(Serilog.ILogger log)
        {
            List<string> imageNames = new List<string>();

            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                string containerName = "unprocessedimage";

                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                await foreach (BlobItem blobItem in blobContainerClient.GetBlobsAsync())
                {
                    // Assuming you want to fetch only image file names 
                    if (blobItem.Name.EndsWith(".jpg") || blobItem.Name.EndsWith(".jpeg") || blobItem.Name.EndsWith(".png"))
                    {
                        imageNames.Add(blobItem.Name);
                    }
                }

                log.Information($"Retrieved {imageNames.Count} image names from container: {containerName}");
            }
            catch (Exception ex)
            {
                log.Error($"Error occurred while retrieving image names: {ex.Message}");
            }

            return imageNames;
        }
    }
}

