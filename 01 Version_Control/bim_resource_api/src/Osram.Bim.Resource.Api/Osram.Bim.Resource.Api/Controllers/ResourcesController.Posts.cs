using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Osram.Bim.Resource.Api.BusinessLogic;
using Osram.Bim.Resource.Api.Models;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Osram.Bim.Resources.Api.Controllers
{
    public partial class ResourcesController
        : ControllerBase
    {

        /// <summary>
        ///     Uploads a resource
        /// </summary>
        /// <param name="organizationId">Required field. Id of the Organization. Sample: 8b73b8b3-37a6-4444-894b-0bb881e2ad29</param>
        /// <param name="encSystemId">Required field. Id of the EncSystem. Sample: 738ff86f-d168-4ae7-a661-53e4fd3272bf</param>
        /// <param name="resourceName">Required field. Name of the resource. Sample: cc559d12-7f37-4aea-8979-f0083739c1ea.jpg
        /// Currently supported types:
        /// "jpg", "jpeg", "png", "bmp", "svg", "egf", "xml", "pdf", "edf", "fdb", "gdb", "dwg", "fbk", "gbk", "wmf", "svgz", "docx", "doc", "rtf", "txt", "tmp", "dat", "eventlog", "egf.gz", "wmf.info", "gz.sav"</param>
        /// <param name="resource">Required field. The resource file. File size cannot be zero. </param>
        /// <returns>Returns URL of the uploaded resource</returns>
        /// <response code="200">Returns the newly uploaded resource</response>
        /// <response code="400">No files is included in the request</response>
        /// <response code="500">If any error occurs</response>
        [SwaggerOperation(Tags = new[] { "Resources" })]
        [HttpPost("{organizationId}/{encSystemId}/{resourceName}")]
        [RequestSizeLimit(30000000)]  //TODO: 30 MB, should be put into the config.. some of the files might be large. We will add 
        public async Task<ActionResult<ResourceResponse>> UploadResource(Guid organizationId, Guid encSystemId, string resourceName, IFormFile resource)
        {
            // TODO: Might want to handle multiple files upload at once.
            // restrict file types
            // check ext consistency between resourceId.ext and resource.ext
            // check for existing resourceId with different file type too

            if (!Path.HasExtension(resourceName))
                throw GenerateResourceApiException(ResourceApiErrorCode.NoFileExtension);

            if (resource == null || resource.Length == 0)
                throw GenerateResourceApiException(ResourceApiErrorCode.InvalidFile);

            int extCount = resourceName.Count(f => f == '.');
            string fileExtension = GetFullFileExtension(resource.FileName);

            if (extCount > 2)
                throw GenerateResourceApiException(ResourceApiErrorCode.FileTypeNotSupported, fileExtension);

            if (!fileExtension.Replace(".", "").All(Char.IsLetterOrDigit))
                throw GenerateResourceApiException(ResourceApiErrorCode.InvalidChar, fileExtension);

            var temp = GetFullFileExtension((resourceName));
            if (fileExtension != GetFullFileExtension(resourceName))
                throw GenerateResourceApiException(ResourceApiErrorCode.FileExtensionMismatch);

            if (!_adapter.FileSupported(fileExtension))
                throw GenerateResourceApiException(ResourceApiErrorCode.FileTypeNotSupported, fileExtension);

            var isGuid = IsGuid(resourceName);

            if (!isGuid)
                throw GenerateResourceApiException(ResourceApiErrorCode.InvalidGuid, resourceName);

            var uploadedFileUrl = String.Empty;
            var path = ResourceUtility.CreatePathName(organizationId.ToString(), encSystemId.ToString(), resourceName.ToLower());

            if (await _adapter.DoesExist(path))
                throw GenerateResourceApiException(ResourceApiErrorCode.FileAlreadyExists, resourceName);

            using (var stream = resource.OpenReadStream())
            {
                uploadedFileUrl = await _adapter.Upload(path, stream);
            }

            string url = ConvertStorageUrlToResourceApiUrl(organizationId, uploadedFileUrl);

            _logger.Log(LogLevel.Information, "File {0} uploaded successfully.", url);

            return Ok(new ResourceResponse(url));
        }

        private static bool IsGuid(string resourceName)
        {
            Guid testGuid = Guid.Empty;
            return Guid.TryParse(resourceName.Substring(0, resourceName.IndexOf('.')), out testGuid);
        }

        private static string GetFullFileExtension(string resource)
        {
            var resourceString = resource.ToLower();
            var fileExtension = resourceString.Substring(resourceString.IndexOf('.'));
            return fileExtension;
        }
    }
}