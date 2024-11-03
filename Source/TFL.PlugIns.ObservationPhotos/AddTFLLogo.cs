using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Messages;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using static System.Net.WebRequestMethods;
using System.Reflection;
using Microsoft.AspNetCore.StaticFiles;
using System.IdentityModel.Metadata;
using System.Runtime.Remoting.Services;

namespace TFL.PlugIns.ObservationPhotos
{
    public class AddTFLLogo : IPlugin
    {
        public void Execute(IServiceProvider serviceProvider)
        {
            var context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            var serviceFactory =(IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            var traceing = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IOrganizationService service = serviceFactory.CreateOrganizationService(null);
            traceing.Trace("Started Plug in");

                traceing.Trace($"Depth is  {context.Depth} ");
            //if (context.Depth > 2) return;
                var target = context.InputParameters["Target"] as Entity;
                if (target !=null)
                {
                    var initializeFileBlocksDownloadRequest = new InitializeFileBlocksDownloadRequest
                    {
                        Target = new EntityReference(target.LogicalName, target.Id),
                        FileAttributeName = "sp_photo"
                    };

                    var initializeFileBlocksDownloadResponse = (InitializeFileBlocksDownloadResponse)
                        service.Execute(initializeFileBlocksDownloadRequest);
                    traceing.Trace($"initilising Download Block request");
                    DownloadBlockRequest downloadBlockRequest = new DownloadBlockRequest
                    {
                        FileContinuationToken = initializeFileBlocksDownloadResponse.FileContinuationToken
                    };
                    var downloadBlockResponse = (DownloadBlockResponse)service.Execute(downloadBlockRequest);

                    traceing.Trace($"Calling Method to Place  Logo");
                    byte[] newIimageByte = OverLayImage(downloadBlockResponse.Data);
                    traceing.Trace($"Calling Method to Update the new file back to Record");
                    UploadFile(traceing,service, "sp_observationphoto", target.Id, "sp_photo", newIimageByte,"AddedLogo");
                }            
        }
        private static void UploadFile(ITracingService traceing, IOrganizationService svc, string entityName, Guid recordGuid,
       string fileAttributeName, byte[] filebyte, string fileName)
        {
            // get the file content in byte array
            var fileContentByteArray = filebyte;
            traceing.Trace($"Initilising Block Upload Request");
            var initializeFileBlocksUploadRequest = new InitializeFileBlocksUploadRequest()
            {
                Target = new EntityReference(entityName, recordGuid),
                FileAttributeName = fileAttributeName,
                FileName = fileName
            };
            traceing.Trace($"Executing  Block Upload Request");
            var initializeFileBlocksUploadResponse = (InitializeFileBlocksUploadResponse)
                svc.Execute(initializeFileBlocksUploadRequest);
            var lstBlock = new List<string>();
            if (fileContentByteArray.Length< 4194304)
            {
                var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                lstBlock.Add(blockId);
                traceing.Trace($"Initilising UploadBlockRequest in side if bloack");
                var uploadBlockRequest = new UploadBlockRequest()
                {
                    BlockId = blockId,
                    BlockData = fileContentByteArray.ToArray(),
                    FileContinuationToken = initializeFileBlocksUploadResponse.FileContinuationToken
                };

                var uploadBlockResponse = (UploadBlockResponse)svc.Execute(uploadBlockRequest);
                traceing.Trace($"Received the  UploadBlockResponse");
                traceing.Trace($"Initilising   CommitBlock");

                var commitFileBlocksUploadRequest = new CommitFileBlocksUploadRequest
                {
                    FileContinuationToken = initializeFileBlocksUploadResponse.FileContinuationToken,
                    FileName = fileName,
                    MimeType = "image/jpeg",
                    BlockList = lstBlock.ToArray()

                };
                
                var commitFileBlocksUploadResponse = (CommitFileBlocksUploadResponse)svc.Execute(commitFileBlocksUploadRequest);
                traceing.Trace($"CommitBlock Completed");
            }
            else
            {
                traceing.Trace($"Initilising UploadBlockRequest Else if bloack");
                for (int i = 0; i < fileContentByteArray.Length / 4194304; i++)
                {
                    var blockId = Convert.ToBase64String(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
                    lstBlock.Add(blockId);
                    var uploadBlockRequest = new UploadBlockRequest()
                    {
                        BlockId = blockId,
                        BlockData = fileContentByteArray.Skip(i * 4194304).Take(4194304).ToArray(),
                        FileContinuationToken = initializeFileBlocksUploadResponse.FileContinuationToken
                    };

                    var uploadBlockResponse = (UploadBlockResponse)svc.Execute(uploadBlockRequest);
                    traceing.Trace($"Received the  UploadBlockResponse Else Block");
                    traceing.Trace($"Initilising   CommitBlock Else Block" );

                    var commitFileBlocksUploadRequest = new CommitFileBlocksUploadRequest
                    {
                        FileContinuationToken = initializeFileBlocksUploadResponse.FileContinuationToken,
                        FileName = fileName,
                        MimeType = "image/jpeg",
                        BlockList = lstBlock.ToArray()

                    };

                    var commitFileBlocksUploadResponse = (CommitFileBlocksUploadResponse)svc.Execute(commitFileBlocksUploadRequest);
                    traceing.Trace($"CommitBlock Completed Else Block");
                }
            }
        }

        private byte[] OverLayImage(byte[] databytes)
        {
            try
            {
                using (var ms= new MemoryStream(databytes))
                using (var bitmap= new Bitmap(ms))
                using (var g= Graphics.FromImage(bitmap))
                using (var pen = new Pen(ColorTranslator.FromHtml("#0019a7"), 6))
                using (var brush = new SolidBrush(ColorTranslator.FromHtml("#0019a8")))
                using (var output = new MemoryStream())
                {
                    g.SmoothingMode=SmoothingMode.AntiAlias;
                    g.DrawEllipse(pen, bitmap.Width - 37, bitmap.Height - 41, 26, 26);
                    g.DrawRectangle(pen, bitmap.Width - 44, bitmap.Height - 31, 40, 6);
                    bitmap.Save(output, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return output.ToArray();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
