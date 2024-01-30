using Azure.Storage.Blobs;
using ImageMagick;
using Serilog;
using System.Reflection.Metadata;
using System.Xml.Linq;

namespace ImageResizerHandler
{
    public  class ImageResizerHandler
    {
        public  async Task ResizeImageInBlobStorage(string name, ILogger log, string connectionString, string directoryTemporaryResizeImage)
        {
            string originalcontainerName = "unprocessedimage";
            string resizedcontainerName = "processedimage";
            try
            {

                string input = name;
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(originalcontainerName);

                //if (string.IsNullOrEmpty(name) == false)
                //{
                //    string imagePathSource = "C:\\Users\\TLTUSer\\Desktop\\Image";
                //    string[] imageFiles = Directory.GetFiles(imagePathSource, "*.png");

                //    foreach (var imagePath in imageFiles)
                //    {
                //        var imageFileName = Path.GetFileName(imagePath);
                //        var blobClient = blobContainerClient.GetBlobClient(imageFileName);

                //        // Check if the blob already exists
                //        if (!await blobClient.ExistsAsync())
                //        {
                //            // Blob doesn't exist, proceed with the upload
                //            using (var fileStream = File.OpenRead(imagePath))
                //            {
                //                await blobClient.UploadAsync(fileStream, true);
                //                log.Information($"Uploaded image: {imageFileName} to blob container: {originalcontainerName}");
                //            }

                //        }
                //        else
                //        {
                //            // Blob already exists, log a message or take appropriate action
                //            log.Information($"Skipped upload for existing image: {imageFileName} in blob container: {originalcontainerName}");
                //        }

                //    }

                //}

                ClearTemporaryImagesInResizedOutputPath(directoryTemporaryResizeImage, log);

                //  var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);

                //if (name.Contains("_large", StringComparison.OrdinalIgnoreCase) || name.Contains("_small", StringComparison.OrdinalIgnoreCase))
                //{
                //    return;

                //    //if (name.Contains("_large", StringComparison.OrdinalIgnoreCase) && (await DoesImageAlreadyExists(connectionString, originalcontainerName, name, log)))
                //    //{
                //    //    string nameOfSmallImage = $"{name}_Small";
                //    //    bool smallImageExists = await DoesImageAlreadyExists(connectionString, originalcontainerName, nameOfSmallImage, log);

                //    //}
                //    //else
                //    //{

                //    //}
                //    //return;
                //}
                //else
                //    log.Information($"Blob trigger function Processed blob\n Name: {name} ");



                string SourcePath = Path.Combine(Path.GetTempPath(), name);

                string DestinationPath = "C:\\Users\\TLTUSer\\Desktop\\Image";

                await DownloadBlobAsync(connectionString, originalcontainerName, name, SourcePath, log);


               ResizeImage(SourcePath, DestinationPath);
                // BlobTrigger1.ImageResizer.ResizeImage(SourcePath, DestinationPath);



                bool result = await UploadImagesToContainerAsync(resizedcontainerName, DestinationPath, log);

                if (result == true)
                {
                    await DeleteBlobImage(connectionString, originalcontainerName, name, log);
                }
                else
                {
                    await DeleteAllLargeAndSmallImages(DestinationPath, name, log);
                }

                log.Information($"Blob downloaded successfully and saved to: {SourcePath}");
            }
            catch (Exception ex)
            {
                log.Error($"An error occurred: {ex.Message}");
            }
        }

        public async Task<bool> UploadImagesToContainerAsync(string resizedcontainerName, string destinationPath, ILogger log)
        {
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(resizedcontainerName);

                if (!Directory.Exists(destinationPath))
                {
                    log.Error($"Directory does not exist: {destinationPath}");
                    return false;
                }

                string[] imageFiles = Directory.GetFiles(destinationPath, "*.png");



                if (imageFiles.Length < 2)
                {
                    log.Error("At least two images are required for the operation.");
                    return false;
                }


                foreach (var imagePath in imageFiles)
                {

                    var imageFileName = Path.GetFileName(imagePath);
                    //var blobName = imageFileName.ToLower();
                    var blobClient = blobContainerClient.GetBlobClient(imageFileName);


                    // Check if the blob already exists
                    if (!await blobClient.ExistsAsync())
                    {
                        // Blob doesn't exist, proceed with the upload
                        using (var fileStream = File.OpenRead(imagePath))
                        {
                            var result = await blobClient.UploadAsync(fileStream, true);
                            log.Information($"Uploaded image: {imageFileName} to blob container: {resizedcontainerName}");
                        }
                    }
                    else
                    {
                        // Blob already exists, log a message or take appropriate action
                        log.Information($"Skipped upload for existing image: {imageFileName} in blob container: {resizedcontainerName}");
                    }
                    //using (var fileStream = File.OpenRead(imagePath))
                    //{
                    //    var result = await blobClient.UploadAsync(fileStream, true);
                    //    log.LogInformation($"Uploaded image: {imageFileName} to blob container: {containerName}");
                    //}
                }
                log.Information($"Upload operation completed for images in {destinationPath}");

                return true;

            }
            catch (Exception ex)
            {
                log.Error($"An error occurred during the upload operation: {ex.Message}");
                return false;
            }
        }


