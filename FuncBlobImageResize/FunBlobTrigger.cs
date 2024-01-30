using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FuncBlobImageResize
{
    public class FunBlobTrigger
    {
        [FunctionName("FunBlobTrigger")]
        public async Task  Run([BlobTrigger("unprocessedimage/{name}", Connection = "AzureWebJobsStorage")]Stream myBlob, string name)
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string directoryTemporaryResizeImage = "C:\\Users\\TLTUSer\\Desktop\\Image";
            //Log.Logger = new LoggerConfiguration()
            //   .WriteTo.Console()
            //   .CreateLogger();

            ImageResizerHandler.ImageResizerHandler imageResizerHandler = new ImageResizerHandler.ImageResizerHandler();

            await imageResizerHandler.ResizeImageInBlobStorage(name, Log.Logger, connectionString, directoryTemporaryResizeImage);

            //Log.CloseAndFlush();

        }
    }
}