        private  async Task DownloadBlobAsync(string connectionString, string containerName, string blobName, string destinationPath, ILogger log)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    var response = await blobClient.DownloadAsync();

                    using (var fileStream = File.OpenWrite(destinationPath))
                    {
                        await response.Value.Content.CopyToAsync(fileStream);
                    }
                }
                else
                {
                    log.Error("Blob does not exist.");
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Error downloading blob: {ex.Message}");
            }
        }

        private async Task<bool> DoesImageAlreadyExists(string connectionString, string containerName, string blobName, ILogger log)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (await blobClient.ExistsAsync())
                {
                    log.Error($"blob already exists details = {blobName}");

                    return true;
                }
                else
                {
                    log.Error($"blob doesn't exist details = {blobName}");

                    return false;
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error deleting original blob: {ex.Message}");
                return false;
            }
        }
        private  async Task DeleteBlobImage(string connectionString, string containerName, string blobName, ILogger log)
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(connectionString);
                var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = blobContainerClient.GetBlobClient(blobName);

                if (await DoesImageAlreadyExists(connectionString, containerName, blobName, log))
                {
                    await blobClient.DeleteIfExistsAsync();
                    log.Information($"Original blob '{blobName}' deleted successfully.");
                }
                else
                {
                    log.Error($"Blob '{blobName}' does not exist.");
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error deleting original blob: {ex.Message}");
            }
        }
        private async Task DeleteAllLargeAndSmallImages(string destinationPath, string name, ILogger log)
        {
            try
            {
                // Get all files in the destination path
                string[] files = Directory.GetFiles(destinationPath);

                foreach (var file in files)
                {
                    string fileName = Path.GetFileName(file);

                    // Check if the file starts with "_small" or "_large" prefix
                    if (name.Contains("_large", StringComparison.OrdinalIgnoreCase) || name.Contains("_small", StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                        log.Information($"Deleted file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error deleting large and small images: {ex.Message}");
            }
        }
        public static void ClearTemporaryImagesInResizedOutputPath(string outputPath, ILogger log)
        {
            try
            {
                // Get all files in the specified output path
                string[] files = Directory.GetFiles(outputPath);

                foreach (var file in files)
                {
                    // Check if the file has an image extension 
                    string extension = Path.GetExtension(file).ToLower();
                    if (extension == ".jpg" || extension == ".png" || extension == ".gif" || extension == ".jpeg")
                    {
                        File.Delete(file);
                        log.Information($"Deleted temporary image file: {file}");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error($"Error clearing temporary images: {ex.Message}");
            }
        }
        public static string ResizeImage(string sourceFileFullPath, string destinationPath)
        {
            // Get the file name without extension
            string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(sourceFileFullPath);

            // Add the suffix "_Small" to the file name for the small size image
            string smallDestinationFileName = $"{fileNameWithoutExtension}_small{System.IO.Path.GetExtension(sourceFileFullPath)}";

            // Add the suffix "_Large" to the file name for the large size image
            string largeDestinationFileName = $"{fileNameWithoutExtension}_large{System.IO.Path.GetExtension(sourceFileFullPath)}";

            // Combine the destination file names with the destination folder
            string smallDestinationPath = System.IO.Path.Combine(destinationPath, smallDestinationFileName);
            string largeDestinationPath = System.IO.Path.Combine(destinationPath, largeDestinationFileName);

            using (var image = new MagickImage(sourceFileFullPath))
            {
                // Resize for small size
                image.Resize(500, 500);

                image.Strip();
                image.Quality = 90; // Adjust quality to achieve desired file size
                image.Write(smallDestinationPath);
            }

            using (var image = new MagickImage(sourceFileFullPath))
            {
                // Resize for large size
                image.Resize(1000, 1000); // You can adjust the dimensions for the large size

                image.Strip();
                image.Quality = 120; // Adjust quality to achieve desired file size
                image.Write(largeDestinationPath);
            }

            return $"{smallDestinationPath}, {largeDestinationPath}";
        }
    }
}
